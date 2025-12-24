using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using radmerceBot.Api.Data;
using radmerceBot.Api.Enums;
using radmerceBot.Api.Models;
using radmerceBot.Core.Models;

namespace radmerceBot.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SuperUserController : ControllerBase
    {
        private readonly AppDbContext _db;

        public SuperUserController(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Add existing user as SuperUser by phone number
        /// </summary>
        /// <param name="phone">User's phone number</param>
        [HttpPost("add")]
        public async Task<IActionResult> AddSuperUser([FromQuery] string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return BadRequest("Phone number is required.");

            var user = await _db.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phone);
            if (user == null)
                return NotFound("User with this phone number does not exist.");

            var existingSuper = await _db.SuperUsers.FirstOrDefaultAsync(s => s.TelegramUserId == user.TelegramUserId);
            if (existingSuper != null)
                return BadRequest("This user is already a super user.");

            var superUser = new SuperUser
            {
                TelegramUserId = user.TelegramUserId,
                PhoneNumber = user.PhoneNumber!,
                CreatedAt = DateTime.UtcNow,
                FirstName = user.FirstName,
                LastName = user.LastName,
                IsActive = true,
                State = SuperUserState.None
            };

            _db.SuperUsers.Add(superUser);
            await _db.SaveChangesAsync();

            return Ok(new
            {
                Message = "Super user added successfully.",
                SuperUser = superUser
            });
        }
    }
}
