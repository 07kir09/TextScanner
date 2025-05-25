using AntiPlagiarism.FileStoringService.Application.Interfaces;
using AntiPlagiarism.FileStoringService.Domain.Interfaces;
using AntiPlagiarism.Common.DTO;
using AntiPlagiarism.FileStoringService.Domain.Entities;
namespace AntiPlagiarism.FileStoringService.Application.Services
{
    public class FileService(IFileRepository fileRepository, IFileStorage fileStorage) : IFileService
    {
        public async Task<FileDto> UploadFileAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("Файл не может быть пустым");
            }

            // Сохраняем файл в локальное хранилище
            await using Stream stream = file.OpenReadStream();
            string location = await fileStorage.SaveFileAsync(stream, file.FileName);

            // Сохраняем метаданные файла в БД
            FileEntity fileEntity = new FileEntity
            {
                Id = Guid.NewGuid(),
                Name = file.FileName,
                Location = location,
            };

            FileEntity savedFile = await fileRepository.SaveAsync(fileEntity);

            return new FileDto
            {
                Id = savedFile.Id,
                Name = savedFile.Name,
                Location = savedFile.Location
            };
        }
        
        public async Task<Stream> GetFileAsync(Guid id)
        {
            FileEntity? fileInfo = await fileRepository.GetFileByIdAsync(id);
            if (fileInfo == null)
            {
                throw new KeyNotFoundException($"Файл с ID {id} не найден");
            }

            return await fileStorage.GetFileAsync(fileInfo.Location);
        }
    }
}