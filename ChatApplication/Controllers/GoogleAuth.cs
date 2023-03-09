using ChatApplication.Data;
using ChatApplication.Models;
using ChatApplication.Services;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ChatApplication.Controllers
{
    [Route("api/[controller]/[Action]")]
    [ApiController]
    public class GoogleAuth : ControllerBase
    {
        private readonly ChatAPIDbContext _dbContext;
        public readonly IConfiguration _configuration;
        public GoogleAuth(ChatAPIDbContext dbContext, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _configuration = configuration;

        }

        [HttpPost]
        public async Task<IActionResult> SignIn(string Token)
        {
            var googleUser = await GoogleJsonWebSignature.ValidateAsync(Token);
            bool IsUserExists = _dbContext.Users.Where(u => u.Email == googleUser.Email).Any();
            if (!IsUserExists)
            {
                var user = new User()
                {
                    UserId = Guid.NewGuid(),
                    FirstName = googleUser.Name,
                    LastName = googleUser.FamilyName,
                    Email = googleUser.Email,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                };
                _dbContext.Users.Add(user);
                _dbContext.SaveChanges();
            
            string Newtoken = CreateToken(user, _configuration);
            Response response = new Response();
            response.StatusCode = 200;
            response.Message = "Token";
            response.Data = Newtoken;
            return Ok(response);
            }
            var ExistingUser = _dbContext.Users.Where(u => u.Email == googleUser.Email).FirstOrDefault();
            string token = CreateToken(ExistingUser, _configuration);
            Response res = new Response();
            res.StatusCode = 200;
            res.Message = "Token";
            res.Data = token;
            return Ok(res);
        }
        private string CreateToken(User obj, IConfiguration _configuration)
        {
            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name,obj.Email)
           };
            var Key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_configuration.GetSection("AppSettings:Token").Value));
            var creds = new SigningCredentials(Key, SecurityAlgorithms.HmacSha512Signature);
            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: creds
            );
            var jwt = new JwtSecurityTokenHandler().WriteToken(token);
            return jwt;
        }
    }
}
