using ChatApplication.Models;

namespace ChatApplication.Services
{
    public interface IFileService
    {
       // public Response UploadProfileImage(FileUpload upload, string email);
        public Response FileUpload(FileUpload upload, string email , int type);
       // public Response UploadFile(FileUpload upload, string email);
    }
}
