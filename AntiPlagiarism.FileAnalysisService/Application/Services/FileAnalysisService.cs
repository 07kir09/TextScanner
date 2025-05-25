using AntiPlagiarism.Common.DTO;
using AntiPlagiarism.Common.Utilities;
using AntiPlagiarism.FileAnalysisService.Application.Interfaces;
using AntiPlagiarism.FileAnalysisService.Domain.Entities;
using AntiPlagiarism.FileAnalysisService.Domain.Interfaces;
namespace AntiPlagiarism.FileAnalysisService.Application.Services
{
    public class FileAnalysisService(
        IAnalysisResultRepository analysisResultRepository,
        IPlagiarismCheckRepository plagiarismCheckRepository,
        IWordCloudStorage wordCloudStorage,
        HttpClient httpClient)
        : IFileAnalysisService
    {
        public async Task<FileAnalysisResultDto> AnalyzeFileAsync(Guid fileId)
        {
            // Проверяем, есть ли уже результат анализа
            FileAnalysisEntity? existingResult = await analysisResultRepository.GetByFileIdAsync(fileId);
            
            if (existingResult != null)
            {
                PlagiarismCheckEntity? existingPlagiarismCheck = await plagiarismCheckRepository.GetByFileIdAsync(fileId);
                return new FileAnalysisResultDto
                {
                    FileId = fileId,
                    ParagraphCount = existingResult.ParagraphCount,
                    WordCount = existingResult.WordCount,
                    CharacterCount = existingResult.CharacterCount,
                    WordCloudLocation = existingResult.WordCloudLocation,
                    IsPlagiarism = existingPlagiarismCheck?.IsPlagiarized ?? false,
                    OriginalFileId = existingPlagiarismCheck?.SimilarFileId
                };
            }

            // Запрашиваем файл из FileStoringService
            HttpResponseMessage response = await httpClient.GetAsync($"http://file-storing-service/api/files/{fileId}");
            response.EnsureSuccessStatusCode();
            await using Stream fileStream = await response.Content.ReadAsStreamAsync();

            
            // Создаем копию содержимого в памяти
            MemoryStream memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            // Анализ текста
            using StreamReader reader = new StreamReader(memoryStream);
            string content = await reader.ReadToEndAsync();
            (int ParagraphCount, int WordCount, int CharacterCount) analysis = AnalyzeText(content);

            // Сброс позиции для вычисления хеша
            memoryStream.Position = 0;
            string hash = await HashUtility.ComputeSha256Hash(memoryStream);


            Task<Stream> wordCloudStream = GenerateWordCloud(content); // Заглушка, см. ниже

            // Сохраняем облако слов
            string wordCloudLocation = await wordCloudStorage.SaveWordCloudAsync(await wordCloudStream, $"wordcloud-{fileId}");

            // Проверка на плагиат
            PlagiarismCheckEntity? existingCheck = await plagiarismCheckRepository.GetByHashAsync(hash);
            PlagiarismCheckEntity plagiarismCheck = new PlagiarismCheckEntity
            {
                Id = Guid.NewGuid(),
                FileId = fileId,
                Hash = hash,
                IsPlagiarized = existingCheck != null,
                SimilarFileId = existingCheck?.FileId
            };
            await plagiarismCheckRepository.SaveAsync(plagiarismCheck);

            // Сохраняем результат анализа
            FileAnalysisEntity analysisResult = new FileAnalysisEntity
            {
                Id = Guid.NewGuid(),
                FileId = fileId,
                ParagraphCount = analysis.ParagraphCount,
                WordCount = analysis.WordCount,
                CharacterCount = analysis.CharacterCount,
                WordCloudLocation = wordCloudLocation
            };
            await analysisResultRepository.SaveAsync(analysisResult);

            return new FileAnalysisResultDto
            {
                FileId = fileId,
                ParagraphCount = analysis.ParagraphCount,
                WordCount = analysis.WordCount,
                CharacterCount = analysis.CharacterCount,
                WordCloudLocation = wordCloudLocation,
                IsPlagiarism = plagiarismCheck.IsPlagiarized,
                OriginalFileId = plagiarismCheck.SimilarFileId
            };
        }
        
        
        public async Task<Stream> GetWordCloudAsync(string location)
        {
            return await wordCloudStorage.GetWordCloudAsync(location);
        }
        
        
        private (int ParagraphCount, int WordCount, int CharacterCount) AnalyzeText(string content)
        {
            string[] paragraphs = content.Split(["\r\n\r\n", "\n\n"], StringSplitOptions.RemoveEmptyEntries);
            string[] words = content.Split([' ', '\t', '\n'], StringSplitOptions.RemoveEmptyEntries);
            return (
                ParagraphCount: paragraphs.Length,
                WordCount: words.Length,
                CharacterCount: content.Length
            );
        }
        
        
        private async Task<Stream> GenerateWordCloud(string content)
        {
            var request = new
            {
                format = "png",
                width = 800,
                height = 600,
                fontFamily = "sans-serif",
                fontScale = 19,
                scale = "linear",
                removeStopwords = true,
                minWordLength = 4,
                text = content
            };

            try
            {
                HttpResponseMessage response =
                    await httpClient.PostAsJsonAsync("https://quickchart.io/wordcloud", request);
                response.EnsureSuccessStatusCode();
                Stream stream = await response.Content.ReadAsStreamAsync();
                MemoryStream memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                memoryStream.Position = 0;
                return memoryStream;
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Ошибка при генерации облака слов: {ex.Message}", ex);
            }
        }
    }
}