using AntiPlagiarism.FileStoringService.Domain.Entities;

namespace AntiPlagiarism.FileStoringService.Domain.Interfaces
{
    public interface IFileRepository
    {
        Task<FileEntity?> GetFileByIdAsync(Guid id);
        Task<FileEntity> SaveAsync(FileEntity file);
    }
}