using AntiPlagiarism.Common.DTO;
using AntiPlagiarism.FileAnalysisService.Domain.Entities;
using AntiPlagiarism.FileAnalysisService.Domain.Interfaces;
using Moq;
using Moq.Protected;
using System.Net;
using AntiPlagiarism.FileAnalysisService.Infrastructure;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using FileAnalysisServiceClass = AntiPlagiarism.FileAnalysisService.Application.Services.FileAnalysisService;
namespace AntiPlagiarism.FileAnalysisService.Tests
{
    public class FileAnalysisServiceTests
    {
        private readonly Mock<IAnalysisResultRepository> _mockAnalysisRepo;
        private readonly Mock<IPlagiarismCheckRepository> _mockPlagiarismRepo;
        private readonly Mock<IWordCloudStorage> _mockWordCloudStorage;
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly HttpClient _httpClient;
        private readonly FileAnalysisServiceClass _service;

        public FileAnalysisServiceTests()
        {
            _mockAnalysisRepo = new Mock<IAnalysisResultRepository>();
            _mockPlagiarismRepo = new Mock<IPlagiarismCheckRepository>();
            _mockWordCloudStorage = new Mock<IWordCloudStorage>();
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
            _service = new FileAnalysisServiceClass(
                _mockAnalysisRepo.Object,
                _mockPlagiarismRepo.Object,
                _mockWordCloudStorage.Object,
                _httpClient
            );
        }

