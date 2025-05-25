using AntiPlagiarism.FileStoringService.Domain.Entities;
using AntiPlagiarism.FileStoringService.Domain.Interfaces;

namespace AntiPlagiarism.FileStoringService.Infrastructure
{
    public class FileRepository(FileDbContext context) : IFileRepository
    {
        public async Task<FileEntity?> GetFileByIdAsync(Guid id)
        {
            return await context.Files.FindAsync(id);
        }
        
        public async Task<FileEntity> SaveAsync(FileEntity file)
        {
            await context.Files.AddAsync(file);
            await context.SaveChangesAsync();
            return file;
        }
    }
}