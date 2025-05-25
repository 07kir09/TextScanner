using AntiPlagiarism.ApiGateway.Application.Interfaces;
using Microsoft.Extensions.Primitives;
using System.Net.Http.Headers;

namespace AntiPlagiarism.ApiGateway.Application.Services
{
    public class ProxyService(IHttpClientFactory httpClientFactory, ILogger<ProxyService> logger)
        : IProxyService
    {
        public async Task<HttpResponseMessage> ProxyRequestAsync(HttpRequest request, string targetService, string targetPath)
        {
            try
            {
                logger.LogInformation($"Проксирование запроса к {targetService} по пути {targetPath}");
                
                HttpClient client = httpClientFactory.CreateClient(targetService);
                string path = targetPath.StartsWith("/") ? targetPath.Substring(1) : targetPath;
                HttpRequestMessage requestMessage = new HttpRequestMessage
                {
                    Method = new HttpMethod(request.Method),
                    RequestUri = new Uri($"{client.BaseAddress}{path}{request.QueryString}")
                };

                // Копируем заголовки
                foreach (KeyValuePair<string, StringValues> header in request.Headers)
                {
                    if (!header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase) &&
                        !header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase) &&
                        !header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                    {
                        requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                    }
                }

                // Обработка загрузки файлов с multipart/form-data
                if (request.HasFormContentType)
                {
                    MultipartFormDataContent formContent = new();
                    
                    // Обработка файлов
                    foreach (IFormFile file in request.Form.Files)
                    {
                        byte[] fileBytes;
                        using (MemoryStream memoryStream = new())
                        {
                            await file.CopyToAsync(memoryStream);
                            fileBytes = memoryStream.ToArray();
                        }

                        ByteArrayContent fileContent = new(fileBytes);
                        if (!string.IsNullOrEmpty(file.ContentType))
                        {
                            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(file.ContentType);
                        }
                        
                        formContent.Add(fileContent, "file", file.FileName);
                        logger.LogInformation($"Добавлен файл: {file.FileName}, размер: {file.Length} байт");
                    }
                    
                    // Добавляем поля формы
                    foreach (string key in request.Form.Keys)
                    {
                        formContent.Add(new StringContent(request.Form[key]), key);
                    }
                    
                    requestMessage.Content = formContent;
                }
                else if (request.ContentLength > 0)
                {
                    // Обработка других типов содержимого
                    request.EnableBuffering();
                    
                    MemoryStream ms = new MemoryStream();
                    await request.Body.CopyToAsync(ms);
                    ms.Position = 0;
                    request.Body.Position = 0;

                    ByteArrayContent bodyContent = new ByteArrayContent(ms.ToArray());
                    await ms.DisposeAsync();
                    
                    if (request.ContentType != null)
                    {
                        bodyContent.Headers.ContentType = MediaTypeHeaderValue.Parse(request.ContentType);
                    }
                    
                    requestMessage.Content = bodyContent;
                }

                Console.WriteLine($"Проксирование запроса к {targetService} по пути {targetPath}");
                logger.LogInformation($"Отправка запроса к {targetService}");
                HttpResponseMessage response = await client.SendAsync(requestMessage);
                Console.WriteLine($"Получен ответ от {targetService} со статусом {response.StatusCode}");
                logger.LogInformation($"Получен ответ от {targetService} со статусом {response.StatusCode}");
                
                return response;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Ошибка при проксировании запроса к {targetService}: {ex.Message}");
                throw;
            }
        }
    }
}