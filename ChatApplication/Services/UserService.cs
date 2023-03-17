﻿using Azure;
using ChatApplication.Data;
using ChatApplication.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Formats.Tar;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using static Google.Apis.Requests.BatchRequest;
using Response = ChatApplication.Models.Response;

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
            Response response = new Response();
            if (user.FirstName == null) {
                response.StatusCode = 400;
                response.Message = "FirstName cannot be empty";
                response.Data = null;
                return response;
            }
            if (user.LastName == null)
            {
                response.StatusCode = 400;
                response.Message = "LastName cannot be empty";
                response.Data = null;
                return response;
            }
            
            TimeSpan ageTimeSpan = DateTime.Now - user.DateOfBirth;
            int age = (int)(ageTimeSpan.Days / 365.25);                 
            if (age < 12)
            {
                response.StatusCode = 400;
                response.Message = "You should be atleast 12 years old";
                response.Data = null;
                return response;
            }
            
            string regexPatternEmail = "^[a-z0-9._%+-]+@[a-z0-9.-]+\\.[a-z]{2,4}$";
            if (!Regex.IsMatch(user.Email, regexPatternEmail))
            {
                response.StatusCode = 400;
                response.Message = "Enter Valid email";
                response.Data = null;
                return response;
            }
            
            string regexPatternPassword = "^(?=.*?[A-Z])(?=.*?[a-z])(?=.*?[0-9])(?=.*?[#?!@$%^&*-]).{8,}$";
            if (!Regex.IsMatch(user.Password, regexPatternPassword))
            {
                response.StatusCode = 400;
                response.Message = "\"Password should contains 8 Characters atleast one Upper, lower alphabet and one special symbol";
                response.Data = null;
                return response;
            }
            
            string regexPatternPhone = "^[6-9]\\d{9}$";
            if (!Regex.IsMatch(user.Phone.ToString(), regexPatternPhone))
            {
                response.StatusCode = 400;
                response.Message = "Enter Valid Phone Number";
                response.Data = null;
                return response;
            }
            
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
                    DateOfBirth = DateTime.Parse(user.DateOfBirth.ToString("yyyy-MM-dd"))
                 };
                 _dbContext.Users.Add(obj);
                 _dbContext.SaveChanges();
                 string token = CreateToken(obj, _configuration);
                 int len = obj == null ? 0 : 1;
                
                 if (len == 0)
                 {
                    response.Data = null;
                    response.StatusCode = 404;
                    response.Message = "cannot Add The User";
                    return response;
                 }
                 if (len == 1)
                 {
                    DataModel dm = new DataModel();
                    dm.Email = obj.Email;
                    dm.Token = token;
                    response.Data = dm;                   
                    response.StatusCode = 200;
                    response.Message = "User Added Successfully";
                 }
                 return response;
            }
            response.Data = null;
            response.StatusCode = 409;
            response.Message = "Email Already Exists";
            return response;

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

            DataModel dm = new DataModel();
            dm.Email = obj.Email;
            dm.Token = token;
            response.Data = dm;
            response.StatusCode = 200;
            response.Message = "Login Successfull";
            return response;
        }
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
        private bool VerifyPasswordHash(string Password, byte[] PasswordHash, byte[] PasswordSalt)
        {
            using (var hmac = new HMACSHA512(PasswordSalt))
            {
                byte[] computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(Password));
                return computedHash.SequenceEqual(PasswordHash);
            }
        }
        /*
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
        */
        public Response UpdateUser(UpdateUser update,string email)
        {
            var obj = _dbContext.Users.FirstOrDefault(x => x.Email == email);
            int len = obj == null ? 0 : 1;
            Response res = new Response();
            if (len == 0)
            {
                res.StatusCode = 404;
                res.Message = "Not Found";
                res.Data = null;
                return res;
            }
            if (update.FirstName != null ) { obj.FirstName = update.FirstName; }
            if (update.LastName != null ) { obj.LastName = update.LastName; }
            if (update.Phone != 0) {
                string regexPatternPhone = "^[6-9]\\d{9}$";
                if (!Regex.IsMatch(update.Phone.ToString(), regexPatternPhone))
                {
                    res.StatusCode = 400;
                    res.Message = "Enter Valid Phone Number";
                    res.Data = null;
                    return res;
                }
                obj.Phone = update.Phone; 
            }           
            if (update.Email != null ) {
                string regexPatternEmail = "^[a-z0-9._%+-]+@[a-z0-9.-]+\\.[a-z]{2,4}$";
                if (!Regex.IsMatch(update.Email, regexPatternEmail))
                {
                    res.StatusCode = 400;
                    res.Message = "Enter Valid email";
                    res.Data = null;
                    return res;
                }
                obj.Email = update.Email; 
            }            
            if (update.DateOfBirth.Year <= 2011) {
                TimeSpan ageTimeSpan = DateTime.Now - update.DateOfBirth;
                int age = (int)(ageTimeSpan.Days / 365.25);
                if (age < 12)
                {
                    res.StatusCode = 400;
                    res.Message = "You should be atleast 12 years old";
                    res.Data = null;
                    return res;
                }
                obj.DateOfBirth = DateTime.Parse(update.DateOfBirth.ToString("yyyy-MM-dd")); 
            }
            
            obj.UpdatedAt = DateTime.Now;
            _dbContext.SaveChanges();
            res.Data = obj;
            res.StatusCode = 200;
            res.Message = "User details updated";
            return res;    
        }
        public Response DeleteUser(Guid UserId)
        {
            var obj = _dbContext.Users.Find(UserId);
            int len = obj == null ? 0 : 1;
            Response response = new Response();
            if (len == 0)
            {
                response.Data = null;
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
        public Response UploadProfileImage(FileUpload upload, string email)
        {
            var obj = _dbContext.Users.FirstOrDefault(x => x.Email == email);
            int len = obj == null ? 0 : 1;
            Response response = new Response();
            if (len == 0)
            {
                response.Data = null;
                response.StatusCode = 404;
                response.Message = "User Not Found";
                return response;
            }
            var fileName = Path.GetFileNameWithoutExtension(upload.File.FileName);
            var fileExt = Path.GetExtension(upload.File.FileName);
            var uniqueFileName = $"{fileName}_{DateTime.UtcNow.ToString("yyyyMMddHHmmssfff")}{fileExt}";
            string imagePath = Path.Combine("wwwroot", "Images", uniqueFileName);
            string path = Path.Combine(Directory.GetCurrentDirectory(), imagePath);           
            obj.PathToProfilePic = imagePath;
            var filestream = System.IO.File.Create(path);
            upload.File.CopyTo(filestream);
            filestream.Close();
            _dbContext.SaveChanges();
            response.Data = obj;
            response.StatusCode = 200;
            response.Message = "Image Uploaded Successfully..";
            return response;

        }
        public Response ChangePassword(ChangePassword pass, string email)
        {
            Response response = new Response();
            string regexPatternPassword = "^(?=.*?[A-Z])(?=.*?[a-z])(?=.*?[0-9])(?=.*?[#?!@$%^&*-]).{8,}$";
            if (!Regex.IsMatch(pass.NewPassword, regexPatternPassword))
            {
                response.StatusCode = 400;
                response.Message = "Password should be of 8 length contains atleast one Upper, lower alphabet and one special symbol ";
                response.Data = null;
                return response;
            }
            var obj = _dbContext.Users.FirstOrDefault(x => x.Email == email);
            int len = obj == null ? 0 : 1;
            
            if (len == 0)
            {
                response.Data = obj;
                response.StatusCode = 404;
                response.Message = "User Not Found";
                return response;
            }
            if (!VerifyPasswordHash(pass.OldPassword, obj.PasswordHash, obj.PasswordSalt))
            {
                response.Data = null;
                response.StatusCode = 404;
                response.Message = "OldPassword is Wrong";
                return response;
            }

            if (pass.NewPassword != pass.ConfirmPassword)
            {
                response.Data = null;
                response.StatusCode = 404;
                response.Message = "ConfirmPassword doesn't match the New password";
                return response;
            }
            CreatePasswordHash(pass.ConfirmPassword, out byte[] PasswordHash, out byte[] PasswordSalt);
            obj.PasswordHash = PasswordHash;
            obj.PasswordSalt = PasswordSalt;
            obj.UpdatedAt = DateTime.UtcNow;
            _dbContext.SaveChanges();
            
            response.Data = obj;
            response.StatusCode = 200;
            response.Message = "Password Changed Successfully";
            return response;
        }
        public Response Search(string Name, string email)
        {    
            Response response = new Response();
            var users = _dbContext.Users.Where(u => (u.FirstName + " " + u.LastName).Contains(Name));
            var obj = _dbContext.Users.FirstOrDefault(x => x.Email == email);

            
            if(users == null || users.Count() == 0) {
                response.Data = null;
                response.StatusCode = 404;
                response.Message = "No User found with this Name";
                return response; 
            }
            var ExceptloginedUser = users.Where(u => u.UserId != obj.UserId).Select(u => new { u.UserId, u.FirstName, u.LastName, u.Email });
            if (ExceptloginedUser.Count() == 0)
            {
                response.Data = null;
                response.StatusCode = 404;
                response.Message = "No User found with this Name";
                return response;
            }
            response.Data = ExceptloginedUser;
            response.StatusCode = 200;
            response.Message = "List Of Users";
            return response;
        }
        public Response UploadImage(FileUpload upload, string email)
        {
            var obj = _dbContext.Users.FirstOrDefault(x => x.Email == email);
            int len = obj == null ? 0 : 1;
            Response response = new Response();
            if (len == 0)
            {
                response.Data = null;
                response.StatusCode = 404;
                response.Message = "User Not Found";
                return response;
            }
            var fileName = Path.GetFileNameWithoutExtension(upload.File.FileName);
            var fileExt = Path.GetExtension(upload.File.FileName);
            var uniqueFileName = $"{fileName}_{DateTime.UtcNow.ToString("yyyyMMddHHmmssfff")}{fileExt}";
            string imagePath = Path.Combine("wwwroot", "Images", uniqueFileName);
            string path = Path.Combine(Directory.GetCurrentDirectory(), imagePath);

            var filestream = System.IO.File.Create(path);
            upload.File.CopyTo(filestream);
            filestream.Close();
            response.Data = imagePath;
            response.StatusCode = 200;
            response.Message = "Image Uploaded Successfully..";
            return response;
        }
        public Response UploadFile(FileUpload upload, string email)
        {
            var obj = _dbContext.Users.FirstOrDefault(x => x.Email == email);
            int len = obj == null ? 0 : 1;
            Response response = new Response();
            if (len == 0)
            {
                response.Data = null;
                response.StatusCode = 404;
                response.Message = "User Not Found";
                return response;
            }
            var fileName = Path.GetFileNameWithoutExtension(upload.File.FileName);
            var fileExt = Path.GetExtension(upload.File.FileName);
            var uniqueFileName = $"{fileName}_{DateTime.UtcNow.ToString("yyyyMMddHHmmssfff")}{fileExt}";
            string filePath = Path.Combine("wwwroot", "Files", uniqueFileName);
            string path = Path.Combine(Directory.GetCurrentDirectory(), filePath);

            var filestream = System.IO.File.Create(path);
            upload.File.CopyTo(filestream);
            filestream.Close();
            response.Data = filePath;
            response.StatusCode = 200;
            response.Message = "File Uploaded Successfully..";
            return response;
        }
    }
}
