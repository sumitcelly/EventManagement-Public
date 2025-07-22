using EventManagementDbAccess;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CreateTicketApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserController : ControllerBase
    {
        private readonly ILogger<UserController> _logger;
        private readonly UserDbAccess _userDbAccess;

        public UserController(ILogger<UserController> logger, UserDbAccess userDbAccess)
        {
            _logger = logger;
            _userDbAccess = userDbAccess;
        }

        [HttpGet("{email}")]
        public async Task<ActionResult<EventUser>> GetUserByEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
                return BadRequest("Email is null or empty.");
            var user = await _userDbAccess.GetUserByEmail(email);
            if (user == null)
                return NotFound();
            return user;
        }

      

        [HttpPost]
        public async Task<ActionResult<int>> Create([FromBody] EventUser user)
        {
            if (user == null)
                return BadRequest("Invalid user.");
            var id = await _userDbAccess.CreateUser(user);
            if (id > 0)
                return Ok(id);
            return StatusCode(500, "Failed to create user.");
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] EventUser user)
        {
            if (user == null || id != user.UserId)
                return BadRequest("Invalid user or ID mismatch.");
            var result = await _userDbAccess.UpdateUser(user);
            if (result)
                return Ok();
            return StatusCode(500, "Failed to update user.");
        }

        [HttpDelete("{email}")]
        public async Task<IActionResult> Delete(string email)
        {
            if (string.IsNullOrEmpty(email))
                return BadRequest("Invalid user ID.");
            var result = await _userDbAccess.DeleteUserByEmail(email);
            if (result)
                return Ok();
            return StatusCode(500, "Failed to delete user.");
        }
    }
}