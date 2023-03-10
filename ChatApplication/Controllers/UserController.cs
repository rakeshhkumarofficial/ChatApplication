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

namespace ChatApplication.Controllers
{
    [Route("api/[controller]/[Action]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly ChatAPIDbContext _dbContext;
        public readonly IConfiguration _configuration;
        public UserController(ChatAPIDbContext dbContext, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _configuration = configuration;
        }
        
        [HttpPost]
        public IActionResult Register(Register user)
        {
            
            IUserService service = new UserService(_dbContext,_configuration);
            var res = service.AddUser(user);
            return Ok(res);
        }

        [HttpPost]
        public IActionResult Login(Login login)
        {
            IUserService service = new UserService(_dbContext, _configuration);
            var token = service.Login(login, _configuration);
            return Ok(token);
        }

        [HttpGet, Authorize(Roles ="Login")]
        public IActionResult GetUser(Guid UserId, string? FirstName, string? LastName, long Phone, int sort, int pageNumber, int records)
        {
            IUserService service = new UserService(_dbContext, _configuration);
            var res = service.GetUser(UserId, FirstName,LastName, Phone,sort, pageNumber, records);
            return Ok(res);
        }
        [HttpPost, Authorize(Roles = "Login")]
        public IActionResult UpdateUser(UpdateUser update)
        {
            var user = HttpContext.User;
            var email = user.FindFirst(ClaimTypes.Name)?.Value;
            IUserService service = new UserService(_dbContext,_configuration);
            var res = service.UpdateUser(update,email);
            return Ok(res);
        }

        [HttpDelete, Authorize(Roles = "Login")]
        public IActionResult DeleteUser(Guid UserId)
        {
            IUserService service = new UserService(_dbContext, _configuration);
            var res = service.DeleteUser(UserId);
            return Ok(res);
        }

        [HttpPost, Authorize(Roles = "Login")]
        public IActionResult FileUpload([FromForm] FileUpload upload)
        {
            var user = HttpContext.User;
            var email = user.FindFirst(ClaimTypes.Name)?.Value;
            IUserService service = new UserService(_dbContext, _configuration);
            var res = service.UploadProfileImage(upload,email);
            return Ok(res);
        }

        [HttpPost, Authorize(Roles = "Login")]
        public IActionResult ChangePassword(ChangePassword pass)
        {
            var user = HttpContext.User;
            var email = user.FindFirst(ClaimTypes.Name)?.Value;
            IUserService service = new UserService(_dbContext, _configuration);
            var res = service.ChangePassword(pass,email);
            return Ok(res);
        }
    }
}
