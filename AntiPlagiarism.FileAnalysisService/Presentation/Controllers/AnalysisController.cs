using AntiPlagiarism.Common.DTO;
using AntiPlagiarism.FileAnalysisService.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AntiPlagiarism.FileAnalysisService.Presentation.Controllers
{
    
    [ApiController]
    [Route("api/file-analysis")]
    public class AnalysisController(IFileAnalysisService fileAnalysisService) : ControllerBase
    {
        [HttpPost("analyze/{fileId:guid}")]
        public async Task<ActionResult<FileAnalysisResultDto>> AnalyzeFile(Guid fileId)
        {
            try
            {
                FileAnalysisResultDto result = await fileAnalysisService.AnalyzeFileAsync(fileId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка при анализе файла: {ex.Message}");
            }
        }
        
        
        [HttpGet("wordcloud/{location}")]
        public async Task<IActionResult> GetWordCloud(string location)
        {
            try
            {
                Stream wordCloudStream = await fileAnalysisService.GetWordCloudAsync(location);
                return File(wordCloudStream, "image/png");
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка при получении облака слов: {ex.Message}");
            }
        }
    }
}