using Microsoft.AspNetCore.Mvc;
using CreateTicketApi.BusinessLogic;
using EventManagementDbAccess;
using Microsoft.AspNetCore.Authorization;
using System.Text;
using Mysqlx.Crud;
using System.Net;
using System.Security.Claims;
using EventUtils;

namespace CreateTicketApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SalesOrderController : ControllerBase
    {
        private readonly SalesOrderConductor _salesOrderConductor;
        private readonly SalesOrderDbAccess _dbAccess;
        private readonly TicketAccess _ticketAccess;

        private readonly EventDbAccess _eventAccess;
        private readonly ILogger<SalesOrderController> _logger;

        private readonly JwtUtils _tokenUtils;


        public SalesOrderController(ILogger<SalesOrderController> logger,
         SalesOrderConductor salesOrderConductor, SalesOrderDbAccess dbAccess, EventDbAccess eventDbAccess,
          TicketAccess ticketAccess, JwtUtils tokenUtils)
        {
            _salesOrderConductor = salesOrderConductor;
            _dbAccess = dbAccess;
            _ticketAccess = ticketAccess;
            _eventAccess = eventDbAccess;
            _logger = logger;
            _tokenUtils = tokenUtils;
     
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CustomerSalesOrder order)
        {
            if (order == null)
                return BadRequest("Order is null.");

            var result = await _salesOrderConductor.CreateSalesOrder(order);
            if (result.Item1 == null)
                return StatusCode(500, "Failed to create sales order.");
            else
            {
                if (!result.Item2)
                    return Ok(new {SalesOrderData= result.Item1});
                else
                {
                    //need to create temporary token here since this was guest checkout
                    _logger.LogInformation("Guest checkout detected. Creating and sending temp token");
                    var _accessToken = _tokenUtils.GenerateGuestJwtToken(result.Item1.UserId.ToString(),
                                         UserRoles.Attendee.ToString());
                    CustomerSalesOrder returnOrder = result.Item1;
                    return Ok(new
                    {
                        SalesOrderData= result.Item1,
                        AccessToken = _accessToken,
                        User = new { id = returnOrder.UserId.ToString(), guest=true,email = returnOrder.EmailAddress, 
                                    role = UserRoles.Attendee.ToString(), customerId = 0, 
                                     name = returnOrder.Name ?? string.Empty }
                    });
                }
            }
        }


        //Not being used currently
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
                return BadRequest("Id is null.");
            var result = await _salesOrderConductor.DeleteSalesOrder(id);
            if (result)
                return Ok("Sales order deleted.");
            else
                return StatusCode(500, "Failed to delete sales order.");
            // Implement delete logic here if needed

        }

       
        [HttpGet("/SalesOrderQrImage/{orderId}")] 
        [Authorize(Policy="OrderOwnedByUser")]
        public async Task<ActionResult> GetSalesOrderQrImage(int orderId)
        {
            if (orderId <= 0)
                return BadRequest("Invalid order id.");

            try
            {
                var retData = await _dbAccess.GetSalesOrderQrImage(orderId);
                return Ok(retData);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving QR code image: {ex.Message}");
            }
        }


        [HttpGet("/SalesOrderStatus/{orderId}")]
        [Authorize(Policy="OrderOwnedByUser")]        
        public async Task<ActionResult> GetSalesOrderStatus(int orderId)
        {
            if (orderId <= 0)
                return BadRequest("Invalid order id.");

            try
            {
                _logger.LogInformation($"Retrieving sales order status for orderId: {orderId}");
                 var data= await _dbAccess.GetSalesOrderPaymentStatus(orderId);
                 _logger.LogInformation($"Retrieved sales order status data for orderId: {data}");
                 return Ok(data);
                //return Ok(new { Paid = retData.paid , SalesOrderCode = retData.SalesOrderCode, SalesOrderQrCodeImage = retData.QrImage });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving sales order status: {ex.Message}");
            }
        }

        [HttpGet("/SalesOrderRefundAmount/{orderId}/{eventId}")]
        [Authorize(Policy="OrderOwnedByUser")]        
        public async Task<ActionResult> GetSalesOrderRefundAmount(int orderId, int eventId)
        {
            if (orderId <= 0 || eventId <=0)
                return BadRequest("Invalid order or eventId id.");

            try
            {
                EventHeader evt = await _eventAccess.GetEventHeaderById(eventId);
                if (evt == null || evt.RefundMode != RefundMode.CustomerControlled)
                {   
                    return StatusCode(409, "Refund mode must be customer controlled");
                }
                var retData = await _ticketAccess.GetOrderTotalPrice(orderId);
                return Ok(retData);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving sales order refund amount: {ex.Message}");
            }
        }

        [HttpGet("/SalesOrder/byEmailLinkId/{encryptedOrderId}")]
        public async Task<ActionResult> GetSalesOrderByEmailLinkId(string encryptedOrderId)
        {
            _logger.LogInformation("Received request for sales order details with email link ID: {0}", encryptedOrderId);
            if (string.IsNullOrEmpty(encryptedOrderId))
                return BadRequest("Invalid encrypted order id.");
            string decodedId = EncryptionHelper.UrlDecode(encryptedOrderId);
            _logger.LogInformation("Decoded email link ID: {0}", decodedId);
            
            if (string.IsNullOrEmpty(decodedId))
                return BadRequest("Invalid encrypted order id after decoding.");
            try
            {
                var result = await _dbAccess.GetEmailLinkOrderDetails(decodedId);
                if (result != null)
                {
                    _logger.LogInformation("Sales order details retrieved for email link ID: {0} {1}", encryptedOrderId, result.SalesOrderCode);
                    IEnumerable<EventSalesItem> tickets = await _salesOrderConductor.GetSalesOrderByQrCode(result.EventId, result.SalesOrderCode,result.UserId);
                    result.TicketDetails = [.. tickets];
                    return Ok(result);
                }
                else
                    return NotFound("Sales order not found for the provided email link ID.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving sales order: {ex.Message}");
            }
        }

        [HttpGet("/SalesOrderByCustomer/{customerId}")]
        [Authorize(Policy="FullAdminMinimum")]
        [Authorize(Policy="MatchingCustomer")] 
        public  async Task<ActionResult> GetSalesOrderByCustomer(int customerId, int eventId, DateOnly startDate, DateOnly endDate,
                                                        string emailAddress = "", string name = "", string orderStatus = "",
                                                        string orderByColumn = "createat", bool isAscending = false,
                                                        string? dateCursor = null, int? orderIdCursor = null,
                                                        int limit = 10)
        {
            if (customerId <= 0)
                return BadRequest("Invalid customer id.");

            SalesOrderStatus orderStatusData = SalesOrderStatus.InProgress;
            Enum.TryParse(orderStatus, out orderStatusData);
            
            var result = await _dbAccess.SearchByCustomer(customerId, eventId, startDate, endDate,
                                                        emailAddress, name, (int)orderStatusData,
                                                        orderByColumn, isAscending,
                                                        dateCursor, orderIdCursor,
                                                        limit);
            if (result != null)
            {
                Console.WriteLine("retrieved orders");
                return Ok( result);
            }
            else
                return StatusCode(500, "Failed to delete sales order.");
        }

        [HttpGet("/DownloadOrderReport/{customerId}")]
        [Authorize(Policy="FullAdminMinimum")]  
        [Authorize(Policy="MatchingCustomer")] 
        public  async Task<ActionResult> DownloadOrderReport(int customerId, int eventId, DateOnly startDate, DateOnly endDate,
                                                        string emailAddress = "", string name = "", 
                                                        string orderStatus = "",
                                                        bool isAscending = false
                                                       )
        {
            if (customerId <= 0)
                return BadRequest("Invalid customer id.");
        
            Enum.TryParse(orderStatus, out SalesOrderStatus orderStatusData);
            
            var result = await _dbAccess.SearchByCustomer(customerId, eventId, startDate, endDate,
                                                        emailAddress, name, (int)orderStatusData,
                                                        string.Empty, isAscending,
                                                        null, null,
                                                        -1);
            
            if (result != null)
            {
                Console.WriteLine("retrieved orders");
                
                 var sb = new StringBuilder();
                // CSV header
                sb.AppendLine("Order Id, Date, Name, Email,  Event, Status,Order Total, Item Count");
                foreach (var o in result)
                {
                    sb.AppendLine(
                        $"{o.OrderId}," +
                        $"{o.OrderDate:yyyy-MM-dd HH:mm:ss}," +                     
                        $"{Escape(o.FullName)}," +
                        $"{Escape(o.EmailAddress)}," +
                        $"{Escape(o.EventName)}," +
                        $"{o.SalesOrderStatus}," +
                        $"{o.OrderTotal}," +
                        $"{o.OrderCount}"
                    );
                }
                var bytes = Encoding.UTF8.GetBytes(sb.ToString());
                return File(
                    bytes,
                    "text/csv",
                    $"sales-orders-{DateTime.UtcNow:yyyyMMdd}.csv"
                );             
            }
            else
                return StatusCode(500, "Failed to delete sales order.");
        }

        private static string Escape(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            {
                value = value.Replace("\"", "\"\"");
                return $"\"{value}\"";
            }

            return value;
        }
      

        [HttpPost]
        [Route("/SalesOrder/ReturnTickets/{stripeSessionId}/{orderStatus}")]
        [Authorize]
        public async Task<IActionResult> ReturnTicketsToPool(string stripeSessionId, string orderStatus)
        {
            if (string.IsNullOrEmpty(stripeSessionId) || string.IsNullOrEmpty(orderStatus))
                return BadRequest("Invalid session id or order status.");
            if (Enum.TryParse<SalesOrderStatus>(orderStatus, true, out SalesOrderStatus tempStatus))  
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var intUserId))
                    return Unauthorized("User ID not found in token.");
                var result = await _dbAccess.ReturnTicketsToPool(tempStatus, stripeSessionId, intUserId);
                if (result)
                    return Ok("Tickets returned to pool and sales order updated.");
                else
                    return StatusCode(500, "Failed to return tickets to pool.");
            }
            else
            {
                return BadRequest("Invalid Status value:"+orderStatus);
            }
        }       


        
        [HttpGet]
        [Route("/SalesOrder/ByUserId/{userId}")]
        [Authorize(Policy="MatchingUserId")] 
        public async Task<ActionResult<List<UserSalesOrders>>> GetUpcomingSalesOrdersByUserId(int userId)
        {
            if (userId <= 0)
                return BadRequest();
            var result = await _dbAccess.GetUpcomingSalesOrdersForUser(userId);
            if (result != null)
            {
                Console.WriteLine("retrieved orders");
                return result;
            }
            else
                return StatusCode(500, "Failed to delete sales order.");
        }
        
    }
}