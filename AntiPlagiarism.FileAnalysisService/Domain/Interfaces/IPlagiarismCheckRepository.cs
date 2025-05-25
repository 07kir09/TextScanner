using AntiPlagiarism.FileAnalysisService.Domain.Entities;

namespace AntiPlagiarism.FileAnalysisService.Domain.Interfaces
{
    public interface IPlagiarismCheckRepository
    {
        Task<PlagiarismCheckEntity?> GetByFileIdAsync(Guid fileId);
        Task<PlagiarismCheckEntity?> GetByHashAsync(string hash);
        Task<PlagiarismCheckEntity> SaveAsync(PlagiarismCheckEntity check);
    }
}