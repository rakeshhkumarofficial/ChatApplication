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

        [HttpPost]
        public IActionResult Login(Login login)
        {
            IUserService service = new UserService(_dbContext, _configuration);
            var token = service.Login(login, _configuration);
            return Ok(token);
        }

        [HttpGet]
        public IActionResult GetUser(Guid UserId, string? FirstName, string? LastName, long Phone, int sort, int pageNumber, int records)
        {
            IUserService service = new UserService(_dbContext, _configuration);
            var res = service.GetUser(UserId, FirstName,LastName, Phone,sort, pageNumber, records);
            return Ok(res);
        }

    }
}