        [Fact]
        public async Task AnalyzeFileAsync_WithExistingAnalysis_ReturnsStoredResult()
        {
            // Arrange
            Guid fileId = Guid.NewGuid();
            FileAnalysisEntity existingResult = new FileAnalysisEntity
            {
                Id = Guid.NewGuid(),
                FileId = fileId,
                WordCount = 100,
                ParagraphCount = 10,
                CharacterCount = 500,
                WordCloudLocation = "wordcloud-123.png"
            };

            PlagiarismCheckEntity existingCheck = new PlagiarismCheckEntity
            {
                Id = Guid.NewGuid(),
                FileId = fileId,
                Hash = "hash123",
                IsPlagiarized = true,
                SimilarFileId = Guid.NewGuid()
            };

            _mockAnalysisRepo.Setup(r => r.GetByFileIdAsync(fileId))
                .ReturnsAsync(existingResult);

            _mockPlagiarismRepo.Setup(r => r.GetByFileIdAsync(fileId))
                .ReturnsAsync(existingCheck);

            // Act
            FileAnalysisResultDto result = await _service.AnalyzeFileAsync(fileId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(fileId, result.FileId);
            Assert.Equal(existingResult.WordCount, result.WordCount);
            Assert.Equal(existingResult.ParagraphCount, result.ParagraphCount);
            Assert.Equal(existingResult.CharacterCount, result.CharacterCount);
            Assert.Equal(existingResult.WordCloudLocation, result.WordCloudLocation);
            Assert.True(result.IsPlagiarism);
            Assert.Equal(existingCheck.SimilarFileId, result.OriginalFileId);

            // Проверка, что не было вызовов других методов
            _mockHttpMessageHandler.Protected().Verify(
                "SendAsync",
                Times.Never(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task AnalyzeFileAsync_NewFileNotPlagiarized_AnalyzesAndSavesResult()
        {
            // Arrange
            Guid fileId = Guid.NewGuid();
            string fileContent = "This is a test file content.\nIt has multiple lines.\n\nAnd paragraphs.";
            int expectedWordCount = 12; // Количество слов в файле
            int expectedParagraphCount = 2; // Количество параграфов
            int expectedCharCount = fileContent.Length; // Количество символов
            string wordCloudLocation = "wordcloud-123.png";

            // Настраиваем ответы от HTTP-сервисов
            SetupMockHttpResponse(fileId, fileContent);
            SetupMockWordCloudResponse();

            // Настраиваем репозитории для отсутствия предыдущих результатов
            _mockAnalysisRepo.Setup(r => r.GetByFileIdAsync(fileId))
                .ReturnsAsync((FileAnalysisEntity?)null);

            _mockPlagiarismRepo.Setup(r => r.GetByFileIdAsync(fileId))
                .ReturnsAsync((PlagiarismCheckEntity?)null);

            // Настраиваем сохранение хеша и результатов анализа
            _mockPlagiarismRepo.Setup(r => r.GetByHashAsync(It.IsAny<string>()))
                .ReturnsAsync((PlagiarismCheckEntity?)null);

            _mockPlagiarismRepo.Setup(r => r.SaveAsync(It.IsAny<PlagiarismCheckEntity>()))
                .ReturnsAsync((PlagiarismCheckEntity entity) => entity);

            _mockWordCloudStorage.Setup(s => s.SaveWordCloudAsync(It.IsAny<Stream>(), It.IsAny<string>()))
                .ReturnsAsync(wordCloudLocation);

            _mockAnalysisRepo.Setup(r => r.SaveAsync(It.IsAny<FileAnalysisEntity>()))
                .ReturnsAsync((FileAnalysisEntity entity) => entity);

            // Act
            FileAnalysisResultDto result = await _service.AnalyzeFileAsync(fileId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(fileId, result.FileId);
            Assert.Equal(expectedWordCount, result.WordCount);
            Assert.Equal(expectedParagraphCount, result.ParagraphCount);
            Assert.Equal(expectedCharCount, result.CharacterCount);
            Assert.Equal(wordCloudLocation, result.WordCloudLocation);
            Assert.False(result.IsPlagiarism);
            Assert.Null(result.OriginalFileId);

            // Проверка вызовов методов
            _mockPlagiarismRepo.Verify(r => r.SaveAsync(It.Is<PlagiarismCheckEntity>(e =>
                e.FileId == fileId &&
                e.IsPlagiarized == false &&
                e.SimilarFileId == null)),
                Times.Once);

            _mockAnalysisRepo.Verify(r => r.SaveAsync(It.Is<FileAnalysisEntity>(e =>
                e.FileId == fileId &&
                e.WordCount == expectedWordCount &&
                e.ParagraphCount == expectedParagraphCount &&
                e.CharacterCount == expectedCharCount &&
                e.WordCloudLocation == wordCloudLocation)),
                Times.Once);

            _mockWordCloudStorage.Verify(s => s.SaveWordCloudAsync(It.IsAny<Stream>(), 
                It.Is<string>(name => name.Contains(fileId.ToString()))), 
                Times.Once);
        }

        [Fact]
        public async Task AnalyzeFileAsync_PlagiarizedFile_DetectsPlagiarism()
        {
            
            // Arrange
            Guid fileId = Guid.NewGuid();
            Guid originalFileId = Guid.NewGuid();
            string fileContent = "This is a plagiarized content.";
            string fileHash = "hash123"; // В реальности это будет SHA-256 хеш
            string wordCloudLocation = "wordcloud-456.png";

            // Настраиваем ответ от сервиса хранения файлов
            SetupMockHttpResponse(fileId, fileContent);

            // Настраиваем ответ от сервиса генерации облака слов
            SetupMockWordCloudResponse();

            // Настраиваем репозитории
            _mockAnalysisRepo.Setup(r => r.GetByFileIdAsync(fileId))
                .ReturnsAsync((FileAnalysisEntity?)null);

            _mockPlagiarismRepo.Setup(r => r.GetByHashAsync(It.IsAny<string>()))
                .ReturnsAsync(new PlagiarismCheckEntity 
                { 
                    Id = Guid.NewGuid(),
                    FileId = originalFileId,
                    Hash = fileHash,
                    IsPlagiarized = false,
                    SimilarFileId = null
                });

            _mockPlagiarismRepo.Setup(r => r.SaveAsync(It.IsAny<PlagiarismCheckEntity>()))
                .ReturnsAsync((PlagiarismCheckEntity entity) => entity);

            _mockWordCloudStorage.Setup(s => s.SaveWordCloudAsync(It.IsAny<Stream>(), It.IsAny<string>()))
                .ReturnsAsync(wordCloudLocation);

            _mockAnalysisRepo.Setup(r => r.SaveAsync(It.IsAny<FileAnalysisEntity>()))
                .ReturnsAsync((FileAnalysisEntity entity) => entity);

            // Act
            FileAnalysisResultDto result = await _service.AnalyzeFileAsync(fileId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(fileId, result.FileId);
            Assert.True(result.IsPlagiarism);
            Assert.Equal(originalFileId, result.OriginalFileId);

            // Проверка вызовов методов
            _mockPlagiarismRepo.Verify(r => r.SaveAsync(It.Is<PlagiarismCheckEntity>(e =>
                e.FileId == fileId && e.IsPlagiarized == true && e.SimilarFileId == originalFileId)), Times.Once);
        }

        [Fact]
        public async Task GetWordCloudAsync_ExistingWordCloud_ReturnsStream()
        {
            // Arrange
            string location = "wordcloud-123.png";
            MemoryStream expectedStream = new MemoryStream("mock image data"u8.ToArray());

            _mockWordCloudStorage.Setup(s => s.GetWordCloudAsync(location))
                .ReturnsAsync(expectedStream);

            // Act
            Stream result = await _service.GetWordCloudAsync(location);

            // Assert
            Assert.NotNull(result);
            Assert.Same(expectedStream, result);
            _mockWordCloudStorage.Verify(s => s.GetWordCloudAsync(location), Times.Once);
        }

        [Fact]
        public async Task GetWordCloudAsync_NonExistingWordCloud_ThrowsKeyNotFoundException()
        {
            // Arrange
            string nonExistingLocation = "non-existing.png";

            _mockWordCloudStorage.Setup(s => s.GetWordCloudAsync(nonExistingLocation))
                .ThrowsAsync(new KeyNotFoundException($"Изображение не найдено по пути: {nonExistingLocation}"));

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.GetWordCloudAsync(nonExistingLocation));
            _mockWordCloudStorage.Verify(s => s.GetWordCloudAsync(nonExistingLocation), Times.Once);
        }

        private void SetupMockHttpResponse(Guid fileId, string content)
        {
            HttpResponseMessage mockResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(content)
            };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get &&
                        req.RequestUri != null && 
                        req.RequestUri.ToString().Contains($"/{fileId}")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(mockResponse);
        }
        
        private void SetupMockWordCloudResponse()
        {
            // Создаем фейковый ответ от сервиса облака слов
            HttpResponseMessage mockResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ByteArrayContent(new byte[100]) // имитация изображения
            };

            // Настраиваем обработку любых POST запросов к quickchart.io
            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Post &&
                        req.RequestUri != null &&
                        req.RequestUri.ToString().Contains("wordcloud")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(mockResponse);
        }
    }
    
    
    public class FileAnalysisResultRepositoryTests
    {
        private readonly DbContextOptions<AnalysisDbContext> _options;
        
        public FileAnalysisResultRepositoryTests()
        {
            _options = new DbContextOptionsBuilder<AnalysisDbContext>()
                .UseInMemoryDatabase(databaseName: $"InMemoryAnalysisTestDb_{Guid.NewGuid()}")
                .Options;
        }
        
        [Fact]
        public async Task GetByFileIdAsync_ExistingResult_ReturnsResult()
        {
            // Arrange
            Guid fileId = Guid.NewGuid();
            FileAnalysisEntity resultEntity = new FileAnalysisEntity
            {
                Id = Guid.NewGuid(),
                FileId = fileId,
                WordCount = 100,
                ParagraphCount = 5,
                CharacterCount = 500,
                WordCloudLocation = "wordcloud-123.png"
            };

            using (AnalysisDbContext context = new AnalysisDbContext(_options))
            {
                await context.AnalysisResults.AddAsync(resultEntity);
                await context.SaveChangesAsync();
            }

            // Act
            using (AnalysisDbContext context = new AnalysisDbContext(_options))
            {
                FileAnalysisResultRepository repository = new FileAnalysisResultRepository(context);
                FileAnalysisEntity? result = await repository.GetByFileIdAsync(fileId);

                // Assert
                Assert.NotNull(result);
                Assert.Equal(fileId, result.FileId);
                Assert.Equal(100, result.WordCount);
                Assert.Equal(5, result.ParagraphCount);
                Assert.Equal(500, result.CharacterCount);
                Assert.Equal("wordcloud-123.png", result.WordCloudLocation);
            }
        }

        [Fact]
        public async Task GetByFileIdAsync_NonExistingResult_ReturnsNull()
        {
            // Arrange
            Guid nonExistingId = Guid.NewGuid();

            // Act
            using (AnalysisDbContext context = new AnalysisDbContext(_options))
            {
                FileAnalysisResultRepository repository = new FileAnalysisResultRepository(context);
                FileAnalysisEntity? result = await repository.GetByFileIdAsync(nonExistingId);

                // Assert
                Assert.Null(result);
            }
        }

        [Fact]
        public async Task SaveAsync_NewResult_SavesAndReturnsResult()
        {
            // Arrange
            FileAnalysisEntity newEntity = new FileAnalysisEntity
            {
                Id = Guid.NewGuid(),
                FileId = Guid.NewGuid(),
                WordCount = 200,
                ParagraphCount = 10,
                CharacterCount = 1000,
                WordCloudLocation = "new-wordcloud.png"
            };

            // Act
            using (AnalysisDbContext context = new AnalysisDbContext(_options))
            {
                FileAnalysisResultRepository repository = new FileAnalysisResultRepository(context);
                FileAnalysisEntity result = await repository.SaveAsync(newEntity);

                // Assert
                Assert.Equal(newEntity.Id, result.Id);
                Assert.Equal(newEntity.FileId, result.FileId);
                Assert.Equal(newEntity.WordCount, result.WordCount);
            }

            // Дополнительно проверяем, что сущность действительно сохранена в БД
            using (AnalysisDbContext context = new AnalysisDbContext(_options))
            {
                FileAnalysisEntity? savedEntity = await context.AnalysisResults.FindAsync(newEntity.Id);
                Assert.NotNull(savedEntity);
                Assert.Equal(newEntity.FileId, savedEntity.FileId);
                Assert.Equal(newEntity.WordCount, savedEntity.WordCount);
                Assert.Equal(newEntity.ParagraphCount, savedEntity.ParagraphCount);
                Assert.Equal(newEntity.CharacterCount, savedEntity.CharacterCount);
                Assert.Equal(newEntity.WordCloudLocation, savedEntity.WordCloudLocation);
            }
        }
    }
    
    
    public class PlagiarismCheckRepositoryTests
    {
        private readonly DbContextOptions<AnalysisDbContext> _options;
        
        public PlagiarismCheckRepositoryTests()
        {
            _options = new DbContextOptionsBuilder<AnalysisDbContext>()
                .UseInMemoryDatabase(databaseName: $"InMemoryPlagiarismTestDb_{Guid.NewGuid()}")
                .Options;
        }

        [Fact]
        public async Task GetByFileIdAsync_ExistingCheck_ReturnsCheck()
        {
            // Arrange
            Guid fileId = Guid.NewGuid();
            Guid similarFileId = Guid.NewGuid();
            PlagiarismCheckEntity checkEntity = new PlagiarismCheckEntity
            {
                Id = Guid.NewGuid(),
                FileId = fileId,
                Hash = "test-hash-123",
                IsPlagiarized = true,
                SimilarFileId = similarFileId
            };

            using (AnalysisDbContext context = new AnalysisDbContext(_options))
            {
                await context.PlagiarismChecks.AddAsync(checkEntity);
                await context.SaveChangesAsync();
            }

            // Act
            using (AnalysisDbContext context = new AnalysisDbContext(_options))
            {
                PlagiarismCheckRepository repository = new PlagiarismCheckRepository(context);
                PlagiarismCheckEntity? result = await repository.GetByFileIdAsync(fileId);

                // Assert
                Assert.NotNull(result);
                Assert.Equal(fileId, result.FileId);
                Assert.Equal("test-hash-123", result.Hash);
                Assert.True(result.IsPlagiarized);
                Assert.Equal(similarFileId, result.SimilarFileId);
            }
        }

        [Fact]
        public async Task GetByHashAsync_ExistingHash_ReturnsCheck()
        {
            // Arrange
            string hash = "unique-hash-456";
            Guid fileId = Guid.NewGuid();
            PlagiarismCheckEntity checkEntity = new PlagiarismCheckEntity
            {
                Id = Guid.NewGuid(),
                FileId = fileId,
                Hash = hash,
                IsPlagiarized = false,
                SimilarFileId = null
            };

            using (AnalysisDbContext context = new AnalysisDbContext(_options))
            {
                await context.PlagiarismChecks.AddAsync(checkEntity);
                await context.SaveChangesAsync();
            }

            // Act
            using (AnalysisDbContext context = new AnalysisDbContext(_options))
            {
                PlagiarismCheckRepository repository = new PlagiarismCheckRepository(context);
                PlagiarismCheckEntity? result = await repository.GetByHashAsync(hash);

                // Assert
                Assert.NotNull(result);
                Assert.Equal(fileId, result.FileId);
                Assert.Equal(hash, result.Hash);
                Assert.False(result.IsPlagiarized);
                Assert.Null(result.SimilarFileId);
            }
        }

        [Fact]
        public async Task SaveAsync_NewCheck_SavesAndReturnsCheck()
        {
            // Arrange
            PlagiarismCheckEntity newEntity = new PlagiarismCheckEntity
            {
                Id = Guid.NewGuid(),
                FileId = Guid.NewGuid(),
                Hash = "new-hash-789",
                IsPlagiarized = true,
                SimilarFileId = Guid.NewGuid()
            };

            // Act
            using (AnalysisDbContext context = new AnalysisDbContext(_options))
            {
                PlagiarismCheckRepository repository = new PlagiarismCheckRepository(context);
                PlagiarismCheckEntity result = await repository.SaveAsync(newEntity);

                // Assert
                Assert.Equal(newEntity.Id, result.Id);
                Assert.Equal(newEntity.FileId, result.FileId);
                Assert.Equal(newEntity.Hash, result.Hash);
                Assert.Equal(newEntity.IsPlagiarized, result.IsPlagiarized);
                Assert.Equal(newEntity.SimilarFileId, result.SimilarFileId);
            }

            // Проверяем сохранение
            using (AnalysisDbContext context = new AnalysisDbContext(_options))
            {
                PlagiarismCheckEntity? savedEntity = await context.PlagiarismChecks.FindAsync(newEntity.Id);
                Assert.NotNull(savedEntity);
                Assert.Equal(newEntity.FileId, savedEntity.FileId);
                Assert.Equal(newEntity.Hash, savedEntity.Hash);
                Assert.Equal(newEntity.IsPlagiarized, savedEntity.IsPlagiarized);
                Assert.Equal(newEntity.SimilarFileId, savedEntity.SimilarFileId);
            }
        }
    }
    
    
    
    public class WordCloudStorageTests : IDisposable
    {
        private readonly string _testDir;
        private readonly WordCloudStorage _storage;

        public WordCloudStorageTests()
        {
            // Создаем временную директорию для тестов
            _testDir = Path.Combine(Path.GetTempPath(), $"test_wordclouds_{Guid.NewGuid()}");
            
            IOptions<WordCloudStorageSettings> options = Options.Create(new WordCloudStorageSettings { StorageDirectory = _testDir });
            _storage = new WordCloudStorage(options);
        }

        [Fact]
        public async Task SaveWordCloudAsync_ValidImage_SavesAndReturnsLocation()
        {
            // Arrange
            string fileName = "test-cloud";
            byte[] mockImageData = "mock image data"u8.ToArray();
            using MemoryStream stream = new MemoryStream(mockImageData);

            // Act
            string location = await _storage.SaveWordCloudAsync(stream, fileName);

            // Assert
            Assert.NotEmpty(location);
            Assert.EndsWith(".png", location);
            Assert.Contains(fileName, location);

            // Проверяем, что файл действительно создан
            string fullPath = Path.Combine(_testDir, location);
            Assert.True(File.Exists(fullPath));
            byte[] savedData = await File.ReadAllBytesAsync(fullPath);
            Assert.Equal(mockImageData, savedData);
        }

        [Fact]
        public async Task SaveWordCloudAsync_NullOrEmptyStream_ThrowsArgumentException()
        {
            // Arrange
            string fileName = "test-empty";
            Stream emptyStream = new MemoryStream();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _storage.SaveWordCloudAsync(emptyStream, fileName));
            await Assert.ThrowsAsync<ArgumentException>(() => _storage.SaveWordCloudAsync(null, fileName));
        }

        [Fact]
        public async Task GetWordCloudAsync_ExistingFile_ReturnsStream()
        {
            // Arrange
            string fileName = "test-get.png";
            byte[] mockImageData = "test image content"u8.ToArray();
            string filePath = Path.Combine(_testDir, fileName);
            
            await File.WriteAllBytesAsync(filePath, mockImageData);

            // Act
            using Stream stream = await _storage.GetWordCloudAsync(fileName);
            using MemoryStream reader = new MemoryStream();
            await stream.CopyToAsync(reader);
            byte[] readData = reader.ToArray();

            // Assert
            Assert.Equal(mockImageData, readData);
        }

        [Fact]
        public async Task GetWordCloudAsync_NonExistingFile_ThrowsKeyNotFoundException()
        {
            // Arrange
            string nonExistingFile = "non-existing.png";

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() => _storage.GetWordCloudAsync(nonExistingFile));
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
}