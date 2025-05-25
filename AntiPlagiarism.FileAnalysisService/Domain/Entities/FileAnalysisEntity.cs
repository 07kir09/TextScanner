namespace AntiPlagiarism.FileAnalysisService.Domain.Entities
{
    public class FileAnalysisEntity
    {
        public Guid Id { get; set; }
        public Guid FileId { get; set; }
        public int WordCount { get; set; }
        public int ParagraphCount { get; set; }
        public int CharacterCount { get; set; }
        public string? WordCloudLocation { get; set; }
    }
}