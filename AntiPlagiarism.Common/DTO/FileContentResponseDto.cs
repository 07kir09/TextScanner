namespace AntiPlagiarism.Common.DTO
{
    public class FileContentResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Stream Content { get; set; } = null!;
        public string ContentType { get; set; } = "application/octet-stream";
    }
}