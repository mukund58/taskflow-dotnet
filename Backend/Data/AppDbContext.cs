namespace Backend.Data;

using Microsoft.EntityFrameworkCore;
using Backend.Models.Entities;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Project> Projects { get; set; }
    public DbSet<TaskItem> Tasks { get; set; }
    public DbSet<ChecklistItem> ChecklistItems { get; set; }
    public DbSet<TaskActivity> TaskActivities { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TaskItem>().HasQueryFilter(task => !task.IsDeleted);

        modelBuilder.Entity<ChecklistItem>()
            .HasOne(x => x.TaskItem)
            .WithMany()
            .HasForeignKey(x => x.TaskItemId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ChecklistItem>()
            .HasIndex(x => new { x.TaskItemId, x.Position });

        modelBuilder.Entity<TaskActivity>()
            .HasOne(x => x.TaskItem)
            .WithMany()
            .HasForeignKey(x => x.TaskItemId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TaskActivity>()
            .HasIndex(x => new { x.TaskItemId, x.CreatedAt });
    }
}
