using Microsoft.AspNetCore.Mvc;
using CreateTicketApi.BusinessLogic;

namespace CreateTicketApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SalesOrderController : ControllerBase
    {
        private readonly SalesOrderConductor _salesOrderConductor;

        public SalesOrderController(SalesOrderConductor salesOrderConductor)
        {
            _salesOrderConductor = salesOrderConductor;
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

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
             if (id <=0)
                return BadRequest("Id is null.");
            var order = await _salesOrderConductor.GetSalesOrderById(id);
            if (order == null)
                return NotFound();
            return Ok(order);
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