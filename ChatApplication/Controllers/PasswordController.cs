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

namespace ChatApplication.Controllers
{
    [Route("api/[controller]")]
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
        [Route("Forget-Password")]
        public IActionResult ForgetPassword(string email)
        {
            var user = _dbContext.Users.FirstOrDefault(x => x.Email == email);
            if (user == null)
            {
                return BadRequest("user not found");
            }
            //Generate One Time Password
            Random random = new Random();
            int otp = random.Next(100000, 999999);

            ForgetPassword p = new ForgetPassword();
            p.Id = Guid.NewGuid();
            p.Email = email ;
            p.ResetPasswordToken = CreateRandomToken();
            p.OneTimePass = otp;
            p.ExpiresAt = DateTime.Now.AddDays(1);
            _dbContext.ForgetPasswords.Add(p);
            _dbContext.SaveChanges();                     

            MailMessage message = new MailMessage();
            message.From = new MailAddress("rakesh.kumar23@chicmic.co.in");
            message.To.Add(new MailAddress(email));
            message.Subject = "Reset your Password";
            message.Body = $"Your One Time Password is : {p.OneTimePass} \n" + "https://localhost:7293/api/Password/Verify-Mail";

            SmtpClient client = new SmtpClient();
            client.Credentials = new NetworkCredential("rakesh.kumar23@chicmic.co.in", "Chicmic@2022");
            client.Host = "mail.chicmic.co.in";
            client.Port = 587;
            client.EnableSsl = true;
            client.Send(message);

            return Ok("Verification mail is sent");
        }
        [HttpPost]
        [Authorize]
        [Route("Reset-Password")]
        public IActionResult ResetPassword(ResetPassword reset)
        {
            if(reset.NewPassword != reset.ConfirmPassword)
            {
                return BadRequest("Confirm Password does not match with the New Password");
            }
            
            var user = _dbContext.ForgetPasswords.FirstOrDefault(x => x.Email == reset.Email);  
          
            var resuser = _dbContext.Users.FirstOrDefault(x => x.Email == user.Email);
            if (user == null || user.ExpiresAt < DateTime.Now)
            {
                return BadRequest("Invalid Email");
            }
            CreatePasswordHash(reset.NewPassword, out byte[] PasswordHash, out byte[] PasswordSalt);
            resuser.PasswordHash = PasswordHash;
            resuser.PasswordSalt = PasswordSalt;
            _dbContext.SaveChanges();

            Response res = new Response();
            res.Data = resuser; 
            res.StatusCode = 200;
            res.Message = "Password Reset Successfully";
            user.ExpiresAt = DateTime.Parse(null);
            user.ResetPasswordToken = null;
            return Ok(res);
        }

        [HttpPost]
        [Route("Verify-Mail")]
        public  IActionResult VerifyMail(string email,int OneTimePassword)
        {
            var user = _dbContext.ForgetPasswords.FirstOrDefault(x => x.Email == email);
            if (user == null)
            {
                return BadRequest("Invalid email");
            }
            if(user.OneTimePass != OneTimePassword)
            {
                return BadRequest("Invalid OTP");
            }

            if (user == null || user.ExpiresAt < DateTime.Now)
            {
                return BadRequest("OTP is Expired..");
            }
            Response res = new Response();
            res.Data = user.ResetPasswordToken;
            res.StatusCode = 200;
            res.Message = "Email is Verified";
            return Ok(res);

        }


        private string CreateRandomToken()
        {
            return Convert.ToHexString(RandomNumberGenerator.GetBytes(64));
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
