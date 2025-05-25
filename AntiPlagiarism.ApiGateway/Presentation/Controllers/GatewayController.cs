using AntiPlagiarism.ApiGateway.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;


namespace AntiPlagiarism.ApiGateway.Presentation.Controllers
{
    [ApiController]
    [Route("api")]
    public class GatewayController(IProxyService proxyService) : ControllerBase
    {
        [HttpPost("files/upload")]
        public async Task<IActionResult> UploadFile()
        {
            try
            {
                HttpResponseMessage response = await proxyService.ProxyRequestAsync(Request, "FileStoringService", "/api/files/upload");
                return await HandleResponseAsync(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка при загрузке файла: {ex.Message}");
            }
        }

        [HttpGet("files/{id:guid}")]
        public async Task<IActionResult> GetFile(Guid id)
        {
            try
            {
                HttpResponseMessage response = await proxyService.ProxyRequestAsync(Request, "FileStoringService", $"/api/files/{id}");
                return await HandleResponseAsync(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Ошибка при получении файла");
            }
        }

        [HttpPost("file-analysis/analyze/{fileId:guid}")]
        public async Task<IActionResult> AnalyzeFile(Guid fileId)
        {
            try
            {
                HttpResponseMessage response = await proxyService.ProxyRequestAsync(Request, "FileAnalysisService", $"/api/file-analysis/analyze/{fileId}");
                return await HandleResponseAsync(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Ошибка при анализе файла");
            }
        }

        [HttpGet("file-analysis/wordcloud/{location}")]
        public async Task<IActionResult> GetWordCloud(string location)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(location))
                {
                    return BadRequest("Параметр location не может быть пустым");
                }

                HttpResponseMessage response = await proxyService.ProxyRequestAsync(Request, "FileAnalysisService", $"/api/file-analysis/wordcloud/{location}");
                return await HandleResponseAsync(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Ошибка при получении облака слов");
            }
        }
        
        [HttpGet("")]
        public IActionResult Index()
        {
            return Redirect("/swagger");
        }

        private async Task<IActionResult> HandleResponseAsync(HttpResponseMessage response)
        {
            try
            {
                int statusCode = (int)response.StatusCode;
                string contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";

                // Для ошибок возвращаем текст ошибки с соответствующим статус-кодом
                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    return StatusCode(statusCode, errorContent);
                }

                // Для файлов и изображений
                if (contentType.Contains("image/") || contentType.Contains("application/octet-stream"))
                {
                    // Копируем данные в память для безопасного возврата
                    byte[] contentBytes = await response.Content.ReadAsByteArrayAsync();
                    return File(contentBytes, contentType);
                }

                // Для JSON и текстового содержимого
                string textContent = await response.Content.ReadAsStringAsync();
                return Content(textContent, contentType, System.Text.Encoding.UTF8);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка при обработке ответа: {ex.Message}");
            }
        }
    }
}