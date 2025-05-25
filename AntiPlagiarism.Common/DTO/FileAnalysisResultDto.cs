namespace AntiPlagiarism.Common.DTO
{
    public class FileAnalysisResultDto
    {
        public Guid FileId { get; set; }
        public int ParagraphCount { get; set; }
        public int WordCount { get; set; }
        public int CharacterCount { get; set; }
        public string? WordCloudLocation { get; set; } = string.Empty;
        public bool IsPlagiarism { get; set; }
        public Guid? OriginalFileId { get; set; }
    }
}