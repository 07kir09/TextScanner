using AntiPlagiarism.Common.DTO;
using AntiPlagiarism.FileStoringService.Application.Services;
using AntiPlagiarism.FileStoringService.Domain.Entities;
using AntiPlagiarism.FileStoringService.Domain.Interfaces;
using AntiPlagiarism.FileStoringService.Infrastructure;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Text;

namespace AntiPlagiarism.FileStoringService.Tests
{
    public class FileServiceTests
    {
        private readonly Mock<IFileRepository> _mockRepository;
        private readonly Mock<IFileStorage> _mockStorage;
        private readonly FileService _fileService;

        public FileServiceTests()
        {
            _mockRepository = new Mock<IFileRepository>();
            _mockStorage = new Mock<IFileStorage>();
            _fileService = new FileService(_mockRepository.Object, _mockStorage.Object);
        }

        [Fact]
        public async Task UploadFileAsync_ValidFile_ReturnsFileDto()
        {
            // Arrange
            string fileName = "test.txt";
            string fileContent = "Test content";
            string location = "unique-file-name.txt";
            Guid guid = Guid.NewGuid();

            Mock<IFormFile> mockFile = CreateMockFile(fileName, fileContent);

            _mockStorage.Setup(s => s.SaveFileAsync(It.IsAny<Stream>(), fileName))
                .ReturnsAsync(location);

            _mockRepository.Setup(r => r.SaveAsync(It.IsAny<FileEntity>()))
                .ReturnsAsync((FileEntity entity) =>
                {
                    entity.Id = guid;
                    return entity;
                });

            // Act
            FileDto result = await _fileService.UploadFileAsync(mockFile.Object);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(guid, result.Id);
            Assert.Equal(fileName, result.Name);
            Assert.Equal(location, result.Location);

            _mockStorage.Verify(s => s.SaveFileAsync(It.IsAny<Stream>(), fileName), Times.Once);
            _mockRepository.Verify(r => r.SaveAsync(It.Is<FileEntity>(e => 
                e.Name == fileName && 
                e.Location == location)), 
                Times.Once);
        }

