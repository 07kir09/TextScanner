using Microsoft.EntityFrameworkCore;
using AntiPlagiarism.FileAnalysisService.Domain.Entities;
namespace AntiPlagiarism.FileAnalysisService.Infrastructure
{
    public class AnalysisDbContext(DbContextOptions<AnalysisDbContext> options) : DbContext(options)
    {
        public DbSet<FileAnalysisEntity> AnalysisResults { get; set; } = null!;
        public DbSet<PlagiarismCheckEntity> PlagiarismChecks { get; set; } = null!;
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FileAnalysisEntity>(entity =>
            {
                entity.ToTable("analysis_results", "analysis");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id)
                    .HasColumnType("uuid")
                    .HasColumnName("id");
                entity.Property(e => e.FileId)
                    .HasColumnType("uuid")
                    .HasColumnName("file_id")
                    .IsRequired();
                entity.Property(e => e.WordCount)
                    .HasColumnName("word_count");
                entity.Property(e => e.ParagraphCount)
                    .HasColumnName("paragraph_count");
                entity.Property(e => e.CharacterCount)
                    .HasColumnName("character_count");
                entity.Property(e => e.WordCloudLocation)
                    .HasColumnName("word_cloud_location")
                    .IsRequired();
            });

            modelBuilder.Entity<PlagiarismCheckEntity>(entity =>
            {
                entity.ToTable("plagiarism_checks", "analysis");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id)
                    .HasColumnType("uuid")
                    .HasColumnName("id");
                entity.Property(e => e.FileId)
                    .HasColumnType("uuid")
                    .HasColumnName("file_id")
                    .IsRequired();
                entity.Property(e => e.Hash)
                    .HasColumnName("hash")
                    .IsRequired();
                entity.Property(e => e.IsPlagiarized)
                    .HasColumnName("is_plagiarized")
                    .IsRequired();
                entity.Property(e => e.SimilarFileId)
                    .HasColumnType("uuid")
                    .HasColumnName("similar_file_id");
                entity.HasIndex(e => e.Hash);
            });
        }
    }
}