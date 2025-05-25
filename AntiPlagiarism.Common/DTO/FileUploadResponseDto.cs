namespace AntiPlagiarism.Common.DTO
{
    public class FileUploadResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public long Size { get; set; }
        public DateTime UploadedAt { get; set; }
    }
}