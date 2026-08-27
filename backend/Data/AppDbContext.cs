using KampusKayipEsya.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace KampusKayipEsya.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Item> Items => Set<Item>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var item = modelBuilder.Entity<Item>();
        item.ToTable("items");
        item.HasKey(e => e.Id);
        item.Property(e => e.Id).HasColumnName("id");
        item.Property(e => e.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
        item.Property(e => e.Description).HasColumnName("description");
        item.Property(e => e.Location).HasColumnName("location").HasMaxLength(200);
        item.Property(e => e.Category).HasColumnName("category").HasMaxLength(100);
        item.Property(e => e.Contact).HasColumnName("contact").HasMaxLength(200);
        item.Property(e => e.PhotoUrl).HasColumnName("photo_url").HasMaxLength(1000);
        item.Property(e => e.Kind).HasColumnName("kind").HasMaxLength(20).IsRequired();
        item.Property(e => e.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
        item.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone");

        item.HasIndex(e => e.Category);
        item.HasIndex(e => e.Kind);
        item.HasIndex(e => e.Status);
        item.HasIndex(e => e.CreatedAt);
    }
}
