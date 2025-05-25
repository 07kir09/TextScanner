using AntiPlagiarism.FileAnalysisService.Domain.Entities;
using AntiPlagiarism.FileAnalysisService.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
namespace AntiPlagiarism.FileAnalysisService.Infrastructure
{
    public class PlagiarismCheckRepository(AnalysisDbContext context) : IPlagiarismCheckRepository
    {
        public async Task<PlagiarismCheckEntity?> GetByFileIdAsync(Guid fileId)
        {
            return await context.PlagiarismChecks.FirstOrDefaultAsync(c => c.FileId == fileId);
        }

        public async Task<PlagiarismCheckEntity?> GetByHashAsync(string hash)
        {
            return await context.PlagiarismChecks.FirstOrDefaultAsync(c => c.Hash == hash);
        }

        public async Task<PlagiarismCheckEntity> SaveAsync(PlagiarismCheckEntity check)
        {
            context.PlagiarismChecks.Add(check);
            await context.SaveChangesAsync();
            return check;
        }
    }
}