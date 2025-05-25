namespace AntiPlagiarism.FileStoringService.Domain.Entities
{
    public class FileEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
    }
}