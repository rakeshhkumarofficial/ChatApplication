using ChatApplication.Data;
using ChatApplication.Models;
using ChatApplication.Services;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using System.Data;

namespace ChatApplication.Controllers
{
    [Route("api/[controller]/[Action]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly ChatAPIDbContext _dbContext;
        public readonly IConfiguration _configuration;
        private readonly IUserService service;
        public UserController(ChatAPIDbContext dbContext, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _configuration = configuration;
            service = new UserService(_dbContext, _configuration);
        }

        // Register New User
        [HttpPost]
        public IActionResult Register(Register user)
        {           
            var res = service.AddUser(user);
            return Ok(res);
        }

        // Login User
        [HttpPost]
        public IActionResult Login(Login login)
        {
            var token = service.Login(login, _configuration);
            return Ok(token);
        }

        // Update Profile of User
        [HttpPost, Authorize(Roles = "Login")]
        public IActionResult Update(UpdateUser update)
        {
            var user = HttpContext.User;
            var email = user.FindFirst(ClaimTypes.Name)?.Value;
            var res = service.UpdateUser(update,email);
            return Ok(res);
        }

        // Delete Account of User

        [HttpDelete, Authorize(Roles = "Login")]
        public IActionResult Delete()
        {
            var user = HttpContext.User;
            var email = user.FindFirst(ClaimTypes.Name)?.Value;
            var res = service.DeleteUser(email);
            return Ok(res);
        }

        // Change Password of the User
        [HttpPost, Authorize(Roles = "Login")]
        public IActionResult ChangePassword(ChangePassword pass)
        {
            var user = HttpContext.User;
            var email = user.FindFirst(ClaimTypes.Name)?.Value;
            var res = service.ChangePassword(pass,email);
            return Ok(res);
        }

        // Search Available Users
        [HttpGet, Authorize(Roles = "Login")]
        public IActionResult Search(string Name)
        {
            var user = HttpContext.User;
            var email = user.FindFirst(ClaimTypes.Name)?.Value;
            var res = service.Search(Name,email);
            return Ok(res);
        }      
    }
}
