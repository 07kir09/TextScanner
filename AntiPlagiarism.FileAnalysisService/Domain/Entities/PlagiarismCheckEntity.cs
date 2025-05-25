namespace AntiPlagiarism.FileAnalysisService.Domain.Entities
{
    public class PlagiarismCheckEntity
    {
        public Guid Id { get; set; }
        public Guid FileId { get; set; }
        public string Hash { get; set; } = string.Empty;
        public bool IsPlagiarized { get; set; }
        public Guid? SimilarFileId { get; set; }
    }
}