using Microsoft.AspNetCore.Mvc;
using CreateTicketApi.BusinessLogic;
using EventManagementDbAccess;
using Microsoft.AspNetCore.Authorization;

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

            var result = await _salesOrderConductor.CreateSalesOrder(order);
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