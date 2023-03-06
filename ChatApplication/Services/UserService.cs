using ChatApplication.Data;
using ChatApplication.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ChatApplication.Services
{
    public class UserService : IUserService
    {
        private readonly ChatAPIDbContext _dbContext;
        private readonly IConfiguration _configuration;

        public UserService(ChatAPIDbContext dbContext, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _configuration = configuration;
        }
        public Response AddUser(Register user)
        {
            bool IsUserExists = _dbContext.Users.Where(u=>u.Email == user.Email).Any();
            if (!IsUserExists)
            {
                CreatePasswordHash(user.Password, out byte[] PasswordHash, out byte[] PasswordSalt);
                var obj = new User()
                {
                    UserId = Guid.NewGuid(),
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    PasswordHash = PasswordHash,
                    PasswordSalt = PasswordSalt,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Phone = user.Phone,
                    DateOfBirth = user.DateOfBirth,
                };
                _dbContext.Users.Add(obj);
                _dbContext.SaveChanges();
                int len = obj == null ? 0 : 1;
                Response response = new Response();
                if (len == 0)
                {
                    response.Data = obj;
                    response.StatusCode = 404;
                    response.Message = "cannot Add The User";
                    return response;
                }
                if (len == 1)
                {
                    response.Data = obj;
                    response.StatusCode = 200;
                    response.Message = "User Added Successfully";
                }
                return response;
            }

            Response res = new Response();
            res.Data = null;
            res.StatusCode = 409;
            res.Message = "Email Already Exists";
            return res;

        }
        private void CreatePasswordHash(string Password, out byte[] PasswordHash, out byte[] PasswordSalt)
        {
            using (var hmac = new HMACSHA512())
            {
                PasswordSalt = hmac.Key;
                PasswordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(Password));
            }
        }
        public Response Login(string Email, string Password, IConfiguration _configuration)
        {
            
            var obj = _dbContext.Users.Where(u=>u.Email == Email).FirstOrDefault();
            Response response = new Response();
            if (obj == null)
            {
                response.Data = null;
                response.StatusCode = 404;
                response.Message = "Wrong Email";
                return response;
            }
            if (!VerifyPasswordHash(Password, obj.PasswordHash,obj.PasswordSalt))
            {
                response.Data = null;
                response.StatusCode = 404;
                response.Message = "Wrong Password";
                return response;
            }
            string token = CreateToken(obj, _configuration);
           
            response.Data = token;
            response.StatusCode = 200;
            response.Message = "Token";
            return response;
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
        private bool VerifyPasswordHash(string Password, byte[] PasswordHash, byte[] PasswordSalt)
        {
            using (var hmac = new HMACSHA512(PasswordSalt))
            {
                byte[] computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(Password));
                return computedHash.SequenceEqual(PasswordHash);
            }
        }
       
    }
}
