using AntiPlagiarism.FileAnalysisService.Domain.Interfaces;
using Microsoft.Extensions.Options;
namespace AntiPlagiarism.FileAnalysisService.Infrastructure
{
    public class WordCloudStorage : IWordCloudStorage
    {
        private readonly string _storageDirectory;
        
        public WordCloudStorage(IOptions<WordCloudStorageSettings> options)
        {
            _storageDirectory = options.Value.StorageDirectory;
            if (!Directory.Exists(_storageDirectory))
            {
                Directory.CreateDirectory(_storageDirectory);
            }
        }
        
        public async Task<string> SaveWordCloudAsync(Stream image, string fileName)
        {
            if (image == null || image.Length == 0)
            {
                throw new ArgumentException("Изображение не может быть пустым");
            }

            string uniqueFileName = $"{Guid.NewGuid()}-{fileName}.png";
            string filePath = Path.Combine(_storageDirectory, uniqueFileName);

            await using FileStream fileStream = new FileStream(filePath, FileMode.Create);
            await image.CopyToAsync(fileStream);

            return uniqueFileName;
        }
        
        public async Task<Stream> GetWordCloudAsync(string location)
        {
            string filePath = Path.Combine(_storageDirectory, location);
            if (!File.Exists(filePath))
            {
                throw new KeyNotFoundException($"Изображение не найдено по пути: {location}");
            }

            return await Task.FromResult<Stream>(new FileStream(filePath, FileMode.Open, FileAccess.Read));
        }
    }
}