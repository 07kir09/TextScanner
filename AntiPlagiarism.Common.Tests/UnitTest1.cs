using System.Text;
using AntiPlagiarism.Common.Utilities;
using AntiPlagiarism.Common.DTO;
namespace AntiPlagiarism.Common.Tests
{
    public class HashUtilityTests
    {
        [Fact]
        public async Task ComputeSha256Hash_EmptyStream_ReturnsCorrectHash()
        {
            // Arrange
            using MemoryStream stream = new();
            
            // Известный SHA-256 хеш для пустой строки
            string expectedHash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";
            
            // Act
            string resultHash = await HashUtility.ComputeSha256Hash(stream);
            
            // Assert
            Assert.Equal(expectedHash, resultHash);
        }
        
        [Fact]
        public async Task ComputeSha256Hash_SampleText_ReturnsCorrectHash()
        {
            // Arrange
            string testText = "Hello, World!";
            using MemoryStream stream = new(Encoding.UTF8.GetBytes(testText));
            
            // Известный SHA-256 хеш для "Hello, World!"
            string expectedHash = "dffd6021bb2bd5b0af676290809ec3a53191dd81c7f70a4b28688a362182986f";
            
            // Act
            string resultHash = await HashUtility.ComputeSha256Hash(stream);
            
            // Assert
            Assert.Equal(expectedHash, resultHash);
        }
        
        [Fact]
        public async Task ComputeSha256Hash_ResetsStreamPosition()
        {
            // Arrange
            string testText = "Test Content";
            using MemoryStream stream = new(Encoding.UTF8.GetBytes(testText));
            
            // Act
            await HashUtility.ComputeSha256Hash(stream);
            
            // Assert
            Assert.Equal(0, stream.Position);
            
            // Проверяем, что можем прочитать данные после хеширования
            using StreamReader reader = new(stream);
            string readContent = await reader.ReadToEndAsync();
            Assert.Equal(testText, readContent);
        }
    }
    
    public class FileContentResponseDtoTests
    {
        [Fact]
        public void Constructor_DefaultValues_SetsCorrectDefaults()
        {
            // Act
            FileContentResponseDto dto = new FileContentResponseDto();
            
            // Assert
            Assert.Equal(Guid.Empty, dto.Id);
            Assert.Equal(string.Empty, dto.Name);
            Assert.Equal("application/octet-stream", dto.ContentType);
            Assert.Null(dto.Content);
        }

        [Fact]
        public void SetValues_ValidData_PropertiesAreSet()
        {
            // Arrange
            FileContentResponseDto dto = new FileContentResponseDto();
            Guid id = Guid.NewGuid();
            string name = "testfile.txt";
            MemoryStream content = new MemoryStream("Test content"u8.ToArray());
            string contentType = "text/plain";
            
            // Act
            dto.Id = id;
            dto.Name = name;
            dto.Content = content;
            dto.ContentType = contentType;
            
            // Assert
            Assert.Equal(id, dto.Id);
            Assert.Equal(name, dto.Name);
            Assert.Equal(content, dto.Content);
            Assert.Equal(contentType, dto.ContentType);
        }
    }
    
    
    public class FileUploadResponseDtoTests
    {
        [Fact]
        public void Constructor_DefaultValues_SetsCorrectDefaults()
        {
            // Act
            FileUploadResponseDto dto = new FileUploadResponseDto();
            
            // Assert
            Assert.Equal(Guid.Empty, dto.Id);
            Assert.Equal(string.Empty, dto.Name);
            Assert.Equal(0, dto.Size);
            // DateTime.MinValue и DateTime.UtcNow будут отличаться
            Assert.True(dto.UploadedAt.Date <= DateTime.UtcNow.Date);
        }
        
        [Fact]
        public void SetValues_ValidData_PropertiesAreSet()
        {
            // Arrange
            FileUploadResponseDto dto = new FileUploadResponseDto();
            Guid id = Guid.NewGuid();
            string name = "testfile.txt";
            long size = 1024;
            DateTime uploadDate = new DateTime(2023, 1, 1);
            
            // Act
            dto.Id = id;
            dto.Name = name;
            dto.Size = size;
            dto.UploadedAt = uploadDate;
            
            // Assert
            Assert.Equal(id, dto.Id);
            Assert.Equal(name, dto.Name);
            Assert.Equal(size, dto.Size);
            Assert.Equal(uploadDate, dto.UploadedAt);
        }
    }
    
    public class FileAnalysisResultDtoTests
    {
        [Fact]
        public void Constructor_DefaultValues_SetsCorrectDefaults()
        {
            // Act
            FileAnalysisResultDto dto = new FileAnalysisResultDto();
            
            // Assert
            Assert.Equal(Guid.Empty, dto.FileId);
            Assert.Equal(0, dto.ParagraphCount);
            Assert.Equal(0, dto.WordCount);
            Assert.Equal(0, dto.CharacterCount);
            Assert.Equal(string.Empty, dto.WordCloudLocation);
            Assert.False(dto.IsPlagiarism);
            Assert.Null(dto.OriginalFileId);
        }
        
        [Fact]
        public void SetValues_ValidData_PropertiesAreSet()
        {
            // Arrange
            FileAnalysisResultDto dto = new FileAnalysisResultDto();
            Guid fileId = Guid.NewGuid();
            Guid originalFileId = Guid.NewGuid();
            
            // Act
            dto.FileId = fileId;
            dto.ParagraphCount = 10;
            dto.WordCount = 100;
            dto.CharacterCount = 500;
            dto.WordCloudLocation = "wordclouds/123.png";
            dto.IsPlagiarism = true;
            dto.OriginalFileId = originalFileId;
            
            // Assert
            Assert.Equal(fileId, dto.FileId);
            Assert.Equal(10, dto.ParagraphCount);
            Assert.Equal(100, dto.WordCount);
            Assert.Equal(500, dto.CharacterCount);
            Assert.Equal("wordclouds/123.png", dto.WordCloudLocation);
            Assert.True(dto.IsPlagiarism);
            Assert.Equal(originalFileId, dto.OriginalFileId);
        }
    }
    
    
    public class WordCloudResponseDtoTests
    {
        [Fact]
        public void Constructor_DefaultValues_PropertiesAreNull()
        {
            // Act
            WordCloudResponseDto dto = new WordCloudResponseDto();
            
            // Assert
            Assert.Null(dto.Location);
        }
        
        [Fact]
        public void SetLocation_ValidValue_LocationIsSet()
        {
            // Arrange
            WordCloudResponseDto dto = new WordCloudResponseDto();
            string location = "wordclouds/123.png";
            
            // Act
            dto.Location = location;
            
            // Assert
            Assert.Equal(location, dto.Location);
        }
    }
    
    public class FileDtoTests
    {
        [Fact]
        public void Constructor_DefaultValues_SetsCorrectDefaults()
        {
            // Act
            FileDto dto = new FileDto();
        
            // Assert
            Assert.Equal(Guid.Empty, dto.Id);
            Assert.Equal(string.Empty, dto.Name);
            Assert.Equal(string.Empty, dto.Location);
        }
    
        [Fact]
        public void SetValues_ValidData_PropertiesAreSet()
        {
            // Arrange
            FileDto dto = new FileDto();
            Guid id = Guid.NewGuid();
            string name = "test.txt";
            string location = "files/test.txt";
        
            // Act
            dto.Id = id;
            dto.Name = name;
            dto.Location = location;
        
            // Assert
            Assert.Equal(id, dto.Id);
            Assert.Equal(name, dto.Name);
            Assert.Equal(location, dto.Location);
        }
    }
}