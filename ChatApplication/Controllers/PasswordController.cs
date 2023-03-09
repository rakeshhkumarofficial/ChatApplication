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

        [HttpPost]
        public IActionResult ForgetPassword(string email)
        {
            var user = _dbContext.Users.FirstOrDefault(x => x.Email == email);
            bool IsUserExists = _dbContext.ForgetPasswords.Where(u => u.Email == email).Any();
            if (user == null)
            {
                return BadRequest("user not found");
            }
            //Generate One Time Password
            Random random = new Random();
            int otp = random.Next(100000, 999999);
            
            if (!IsUserExists)
            {
                ForgetPassword p = new ForgetPassword();
                p.Id = Guid.NewGuid();
                p.Email = email;
                p.ResetPasswordToken = CreateToken(user, _configuration);
                p.OneTimePass = otp;
                p.ExpiresAt = DateTime.Now.AddDays(1);
                _dbContext.ForgetPasswords.Add(p);
                _dbContext.SaveChanges();

                MailMessage message = new MailMessage();
                message.From = new MailAddress("rakesh.kumar23@chicmic.co.in");
                message.To.Add(new MailAddress(email));
                message.Subject = "Reset your Password";
                message.Body = $"Your One Time Password is : {p.OneTimePass} \n" + "http://192.180.0.29:41485/verify";

                SmtpClient Newclient = new SmtpClient();
                Newclient.Credentials = new NetworkCredential("rakesh.kumar23@chicmic.co.in", "Chicmic@2022");
                Newclient.Host = "mail.chicmic.co.in";
                Newclient.Port = 587;
                Newclient.EnableSsl = true;
                Newclient.Send(message);

                Response res = new Response();
                res.Data = p.ResetPasswordToken;
                res.StatusCode = 200;
                res.Message = "Verification Mail is Sent";
                return Ok(res);
            }

            var fpuser = _dbContext.ForgetPasswords.FirstOrDefault(x => x.Email == email);
            fpuser.OneTimePass = otp;
            fpuser.ResetPasswordToken = CreateToken(user, _configuration);
            fpuser.ExpiresAt = DateTime.Now.AddDays(1);
            _dbContext.SaveChanges();

            MailMessage msg = new MailMessage();
            msg.From = new MailAddress("rakesh.kumar23@chicmic.co.in");
            msg.To.Add(new MailAddress(email));
            msg.Subject = "Reset your Password";
            msg.Body = $"Your One Time Password is : {fpuser.OneTimePass} \n" + "http://192.180.0.29:41485/verify";
            
            SmtpClient client = new SmtpClient();
            client.Credentials = new NetworkCredential("rakesh.kumar23@chicmic.co.in", "Chicmic@2022");
            client.Host = "mail.chicmic.co.in";
            client.Port = 587;
            client.EnableSsl = true;
            client.Send(msg);

            Response response = new Response();
            response.Data = fpuser.ResetPasswordToken;
            response.StatusCode = 200;
            response.Message = "Verification Mail is Sent";
            return Ok(response);



        }

        [HttpPost,Authorize]
        public IActionResult ResetPassword(ResetPassword reset)
        {
            var user = HttpContext.User;
            var email = user.FindFirst(ClaimTypes.Name)?.Value;
            var dbuser = _dbContext.Users.FirstOrDefault(x => x.Email == email);
            var fpUser = _dbContext.ForgetPasswords.FirstOrDefault(x => x.Email == dbuser.Email);

            if (fpUser == null || fpUser.ExpiresAt < DateTime.Now)
            {
                return BadRequest("Token Expired");
            }
            if (fpUser.OneTimePass != reset.OneTimePassword)
            {
                return BadRequest("Invalid OTP");
            }
            if (reset.NewPassword != reset.ConfirmPassword)
            {
                return BadRequest("Confirm Password does not match with the New Password");
            }     
            
            CreatePasswordHash(reset.NewPassword, out byte[] PasswordHash, out byte[] PasswordSalt);
            dbuser.PasswordHash = PasswordHash;
            dbuser.PasswordSalt = PasswordSalt;
            _dbContext.SaveChanges();

            Response res = new Response();
            res.Data = dbuser;
            res.StatusCode = 200;
            res.Message = "Password Reset Successfully";
            _dbContext.Remove(fpUser);
            _dbContext.SaveChanges();
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
