using ChatApplication.Data;
using ChatApplication.Models;
using ChatApplication.Services;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ChatApplication.Controllers
{
    [Route("api/[controller]")]
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
        [Route("GoogleAuth")]
        public async Task<IActionResult> SignIn(string Token)
        {
            var googleUser = await GoogleJsonWebSignature.ValidateAsync(Token);
            var user = new GoogleUser();
            user.Email = googleUser.Email;
            user.Password = null;
            string token = CreateToken(user, _configuration);
            var response = new Response();
            response.StatusCode = 200;
            response.Message = "Token";
            response.Data = token;
            return Ok(response);
        }
        private string CreateToken(GoogleUser obj, IConfiguration _configuration)
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
