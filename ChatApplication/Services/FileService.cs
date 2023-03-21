using ChatApplication.Data;
using ChatApplication.Models;

namespace ChatApplication.Services
{
    public class FileService : IFileService
    {
        private readonly ChatAPIDbContext _dbContext;
        public FileService(ChatAPIDbContext dbContext)
        {
            _dbContext = dbContext;
        }       
        public Response FileUpload(FileUpload upload, string email , int type)
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
            string FilePath;
            if (type == 1 || type == 2)
            {
                FilePath = Path.Combine("wwwroot", "Images", uniqueFileName);
            }
            else
            {
                FilePath= Path.Combine("wwwroot", "Files", uniqueFileName);
            }
            string path = Path.Combine(Directory.GetCurrentDirectory(), FilePath);
            if (type == 1)
            {
                obj.PathToProfilePic = FilePath;
            }
            var filestream = System.IO.File.Create(path);
            upload.File.CopyTo(filestream);
            filestream.Close();
            _dbContext.SaveChanges();
            response.Data = FilePath;
            response.StatusCode = 200;
            response.Message = "File Uploaded Successfully..";
            return response;
        }
   
    }
}
