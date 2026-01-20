using Microsoft.AspNetCore.Mvc;
using CreateTicketApi.BusinessLogic;
using EventManagementDbAccess;
using Microsoft.AspNetCore.Authorization;
using System.Text;

namespace CreateTicketApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SalesOrderController : ControllerBase
    {
        private readonly SalesOrderConductor _salesOrderConductor;
        private readonly SalesOrderDbAccess _dbAccess;

        public SalesOrderController(SalesOrderConductor salesOrderConductor, SalesOrderDbAccess dbAccess)
        {
            _salesOrderConductor = salesOrderConductor;
            _dbAccess = dbAccess;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CustomerSalesOrder order)
        {
            if (order == null)
                return BadRequest("Order is null.");

            CustomerSalesOrder result = await _salesOrderConductor.CreateSalesOrder(order);
            if (result == null || result.SalesOrderCode == null)
                return StatusCode(500, "Failed to create sales order.");
            else
                return Ok(result);
        }



        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CustomerSalesOrder order)
        {
            if (order == null || id <= 0)
                return BadRequest("Invalid order data.");

            var result = await _salesOrderConductor.UpdateSalesOrder(id, order);
            if (result == null || result.SalesOrderCode == null)
                return StatusCode(500, "Failed to create sales order.");
            else
                return Ok(result);
        }

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
        public async Task<ActionResult> GetSalesOrderQrImage(int orderId)
        {
            if (orderId <= 0)
                return BadRequest("Invalid order id.");

            try
            {
                var retData = await _dbAccess.GetSalesOrderQrImage(orderId);
                return Ok(new { SalesOrderQrCodeImage = retData.Item2, SalesOrderCode = retData.Item1 });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving QR code image: {ex.Message}");
            }
        }

        [HttpGet("/SalesOrderStatus/{orderId}")]
        
        public async Task<ActionResult> GetSalesOrderStatus(int orderId)
        {
            if (orderId <= 0)
                return BadRequest("Invalid order id.");

            try
            {
                var retData = await _dbAccess.GetSalesOrderPaymentStatus(orderId);
                return Ok(new { Paid = retData.paid , SalesOrderCode = retData.SalesOrderCode, SalesOrderQrCodeImage = retData.QrImage });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving sales order status: {ex.Message}");
            }
        }


        [HttpGet("/SalesOrderByCustomer/{customerId}")]
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
        public async Task<IActionResult> ReturnTicketsToPool(string stripeSessionId, string orderStatus)
        {
            if (string.IsNullOrEmpty(stripeSessionId) || string.IsNullOrEmpty(orderStatus))
                return BadRequest("Invalid session id or order status.");
            if (Enum.TryParse<SalesOrderStatus>(orderStatus, true, out SalesOrderStatus tempStatus))  
            {
                var result = await _dbAccess.ReturnTicketsToPool(tempStatus, stripeSessionId);
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


        [Authorize] 
        [HttpGet]
        [Route("/SalesOrder/ByUserId/{id}")]
        public async Task<ActionResult<List<UserSalesOrders>>> GetUpcomingSalesOrdersByUserId(int id)
        {
            if (id <= 0)
                return BadRequest();
            var result = await _dbAccess.GetUpcomingSalesOrdersForUser(id);
            if (result != null)
            {
                Console.WriteLine("retrieved orders");
                return result;
            }
            else
                return StatusCode(500, "Failed to delete sales order.");
        }
        // [HttpPut("{id}")]
        // public async Task<IActionResult> Update(int id, [FromBody] SalesOrder order)
        // {
        //     if (order == null || id != order.OrderId)
        //         return BadRequest();

        //     var result = await _dbAccess.UpdateSalesOrder(order);
        //     if (result)
        //         return Ok("Sales order updated.");
        //     return StatusCode(500, "Failed to update sales order.");
        // }

        // [HttpDelete("{id}")]
        // public async Task<IActionResult> Delete(int id)
        // {
        //     var result = await _dbAccess.DeleteSalesOrder(id);
        //     if (result)
        //         return Ok("Sales order deleted.");
        //     return StatusCode(500, "Failed to delete sales order.");
        // }
    }
}