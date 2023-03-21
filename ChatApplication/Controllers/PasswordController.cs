using ChatApplication.Data;
using ChatApplication.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Web;

using NETCore.MailKit.Core;
using NETCore.MailKit.Infrastructure.Internal;

using System.Net.Mail;
using System.Net;
using static System.Net.WebRequestMethods;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using System.Data;
using Newtonsoft.Json.Linq;
using Azure;
using System.Text.RegularExpressions;
using Response = ChatApplication.Models.Response;
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
        public PasswordController(ChatAPIDbContext dbContext, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _configuration = configuration;
            _passwordService = new PasswordService(_dbContext,_configuration);
            
        }

        [HttpPost]
        public IActionResult ForgetPassword(ForgetPasswordRequest fp)
        {
            var res = _passwordService.ForgetPassword(fp);
            return Ok(res);
        }

        [HttpPost, Authorize(Roles = "Reset")]
        public IActionResult ResetPassword(ResetPassword reset)
        {
            var user = HttpContext.User;
            var email = user.FindFirst(ClaimTypes.Name)?.Value;
            var res = _passwordService.ResetPassword(reset, email);
            return Ok(res);
        }

     }
}
