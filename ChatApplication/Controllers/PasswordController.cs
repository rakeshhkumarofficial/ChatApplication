using ChatApplication.Data;
using ChatApplication.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ChatApplication.Services;

namespace ChatApplication.Controllers
{
    [Route("api/[controller]/[Action]")]
    [ApiController]
    public class PasswordController : ControllerBase
    {
        private readonly ChatAPIDbContext _dbContext;
        public readonly IConfiguration _configuration; 
        private readonly IPasswordService _passwordService;
        private readonly ILogger<PasswordController> _logger;
        public PasswordController(ChatAPIDbContext dbContext, IConfiguration configuration, ILogger<PasswordController> logger)
        {
            _dbContext = dbContext;
            _configuration = configuration;
            _logger = logger;
            _passwordService = new PasswordService(_dbContext,_configuration);
            
        }

        [HttpPost]
        public IActionResult ForgetPassword(ForgetPasswordRequest fp)
        {
            _logger.LogInformation("\nExecuting method {MethodName}\n", nameof(ForgetPassword));
            var res = _passwordService.ForgetPassword(fp);
            return Ok(res);
        }

        [HttpPost, Authorize(Roles = "Reset")]
        public IActionResult ResetPassword(ResetPassword reset)
        {
            _logger.LogInformation("\nExecuting method {MethodName}\n", nameof(ResetPassword));
            var user = HttpContext.User;
            var email = user.FindFirst(ClaimTypes.Name)?.Value;
            var res = _passwordService.ResetPassword(reset, email);
            return Ok(res);
        }
     }
}
