using AntiPlagiarism.FileStoringService.Domain.Interfaces;
using Microsoft.Extensions.Options;

namespace AntiPlagiarism.FileStoringService.Infrastructure
{
    public class LocalFileStorage : IFileStorage
    {
        private readonly string _storageDirectory;
        
        public LocalFileStorage(IOptions<LocalFileStorageSettings> options)
        {
            _storageDirectory = options.Value.StorageDirectory;
            if (!Directory.Exists(_storageDirectory))
            {
                Directory.CreateDirectory(_storageDirectory);
            }
        }
        
        public async Task<string> SaveFileAsync(Stream content, string fileName)
        {
            // Генерируем уникальный идентификатор для файла
            string uniqueFileName = $"{Guid.NewGuid()}-{fileName}";
            string filePath = Path.Combine(_storageDirectory, uniqueFileName);

            await using FileStream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            await content.CopyToAsync(fileStream);

            // Возвращаем только имя файла как местоположение
            return uniqueFileName;
        }
        
        
        public async Task<Stream> GetFileAsync(string location)
        {
            string filePath = Path.Combine(_storageDirectory, location);
            
            if (!File.Exists(filePath))
            {
                throw new KeyNotFoundException($"Файл не найден по пути: {location}");
            }
            
            return await Task.FromResult((Stream)new FileStream(filePath, FileMode.Open, FileAccess.Read));
        }
    }
}