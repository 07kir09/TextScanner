using AntiPlagiarism.FileAnalysisService.Domain.Entities;
using AntiPlagiarism.FileAnalysisService.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
namespace AntiPlagiarism.FileAnalysisService.Infrastructure
{
    public class FileAnalysisResultRepository(AnalysisDbContext context) : IAnalysisResultRepository
    {
        public async Task<FileAnalysisEntity?> GetByFileIdAsync(Guid fileId)
        {
            return await context.AnalysisResults.FirstOrDefaultAsync(r => r.FileId == fileId);
        }
        
        
        public async Task<FileAnalysisEntity> SaveAsync(FileAnalysisEntity result)
        {
            context.AnalysisResults.Add(result);
            await context.SaveChangesAsync();
            return result;
        }
    }
}