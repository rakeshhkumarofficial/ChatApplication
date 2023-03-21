using ChatApplication.Data;
using ChatApplication.Models;
using ChatApplication.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Security.Claims;

namespace ChatApplication.Controllers
{
    [Route("api/[controller]/[Action]")]
    [ApiController]
    public class FileController : ControllerBase
    {
        private readonly ChatAPIDbContext _dbContext;
        private readonly IFileService service;
        public FileController(ChatAPIDbContext dbContext)
        {
            _dbContext = dbContext;
            service = new FileService(_dbContext);
        }

        [HttpPost, Authorize(Roles = "Login")]
        public IActionResult FileUpload([FromForm] FileUpload upload, int type)
        {
            var user = HttpContext.User;
            var email = user.FindFirst(ClaimTypes.Name)?.Value;
            var res = service.FileUpload(upload, email, type);
            return Ok(res);         
        }
    }
}
