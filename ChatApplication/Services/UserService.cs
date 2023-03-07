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
using System.Xml;

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

        // Register User
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
                string token = CreateToken(obj, _configuration);
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
                    response.Data = token;
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
        public Response Login(Login login, IConfiguration _configuration)
        {
            
            var obj = _dbContext.Users.Where(u=>u.Email == login.Email).FirstOrDefault();
            Response response = new Response();
            if (obj == null)
            {
                response.Data = null;
                response.StatusCode = 404;
                response.Message = "Wrong Email";
                return response;
            }
            if (!VerifyPasswordHash(login.Password, obj.PasswordHash,obj.PasswordSalt))
            {
                response.Data = null;
                response.StatusCode = 404;
                response.Message = "Wrong Password";
                return response;
            }
            string token = CreateToken(obj, _configuration);
           
            response.Data = token;
            response.StatusCode = 200;
            response.Message = "Login Successfully";
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

        // Get User
        public Response GetUser(Guid UserId, string? FirstName, string? LastName, long Phone, int sort, int pageNumber, int records)
        {
            var users = _dbContext.Users;
            var Userquery = _dbContext.Users.AsQueryable();
            var userlist = from u in Userquery where u.IsDeleted == false select  new { u.UserId,u.FirstName,u.LastName,u.Phone,u.DateOfBirth,u.Email};
            Response res = new Response();
            res.StatusCode = 200;
            res.Message = "User Details";
            if (UserId == Guid.Empty && FirstName == null && LastName == null && Phone == 0 )
            {
                if (sort == -1)
                {
                    var sortDesc = from u in Userquery where u.IsDeleted == false orderby u.FirstName descending select new { u.UserId, u.FirstName, u.LastName, u.Phone, u.DateOfBirth, u.Email };
                    res.Data = sortDesc;
                    return res;
                }
                if (sort == 1)
                {
                    var sortAsc = from u in Userquery where u.IsDeleted == false orderby u.FirstName select new { u.UserId, u.FirstName, u.LastName, u.Phone, u.DateOfBirth, u.Email };
                    res.Data = sortAsc;
                    return res;
                }
                if (pageNumber != 0 && records != 0)
                {
                    var pageRecords = (userlist.Skip((pageNumber - 1) * records).Take(records));
                    res.Data = pageRecords;
                    if (pageRecords != null)
                    {
                        return res;
                    }
                }
                res.Data = userlist;
                return res;
            }
            var obj = from u in Userquery where u.IsDeleted == false && (u.UserId == UserId || UserId == Guid.Empty) && (u.FirstName == FirstName || FirstName == null) && (u.LastName == LastName || LastName == null) && (u.Phone == Phone || Phone == 0) select new { u.UserId, u.FirstName, u.LastName, u.Phone, u.DateOfBirth, u.Email };
            if (sort == -1)
            {
                var sortDesc = from u in obj orderby u.FirstName descending select u;
                res.Data = sortDesc;
                return res;
            }
            if (sort == 1)
            {
                var sortAsc = from u in obj orderby u.FirstName select u;
                res.Data = sortAsc;
                return res;
            }
            res.Data = obj;
            int len = obj.Count();
            if (len == 0)
            {
                res.StatusCode = 404;
                res.Message = "Not Found";
            }
            return res;
        }

        // Update User
        public Response UpdateUser(Guid UserId, UpdateUser update)
        {
            var obj = _dbContext.Users.Find(UserId);
            int len = obj == null ? 0 : 1;
            Response res = new Response();
            if (len == 0)
            {
                res.StatusCode = 404;
                res.Message = "Not Found";
                res.Data = obj;
                return res;
            }

            if (update.FirstName != null ) { obj.FirstName = update.FirstName; }
            if (update.LastName != null ) { obj.LastName = update.LastName; }
            if (update.Phone != 0) { obj.Phone = update.Phone; }
            if (update.Email != null ) { obj.Email = update.Email; }
            if (update.DateOfBirth != DateTime.Now) { obj.DateOfBirth = update.DateOfBirth; }
            obj.UpdatedAt = DateTime.Now;

            _dbContext.SaveChanges(); 
             res.Data = obj;
             res.StatusCode = 200;
             res.Message = "User details updated";
            return res;    
        }

        // Delete User
        public Response DeleteUser(Guid UserId)
        {
            var obj = _dbContext.Users.Find(UserId);
            int len = obj == null ? 0 : 1;
            Response response = new Response();
            if (len == 0)
            {
                response.Data = obj;
                response.StatusCode = 404;
                response.Message = "Not Found";
                return response;
            }
            if (obj != null)
            {
                _dbContext.Remove(obj);
                _dbContext.SaveChanges();
                if (len == 1)
                {
                    response.Data = obj;
                    response.StatusCode = 200;
                    response.Message = "User deleted";
                }
            }
            return response;

        }

    }
}
