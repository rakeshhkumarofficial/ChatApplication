using ChatApplication.Data;
using ChatApplication.Models;
using ChatApplication.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace ChatApplication.Controllers
{
    [Route("api/[controller]/[Action]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly ChatAPIDbContext _dbContext;
        public readonly IConfiguration _configuration;
        private readonly IUserService service;
        private readonly ILogger<UserController> _logger;

       
        public UserController(ChatAPIDbContext dbContext, IConfiguration configuration, ILogger<UserController> logger)
        {
            _dbContext = dbContext;
            _configuration = configuration;
            _logger = logger;
            service = new UserService(_dbContext, _configuration);
        }

        // Register New User
        [HttpPost]
        public IActionResult Register(Register user)
        {
            _logger.LogInformation("\nExecuting method {MethodName}\n", nameof(Register));
            var res = service.AddUser(user);        
            return Ok(res);
        }

        // Login User
        [HttpPost]
        public IActionResult Login(Login login)
        {
            _logger.LogInformation("\nExecuting method {MethodName}\n", nameof(Login));
            var token = service.Login(login, _configuration);
            return Ok(token);
        }

        // Update Profile of User
        [HttpPut, Authorize(Roles = "Login")]
        public IActionResult Update(UpdateUser update)
        {
            _logger.LogInformation("\nExecuting method {MethodName}\n", nameof(Update));
            var user = HttpContext.User;
            var email = user.FindFirst(ClaimTypes.Name)?.Value;
            var res = service.UpdateUser(update,email);
            return Ok(res);
        }

        // Delete Account of User

        [HttpDelete, Authorize(Roles = "Login")]
        public IActionResult Delete()
        {
            _logger.LogInformation("\nExecuting method {MethodName}\n", nameof(Delete));
            var user = HttpContext.User;
            var email = user.FindFirst(ClaimTypes.Name)?.Value;
            var res = service.DeleteUser(email);
            return Ok(res);
        }

        // Change Password of the User
        [HttpPut, Authorize(Roles = "Login")]
        public IActionResult ChangePassword(ChangePassword pass)
        {
            _logger.LogInformation("\nExecuting method {MethodName}\n", nameof(ChangePassword));
            var user = HttpContext.User;
            var email = user.FindFirst(ClaimTypes.Name)?.Value;
            var res = service.ChangePassword(pass,email);
            return Ok(res);
        }

        // Search Available Users
        [HttpGet, Authorize(Roles = "Login")]
        public IActionResult Search(string Name)
        {
            _logger.LogInformation("\nExecuting method {MethodName}\n", nameof(Search));
            var user = HttpContext.User;
            var email = user.FindFirst(ClaimTypes.Name)?.Value;
            var res = service.Search(Name,email);
            return Ok(res);
        }
        [HttpGet, Authorize(Roles = "Login")]
        public IActionResult GetUser()
        {
            _logger.LogInformation("\nExecuting method {MethodName}\n", nameof(GetUser));
            var user = HttpContext.User;
            var email = user.FindFirst(ClaimTypes.Name)?.Value;
            var res = service.GetUser(email);
            return Ok(res);
        }
    }
}
