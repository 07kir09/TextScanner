namespace AntiPlagiarism.FileAnalysisService.Domain.Interfaces
{
    public interface IWordCloudStorage
    {
        Task<string> SaveWordCloudAsync(Stream image, string fileName);
        Task<Stream> GetWordCloudAsync(string location);
    }
}