﻿using ChatApplication.Data;
using ChatApplication.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
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

        // Register New User
        public Response AddUser(Register user)
        {
            Response response = new Response();
            response.Data = null;
            response.StatusCode = 409;
            response.Message = "Email Already Exists";
            if (user.FirstName == null || user.FirstName=="") 
            {
                response.StatusCode = 400;
                response.Message = "FirstName cannot be empty";
                return response;
            }
            if (user.LastName == null || user.LastName=="")
            {
                response.StatusCode = 400;
                response.Message = "LastName cannot be empty";
                return response;
            }        
            /*if( user.Gender != 1 || user.Gender != 2 )
            {
                response.StatusCode = 400;
                response.Message = "Provide the Gender";
                return response;
            }*/
            string regexPatternEmail = "^[a-z0-9._%+-]+@[a-z0-9.-]+\\.[a-z]{2,4}$";
            if (!Regex.IsMatch(user.Email, regexPatternEmail))
            {
                response.StatusCode = 400;
                response.Message = "Enter Valid email";
                return response;
            }
            string regexPatternPhone = "^[6-9]\\d{9}$";
            if (!Regex.IsMatch(user.Phone.ToString(), regexPatternPhone))
            {
                response.StatusCode = 400;
                response.Message = "Enter Valid Phone Number";
                return response;
            }
            string regexPatternPassword = "^(?=.*?[A-Z])(?=.*?[a-z])(?=.*?[0-9])(?=.*?[#?!@$%^&*-]).{8,}$";
            if (!Regex.IsMatch(user.Password, regexPatternPassword))
            {
                response.StatusCode = 400;
                response.Message = "Password should contains 8 Characters atleast one Upper, lower alphabet and one special symbol";
                return response;
            }         
            TimeSpan ageTime = DateTime.Now - user.DateOfBirth;
            int age = (int)(ageTime.Days / 365.25);
            if (age < 12)
            {
                response.StatusCode = 400;
                response.Message = "You should be atleast 12 years old";
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
                    Gender = user.Gender,
                    ProfilePic = null,
                    DateOfBirth = DateTime.Parse(user.DateOfBirth.ToString("yyyy-MM-dd"))
                 };
                
                 if(user.Gender == 1) {
                    obj.ProfilePic = "wwwroot\\Images\\BoyImage.png";
                 }
                if (user.Gender == 2)
                {
                    obj.ProfilePic = "wwwroot\\Images\\GirlImage.png";
                }

                _dbContext.Users.Add(obj);
                 _dbContext.SaveChanges();
                 string token = CreateToken(obj, _configuration);
                 int len = obj == null ? 0 : 1;                             
                 if (len == 1)
                 {
                    DataModel dm = new DataModel();
                    dm.Email = obj.Email;
                    dm.Token = token;
                    response.Data = dm;                   
                    response.StatusCode = 200;
                    response.Message = "User Added Successfully";
                    return response;
                }                
            }
            return response;
        }

        // Encoding Password into PasswordHash using HMACSHA512
        private void CreatePasswordHash(string Password, out byte[] PasswordHash, out byte[] PasswordSalt)
        {
            using (var hmac = new HMACSHA512())
            {
                PasswordSalt = hmac.Key;
                PasswordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(Password));
            }
        }

        // Login User
        public Response Login(Login login, IConfiguration _configuration)
        {
            Response response = new Response();
            string regexPatternEmail = "^[a-z0-9._%+-]+@[a-z0-9.-]+\\.[a-z]{2,4}$";
            if (!Regex.IsMatch(login.Email, regexPatternEmail))
            {
                response.StatusCode = 400;
                response.Message = "Enter Valid email";
                return response;
            }
            if(login.Password=="" || login.Password == null)
            {
                response.StatusCode = 400;
                response.Message = "Please Enter the Password";
                return response;
            }

            var obj = _dbContext.Users.Where(u=>u.Email == login.Email).FirstOrDefault();
            
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
            response.Message = "Login Successful";
            return response;
        }

        // Generating JWT Token for Authentication
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

        // Verfying the PasswordHash
        private bool VerifyPasswordHash(string Password, byte[] PasswordHash, byte[] PasswordSalt)
        {
            using (var hmac = new HMACSHA512(PasswordSalt))
            {
                byte[] computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(Password));
                return computedHash.SequenceEqual(PasswordHash);
            }
        }        

        // Updating User Profile
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
            if (update.Phone != -1 || update.Phone == 0) {
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
            if (update.DateOfBirth != DateTime.MinValue && update.DateOfBirth != DateTime.Now) {
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
            obj.Gender = update.Gender;
            obj.UpdatedAt = DateTime.Now;
            _dbContext.SaveChanges();
            res.Data = obj;
            res.StatusCode = 200;
            res.Message = "User details updated";
            return res;    
        }

        // Deleting the User Account
        public Response DeleteUser(string email)
        {
            var obj = _dbContext.Users.FirstOrDefault(x => x.Email == email);
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

        // Changing the Password of User
        public Response ChangePassword(ChangePassword pass, string email)
        {
            Response response = new Response();
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

            string regexPatternPassword = "^(?=.*?[A-Z])(?=.*?[a-z])(?=.*?[0-9])(?=.*?[#?!@$%^&*-]).{8,}$";
            if (!Regex.IsMatch(pass.NewPassword, regexPatternPassword))
            {
                response.StatusCode = 400;
                response.Message = "Password should be of 8 length contains atleast one Upper, lower alphabet and one special symbol ";
                response.Data = null;
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

        // Searching the Available Users in Database
        public Response Search(string Name, string email)
        {    
            Response response = new Response();
            var users = _dbContext.Users.Where(u => (u.FirstName + " " + u.LastName).Contains(Name));
            var obj = _dbContext.Users.FirstOrDefault(x => x.Email == email);

            
            if(users == null || users.Count() == 0) {
                response.Data = null;
                response.StatusCode = 200;
                response.Message = "User Not Found";
                return response; 
            }
            var ExceptloginedUser = users.Where(u => u.UserId != obj.UserId).Select(u => new { u.UserId, u.FirstName, u.LastName, u.Email,u.ProfilePic });
            if (ExceptloginedUser.Count() == 0)
            {
                response.Data = null;
                response.StatusCode = 200;
                response.Message = "User Not Found";
                return response;
            }
            response.Data = ExceptloginedUser;
            response.StatusCode = 200;
            response.Message = "List Of Users";
            return response;
        }
        public Response GetUser(string email)
        {
            var obj = _dbContext.Users.FirstOrDefault(x => x.Email == email);
            string dateOfBirth = obj.DateOfBirth.ToString("yyyy-MM-dd").Split(" ").First();           
            Response res = new Response();
            res.StatusCode = 200;
            res.Message = "User Details";
            res.Data = new { obj.FirstName , obj.LastName , obj.Email , obj.Phone , dateOfBirth , obj.Gender,obj.ProfilePic} ;
            return res;
        }
    }
}
