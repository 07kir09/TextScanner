using AntiPlagiarism.FileAnalysisService.Domain.Entities;

namespace AntiPlagiarism.FileAnalysisService.Domain.Interfaces
{
    public interface IAnalysisResultRepository
    {
        Task<FileAnalysisEntity?> GetByFileIdAsync(Guid fileId);
        Task<FileAnalysisEntity> SaveAsync(FileAnalysisEntity result);
    }
}