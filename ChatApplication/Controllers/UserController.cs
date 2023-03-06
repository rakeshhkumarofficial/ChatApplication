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

        [HttpGet]
        public IActionResult Login(string Email, string Password)
        {
            IUserService service = new UserService(_dbContext, _configuration);
            var token = service.Login(Email, Password, _configuration);
            return Ok(token);
        }


    }
}
