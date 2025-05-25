namespace AntiPlagiarism.FileStoringService.Domain.Interfaces
{
    public interface IFileStorage
    {
        Task<string> SaveFileAsync(Stream content, string fileName);
        Task<Stream> GetFileAsync(string location);
    }
}