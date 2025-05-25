using AntiPlagiarism.ApiGateway.Application.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Moq;
using Moq.Protected;
using System.Net;

namespace AntiPlagiarism.ApiGateway.Tests
{
    public class ProxyServiceTests
    {
        private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
        private readonly Mock<ILogger<ProxyService>> _mockLogger;
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly ProxyService _proxyService;

        public ProxyServiceTests()
        {
            _mockHttpClientFactory = new Mock<IHttpClientFactory>();
            _mockLogger = new Mock<ILogger<ProxyService>>();
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();

            _proxyService = new ProxyService(
                _mockHttpClientFactory.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task ProxyRequestAsync_SimpleRequest_ForwardsRequestCorrectly()
        {
            // Arrange
            const string targetService = "TestService";
            const string targetPath = "/api/test";
            Uri baseUri = new Uri("http://testservice:5000/");

            // Настраиваем мок HttpClient
            HttpClient httpClient = new HttpClient(_mockHttpMessageHandler.Object)
            {
                BaseAddress = baseUri
            };

            _mockHttpClientFactory
                .Setup(x => x.CreateClient(targetService))
                .Returns(httpClient);

            // Настраиваем мок HttpResponse
            HttpResponseMessage mockResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{'result': 'success'}")
            };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(mockResponse);

            // Создаем тестовый HttpRequest
            DefaultHttpContext context = new DefaultHttpContext();
            HttpRequest request = context.Request;
            request.Method = "GET";
            request.QueryString = new QueryString("?param=value");
            request.Headers.Add("Custom-Header", "TestValue");

            // Act
            HttpResponseMessage result = await _proxyService.ProxyRequestAsync(request, targetService, targetPath);

            // Assert
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            
            // Проверяем, что запрос был отправлен с правильными параметрами
            _mockHttpMessageHandler
                .Protected()
                .Verify(
                    "SendAsync",
                    Times.Once(),
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get &&
                        req.RequestUri.ToString() == "http://testservice:5000/api/test?param=value" &&
                        req.Headers.Contains("Custom-Header")),
                    ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task ProxyRequestAsync_WithFormData_ForwardsFormDataCorrectly()
        {
            // Arrange
            const string targetService = "FileService";
            const string targetPath = "/api/upload";
            Uri baseUri = new Uri("http://fileservice:5000/");

            // Настраиваем мок HttpClient
            HttpClient httpClient = new HttpClient(_mockHttpMessageHandler.Object)
            {
                BaseAddress = baseUri
            };

            _mockHttpClientFactory
                .Setup(x => x.CreateClient(targetService))
                .Returns(httpClient);

            // Настраиваем мок HttpResponse
            HttpResponseMessage mockResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{'fileId': 'abc123'}")
            };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(mockResponse);

            // Создаем тестовый HttpRequest с form-data
            DefaultHttpContext context = new DefaultHttpContext();
            HttpRequest request = context.Request;
            request.Method = "POST";
            request.ContentType = "multipart/form-data; boundary=boundary";

            // Мокаем IFormFile
            Mock<IFormFile> mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("test.txt");
            mockFile.Setup(f => f.ContentType).Returns("text/plain");
            mockFile.Setup(f => f.Length).Returns(10);
            mockFile.Setup(f => f.CopyToAsync(It.IsAny<MemoryStream>(), It.IsAny<CancellationToken>()))
                .Callback<Stream, CancellationToken>((stream, token) =>
                {
                    byte[] data = "test data"u8.ToArray();
                    stream.Write(data, 0, data.Length);
                });

            // Мокаем Form и FormFiles
            FormFileCollection formFileCollection = new FormFileCollection();
            formFileCollection.Add(mockFile.Object);

            FormCollection formCollection = new FormCollection(
                new Dictionary<string, StringValues> { { "description", new StringValues("test description") } },
                formFileCollection);

            // Устанавливаем Form для request через FormFeature
            FormFeatureMock formFeature = new FormFeatureMock(formCollection);
            context.Features.Set<IFormFeature>(formFeature);

            // Act
            HttpResponseMessage result = await _proxyService.ProxyRequestAsync(request, targetService, targetPath);

            // Assert
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            _mockHttpMessageHandler.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri.ToString() == "http://fileservice:5000/api/upload" &&
                    req.Content is MultipartFormDataContent),
                ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task ProxyRequestAsync_WithJsonBody_ForwardsBodyCorrectly()
        {
            // Arrange
            const string targetService = "DataService";
            const string targetPath = "/api/data";
            Uri baseUri = new Uri("http://dataservice:5000/");
            string jsonContent = "{\"property\":\"value\"}";

            // Настраиваем мок HttpClient
            HttpClient httpClient = new HttpClient(_mockHttpMessageHandler.Object)
            {
                BaseAddress = baseUri
            };

            _mockHttpClientFactory
                .Setup(x => x.CreateClient(targetService))
                .Returns(httpClient);

            // Настраиваем мок HttpResponse
            HttpResponseMessage mockResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Created,
                Content = new StringContent("{\"id\":\"123\"}")
            };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(mockResponse);

            // Создаем тестовый HttpRequest с JSON телом
            DefaultHttpContext context = new DefaultHttpContext();
            HttpRequest request = context.Request;
            request.Method = "POST";
            request.ContentType = "application/json";

            // Добавляем тело запроса
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(jsonContent);
            request.Body = new MemoryStream(bytes);
            request.ContentLength = bytes.Length;

            // Act
            HttpResponseMessage result = await _proxyService.ProxyRequestAsync(request, targetService, targetPath);

            // Assert
            Assert.Equal(HttpStatusCode.Created, result.StatusCode);
            _mockHttpMessageHandler.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri.ToString() == "http://dataservice:5000/api/data" &&
                    req.Content is ByteArrayContent),
                ItExpr.IsAny<CancellationToken>());
        }
        
        [Fact]
        public async Task ProxyRequestAsync_ErrorDuringRequest_ThrowsException()
        {
            // Arrange
            const string targetService = "ErrorService";
            const string targetPath = "/api/error";
            Uri baseUri = new Uri("http://errorservice:5000/");

            // Настраиваем мок HttpClient
            HttpClient httpClient = new HttpClient(_mockHttpMessageHandler.Object)
            {
                BaseAddress = baseUri
            };

            _mockHttpClientFactory
                .Setup(x => x.CreateClient(targetService))
                .Returns(httpClient);

            // Настраиваем мок HttpMessageHandler для генерации исключения
            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("Connection refused"));

            // Создаем тестовый HttpRequest
            DefaultHttpContext context = new DefaultHttpContext();
            HttpRequest request = context.Request;
            request.Method = "GET";

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(() =>
                _proxyService.ProxyRequestAsync(request, targetService, targetPath));
            
            // Проверяем логирование ошибки
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
    }
    
    public class FormFeatureMock(IFormCollection form) : IFormFeature
    {
        private IFormCollection _form = form;

        public bool HasFormContentType => true;

        public IFormCollection Form 
        { 
            get => _form;
            set => _form = value;
        }

        public IFormCollection ReadForm()
        {
            return _form;
        }

        public Task<IFormCollection> ReadFormAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_form);
        }
    }
    
}