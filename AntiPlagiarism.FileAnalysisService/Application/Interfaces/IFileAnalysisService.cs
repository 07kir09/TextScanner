using AntiPlagiarism.Common.DTO;

namespace AntiPlagiarism.FileAnalysisService.Application.Interfaces
{
    public interface IFileAnalysisService
    {
        Task<FileAnalysisResultDto> AnalyzeFileAsync(Guid fileId);
        Task<Stream> GetWordCloudAsync(string location);
    }
}