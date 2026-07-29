using Microsoft.EntityFrameworkCore;
using WebApplication1.Entities;

namespace WebApplication1.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options)
{
    public DbSet<ProcessingResult> Results => Set<ProcessingResult>();

    public DbSet<TimeSeriesValue> Values => Set<TimeSeriesValue>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var result = modelBuilder.Entity<ProcessingResult>();

        result.ToTable("Results");
        result.HasKey(item => item.Id);

        result.Property(item => item.FileName)
            .HasMaxLength(255)
            .IsRequired();

        result.Property(item => item.FirstOperationDate)
            .HasColumnType("timestamp with time zone");

        result.HasIndex(item => item.FileName)
            .IsUnique();

        result.HasIndex(item => item.FirstOperationDate);
        result.HasIndex(item => item.AverageValue);
        result.HasIndex(item => item.AverageExecutionTime);

        result.HasMany(item => item.Values)
            .WithOne(item => item.ProcessingResult)
            .HasForeignKey(item => item.ProcessingResultId)
            .OnDelete(DeleteBehavior.Cascade);

        var value = modelBuilder.Entity<TimeSeriesValue>();

        value.ToTable("Values");
        value.HasKey(item => item.Id);

        value.Property(item => item.Date)
            .HasColumnType("timestamp with time zone");

        value.HasIndex(item => new
        {
            item.ProcessingResultId,
            item.Date
        });
    }
}
