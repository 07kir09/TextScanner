using Microsoft.EntityFrameworkCore;

using AntiPlagiarism.FileStoringService.Domain.Entities;
namespace AntiPlagiarism.FileStoringService.Infrastructure
{
    public class FileDbContext : DbContext
    {
        public FileDbContext(DbContextOptions<FileDbContext> options) : base(options) { }
        
        public DbSet<FileEntity> Files { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FileEntity>(entity =>
            {
                entity.ToTable("files", "storage");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id)
                    .HasColumnType("uuid")
                    .HasColumnName("id");
                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasColumnName("name");
                entity.Property(e => e.Location)
                    .IsRequired()
                    .HasColumnName("location");

            });
        }
    }
}