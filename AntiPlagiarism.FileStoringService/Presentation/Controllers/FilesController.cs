using AntiPlagiarism.Common.DTO;
using AntiPlagiarism.FileStoringService.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AntiPlagiarism.FileStoringService.Presentation.Controllers
{
    [ApiController]
    [Route("api/files")]
    public class FilesController(IFileService fileService) : ControllerBase
    {
        [HttpPost("upload")]
        public async Task<ActionResult<FileDto>> UploadFile([FromForm] IFormFile? file)
        {
            if (file == null)
            {
                return BadRequest("Файл не предоставлен.");
            }
            
            try 
            {
                FileDto result = await fileService.UploadFileAsync(file);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка при загрузке файла: {ex.Message}");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetFile(Guid id)
        {
            try
            {
                Stream stream = await fileService.GetFileAsync(id);
                return File(stream, "application/octet-stream");
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }
    }
}