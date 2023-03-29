using ChatApplication.Data;
using ChatApplication.Models;
using ChatApplication.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ChatApplication.Controllers
{
    [Route("api/[controller]/[Action]")]
    [ApiController]
    public class FileController : ControllerBase
    {
        private readonly ChatAPIDbContext _dbContext;
        private readonly IFileService service;
        private readonly ILogger<FileController> _logger;
        public FileController(ChatAPIDbContext dbContext, ILogger<FileController> logger)
        {
            _logger = logger;
            _dbContext = dbContext;
            service = new FileService(_dbContext);        
        }

        [HttpPost, Authorize(Roles = "Login")]
        public IActionResult FileUpload([FromForm] FileUpload upload , int type)
        {
            _logger.LogInformation("\nExecuting method {MethodName}\n", nameof(FileUpload));
            var user = HttpContext.User;
            var email = user.FindFirst(ClaimTypes.Name)?.Value;
            var res = service.FileUpload(upload, email, type);
            return Ok(res);
            
        }
    }
}
