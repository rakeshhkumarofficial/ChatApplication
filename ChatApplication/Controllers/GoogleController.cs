﻿using ChatApplication.Data;
using ChatApplication.Models;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ChatApplication.Controllers
{
    [Route("api/[controller]/[Action]")]
    [ApiController]
    public class GoogleController : ControllerBase
    {
        private readonly ChatAPIDbContext _dbContext;
        public readonly IConfiguration _configuration;
        private readonly ILogger<GoogleController> _logger;
        public GoogleController(ChatAPIDbContext dbContext, IConfiguration configuration, ILogger<GoogleController> logger)
        {
            _dbContext = dbContext;
            _configuration = configuration;
            _logger = logger;
        }
      
        // SignIn With Google
        [HttpPost]
        public async Task<IActionResult> SignIn(string Token)
        {
            _logger.LogInformation("\nExecuting method {MethodName}\n", nameof(SignIn));
            var googleUser = await GoogleJsonWebSignature.ValidateAsync(Token);
            bool IsUserExists = _dbContext.Users.Where(u => u.Email == googleUser.Email).Any();
            DataModel model = new DataModel();
            if (!IsUserExists)
            {
                var user = new User()
                {
                    UserId = Guid.NewGuid(),
                    FirstName = googleUser.GivenName,
                    LastName = googleUser.FamilyName,
                    PasswordHash = null,
                    PasswordSalt = null,
                    Email = googleUser.Email,
                    ProfilePic = null,
                    DateOfBirth = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                };
                _dbContext.Users.Add(user);
                _dbContext.SaveChanges();
            
                string Newtoken = CreateToken(user, _configuration);
                Response response = new Response();
                response.StatusCode = 200;
                response.Message = "Login Successfull";
                
                model.Email = googleUser.Email;
                model.Token = Newtoken;
                response.Data = model;
                return Ok(response);
            }
            var ExistingUser = _dbContext.Users.Where(u => u.Email == googleUser.Email).FirstOrDefault();
            string token = CreateToken(ExistingUser, _configuration);
            Response res = new Response();
            res.StatusCode = 200;
            res.Message = "Login Successfull";
            model.Email = googleUser.Email;
            model.Token = token;
            res.Data = model;
            return Ok(res);
        }

        // Creating JWT Token for Google User
        private string CreateToken(User obj, IConfiguration _configuration)
        {
            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name,obj.Email),
                 new Claim(ClaimTypes.Role,"Login")

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
