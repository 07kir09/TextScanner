using AntiPlagiarism.Common.DTO;
namespace AntiPlagiarism.FileStoringService.Application.Interfaces
{
    public interface IFileService
    {
        Task<FileDto> UploadFileAsync(IFormFile file);
        Task<Stream> GetFileAsync(Guid fileId);
    }
}