        [Fact]
        public async Task UploadFileAsync_NullFile_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _fileService.UploadFileAsync(null!));
        }

        [Fact]
        public async Task UploadFileAsync_EmptyFile_ThrowsArgumentException()
        {
            // Arrange
            Mock<IFormFile> mockFile = CreateMockFile("test.txt", "", 0);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _fileService.UploadFileAsync(mockFile.Object));
        }

        [Fact]
        public async Task GetFileAsync_ExistingFile_ReturnsStream()
        {
            // Arrange
            Guid fileId = Guid.NewGuid();
            FileEntity fileEntity = new FileEntity
            {
                Id = fileId,
                Name = "test.txt",
                Location = "location.txt"
            };

            MemoryStream expectedStream = new MemoryStream("File content"u8.ToArray());

            _mockRepository.Setup(r => r.GetFileByIdAsync(fileId))
                .ReturnsAsync(fileEntity);

            _mockStorage.Setup(s => s.GetFileAsync(fileEntity.Location))
                .ReturnsAsync(expectedStream);

            // Act
            Stream result = await _fileService.GetFileAsync(fileId);

            // Assert
            Assert.NotNull(result);
            Assert.Same(expectedStream, result);

            _mockRepository.Verify(r => r.GetFileByIdAsync(fileId), Times.Once);
            _mockStorage.Verify(s => s.GetFileAsync(fileEntity.Location), Times.Once);
        }

        [Fact]
        public async Task GetFileAsync_NonExistingFile_ThrowsKeyNotFoundException()
        {
            // Arrange
            Guid fileId = Guid.NewGuid();

            _mockRepository.Setup(r => r.GetFileByIdAsync(fileId))
                .ReturnsAsync((FileEntity?)null);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() => _fileService.GetFileAsync(fileId));

            _mockRepository.Verify(r => r.GetFileByIdAsync(fileId), Times.Once);
            _mockStorage.Verify(s => s.GetFileAsync(It.IsAny<string>()), Times.Never);
        }

        private static Mock<IFormFile> CreateMockFile(string fileName, string content, long? fileSize = null)
        {
            Mock<IFormFile> mock = new Mock<IFormFile>();
            byte[] bytes = Encoding.UTF8.GetBytes(content);
            MemoryStream stream = new MemoryStream(bytes);

            mock.Setup(f => f.FileName).Returns(fileName);
            mock.Setup(f => f.Length).Returns(fileSize ?? bytes.Length);
            mock.Setup(f => f.OpenReadStream()).Returns(stream);

            return mock;
        }
    }
    
    public class LocalFileStorageTests : IDisposable
    {
        private readonly string _testDir;
        private readonly LocalFileStorage _fileStorage;

        public LocalFileStorageTests()
        {
            // Создаем временную директорию для тестов
            _testDir = Path.Combine(Path.GetTempPath(), $"test_file_storage_{Guid.NewGuid()}");
            
            IOptions<LocalFileStorageSettings> options = Options.Create(new LocalFileStorageSettings { StorageDirectory = _testDir });
            _fileStorage = new LocalFileStorage(options);
        }

        [Fact]
        public async Task SaveFileAsync_ValidStream_SavesFile()
        {
            // Arrange
            string fileName = "test.txt";
            string content = "Тестовое содержимое файла";
            using MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

            // Act
            string location = await _fileStorage.SaveFileAsync(stream, fileName);

            // Assert
            Assert.NotEmpty(location);
            Assert.EndsWith(fileName, location);
            
            string fullPath = Path.Combine(_testDir, location);
            Assert.True(File.Exists(fullPath));
            string savedContent = await File.ReadAllTextAsync(fullPath);
            Assert.Equal(content, savedContent);
        }

        [Fact]
        public async Task GetFileAsync_ExistingFile_ReturnsStream()
        {
            // Arrange
            string fileName = "test_get.txt";
            string content = "Содержимое для чтения";
            string uniqueFileName = $"{Guid.NewGuid()}-{fileName}";
            string filePath = Path.Combine(_testDir, uniqueFileName);
            
            await File.WriteAllTextAsync(filePath, content);

            // Act
            await using Stream stream = await _fileStorage.GetFileAsync(uniqueFileName);
            using StreamReader reader = new(stream);
            string readContent = await reader.ReadToEndAsync();

            // Assert
            Assert.Equal(content, readContent);
        }

        [Fact]
        public async Task GetFileAsync_NonExistingFile_ThrowsKeyNotFoundException()
        {
            // Arrange
            string nonExistingFile = "non-existing-file.txt";

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() => 
                _fileStorage.GetFileAsync(nonExistingFile));
        }

        public void Dispose()
        {
            // Очистка после тестов
            if (Directory.Exists(_testDir))
            {
                Directory.Delete(_testDir, true);
            }
        }
    }
    
    
    public class FileRepositoryTests
    {
        private readonly DbContextOptions<FileDbContext> _options;

        public FileRepositoryTests()
        {
            _options = new DbContextOptionsBuilder<FileDbContext>()
                .UseInMemoryDatabase(databaseName: $"InMemoryFileDb_{Guid.NewGuid()}")
                .Options;
        }

        [Fact]
        public async Task GetFileByIdAsync_ExistingFile_ReturnsFile()
        {
            // Arrange
            Guid fileId = Guid.NewGuid();
            FileEntity fileEntity = new FileEntity
            {
                Id = fileId,
                Name = "test.txt",
                Location = "test-location.txt"
            };

            using (FileDbContext context = new FileDbContext(_options))
            {
                await context.Files.AddAsync(fileEntity);
                await context.SaveChangesAsync();
            }

            // Act
            using (FileDbContext context = new FileDbContext(_options))
            {
                FileRepository repository = new FileRepository(context);
                FileEntity? result = await repository.GetFileByIdAsync(fileId);

                // Assert
                Assert.NotNull(result);
                Assert.Equal(fileId, result.Id);
                Assert.Equal("test.txt", result.Name);
                Assert.Equal("test-location.txt", result.Location);
            }
        }

        [Fact]
        public async Task GetFileByIdAsync_NonExistingFile_ReturnsNull()
        {
            // Arrange
            Guid nonExistingId = Guid.NewGuid();

            // Act
            using (FileDbContext context = new FileDbContext(_options))
            {
                FileRepository repository = new FileRepository(context);
                FileEntity? result = await repository.GetFileByIdAsync(nonExistingId);

                // Assert
                Assert.Null(result);
            }
        }

        [Fact]
        public async Task SaveAsync_NewFile_SavesAndReturnsFile()
        {
            // Arrange
            FileEntity fileEntity = new FileEntity
            {
                Id = Guid.NewGuid(),
                Name = "save-test.txt",
                Location = "save-location.txt"
            };

            // Act
            using (FileDbContext context = new FileDbContext(_options))
            {
                FileRepository repository = new FileRepository(context);
                FileEntity savedFile = await repository.SaveAsync(fileEntity);

                // Assert сразу после сохранения
                Assert.NotNull(savedFile);
                Assert.Equal(fileEntity.Id, savedFile.Id);
                Assert.Equal(fileEntity.Name, savedFile.Name);
                Assert.Equal(fileEntity.Location, savedFile.Location);
            }

            // Дополнительная проверка, что файл действительно сохранен
            using (FileDbContext context = new FileDbContext(_options))
            {
                FileEntity? file = await context.Files.FindAsync(fileEntity.Id);
                Assert.NotNull(file);
                Assert.Equal(fileEntity.Name, file.Name);
                Assert.Equal(fileEntity.Location, file.Location);
            }
        }
    }
}