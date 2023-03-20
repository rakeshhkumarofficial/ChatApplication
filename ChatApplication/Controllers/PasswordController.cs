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

namespace ChatApplication.Controllers
{
    [Route("api/[controller]/[Action]")]
    [ApiController]
    public class PasswordController : ControllerBase
    {
        private readonly ChatAPIDbContext _dbContext;
        public readonly IConfiguration _configuration; 
        public PasswordController(ChatAPIDbContext dbContext, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _configuration = configuration;
            
        }

        // ForgetPassword Service

        [HttpPost]
        public IActionResult ForgetPassword(ForgetPasswordRequest fp)
        {
            var user = _dbContext.Users.FirstOrDefault(x => x.Email == fp.Email);
            bool IsUserExists = _dbContext.ForgetPasswords.Where(u => u.Email == fp.Email).Any();
            Response res = new Response();
            if (user == null)
            {
                res.StatusCode = 404;
                res.Message = "Email Not found";
                res.Data = null;
                return Ok(res);
            }         
            string urldirect = fp.URL;
            UriBuilder builder = new UriBuilder(urldirect);

            if (!IsUserExists)
            {
                ForgetPassword p = new ForgetPassword();
                p.Id = Guid.NewGuid();
                p.Email = fp.Email;
                p.ResetPasswordToken = CreateToken(user, _configuration);
                p.ExpiresAt = DateTime.Now.AddDays(1);
                _dbContext.ForgetPasswords.Add(p);
                _dbContext.SaveChanges();
                

                // Encode the JWT token as a URL-safe string
                string encodedToken = System.Net.WebUtility.UrlEncode(p.ResetPasswordToken);

                // Add the encoded JWT token as a query string parameter
                builder.Query = "token=" + encodedToken;

                // Get the modified link as a string
                string modifiedLink = builder.ToString();

                MailMessage message = new MailMessage();
                message.From = new MailAddress("rakesh.kumar23@chicmic.co.in");
                message.To.Add(new MailAddress(fp.Email));
                message.Subject = "Reset your Password";
                message.Body = $"link on the below link to verify and then reset your passoword \n" + modifiedLink;

                SmtpClient Newclient = new SmtpClient();
                Newclient.Credentials = new NetworkCredential("rakesh.kumar23@chicmic.co.in", "Chicmic@2022");
                Newclient.Host = "mail.chicmic.co.in";
                Newclient.Port = 587;
                Newclient.EnableSsl = true;
                Newclient.Send(message);
              
                res.Data = p.Email;
                res.StatusCode = 200;
                res.Message = "Verification Mail is Sent";
                return Ok(res);
            }

            var fpuser = _dbContext.ForgetPasswords.FirstOrDefault(x => x.Email == fp.Email);
            fpuser.ResetPasswordToken = CreateToken(user, _configuration);
            fpuser.ExpiresAt = DateTime.Now.AddDays(1);
            _dbContext.SaveChanges();

            // Encode the JWT token as a URL-safe string
            string encodedtoken = System.Net.WebUtility.UrlEncode(fpuser.ResetPasswordToken);

            // Add the encoded JWT token as a query string parameter
            builder.Query = "token=" + encodedtoken;

            // Get the modified link as a string
            string modifiedlink = builder.ToString();

            MailMessage msg = new MailMessage();
            msg.From = new MailAddress("rakesh.kumar23@chicmic.co.in");
            msg.To.Add(new MailAddress(fp.Email));
            msg.Subject = "Reset your Password";
            msg.Body = $"Link on the below link to verify and then reset your passoword \n" + modifiedlink;
            
            SmtpClient client = new SmtpClient();
            client.Credentials = new NetworkCredential("rakesh.kumar23@chicmic.co.in", "Chicmic@2022");
            client.Host = "mail.chicmic.co.in";
            client.Port = 587;
            client.EnableSsl = true;
            client.Send(msg);

            res.Data = fpuser.Email;
            res.StatusCode = 200;
            res.Message = "Verification Mail is Sent";
            return Ok(res);
        }

        // Reset Your Password

        [HttpPost,Authorize(Roles = "Reset")]
        public IActionResult ResetPassword(ResetPassword reset)
        {
            Response res = new Response();
            string regexPatternPassword = "^(?=.*?[A-Z])(?=.*?[a-z])(?=.*?[0-9])(?=.*?[#?!@$%^&*-]).{8,}$";
            if (!Regex.IsMatch(reset.NewPassword, regexPatternPassword))
            {  
                res.StatusCode = 400;
                res.Message = "Password should be of 8 length contains atleast one Upper, lower alphabet and one special symbol ";
                res.Data = null;
                return Ok(res);
            }
            var user = HttpContext.User;
            var email = user.FindFirst(ClaimTypes.Name)?.Value;
            var dbuser = _dbContext.Users.FirstOrDefault(x => x.Email == email);
            var fpUser = _dbContext.ForgetPasswords.FirstOrDefault(x => x.Email == dbuser.Email);

            if (fpUser == null || fpUser.ExpiresAt < DateTime.Now)
            {
                return BadRequest("Link Expired");
            }
        
            if (reset.NewPassword != reset.ConfirmPassword)
            {
                return BadRequest("Confirm Password does not match with the New Password");
            }     
            
            CreatePasswordHash(reset.NewPassword, out byte[] PasswordHash, out byte[] PasswordSalt);
            dbuser.PasswordHash = PasswordHash;
            dbuser.PasswordSalt = PasswordSalt;
            dbuser.UpdatedAt = DateTime.Now;
            _dbContext.SaveChanges();

           
            res.Data = dbuser;
            res.StatusCode = 200;
            res.Message = "Password Reset Successfully";
            _dbContext.Remove(fpUser);
            _dbContext.SaveChanges();
            return Ok(res);
        }

        // Creating JWT Token for Reset Role
        private string CreateToken(User obj, IConfiguration _configuration)
        {
            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name,obj.Email),
                new Claim(ClaimTypes.Role,"Reset")
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

        // Creating PasswordHash for new Password
        private void CreatePasswordHash(string Password, out byte[] PasswordHash, out byte[] PasswordSalt)
        {
            using (var hmac = new HMACSHA512())
            {
                PasswordSalt = hmac.Key;
                PasswordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(Password));
            }
        }
    }
}
