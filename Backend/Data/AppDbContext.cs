namespace Backend.Data;

using Microsoft.EntityFrameworkCore;
using Backend.Models.Entities;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Project> Projects { get; set; }
    public DbSet<ProjectMember> ProjectMembers { get; set; }
    public DbSet<ProjectInvitation> ProjectInvitations { get; set; }
    public DbSet<TaskItem> Tasks { get; set; }
    public DbSet<ChecklistItem> ChecklistItems { get; set; }
    public DbSet<TaskActivity> TaskActivities { get; set; }
    public DbSet<TaskComment> Comments { get; set; }
    public DbSet<Label> Labels { get; set; }
    public DbSet<TaskAttachment> Attachments { get; set; }
    public DbSet<TaskWatcher> TaskWatchers { get; set; }
    public DbSet<Notification> Notifications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Project>()
            .HasOne(p => p.OwnerUser)
            .WithMany()
            .HasForeignKey(p => p.OwnerUserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ProjectMember>()
            .HasKey(pm => new { pm.ProjectId, pm.UserId });

        modelBuilder.Entity<ProjectMember>()
            .HasOne(pm => pm.Project)
            .WithMany()
            .HasForeignKey(pm => pm.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProjectMember>()
            .HasOne(pm => pm.User)
            .WithMany()
            .HasForeignKey(pm => pm.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProjectMember>()
            .HasOne(pm => pm.AddedByUser)
            .WithMany()
            .HasForeignKey(pm => pm.AddedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ProjectMember>()
            .HasIndex(pm => new { pm.ProjectId, pm.Role });

        modelBuilder.Entity<ProjectInvitation>()
            .HasOne(pi => pi.Project)
            .WithMany()
            .HasForeignKey(pi => pi.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProjectInvitation>()
            .HasOne(pi => pi.InvitedByUser)
            .WithMany()
            .HasForeignKey(pi => pi.InvitedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ProjectInvitation>()
            .HasIndex(pi => new { pi.ProjectId, pi.Email, pi.Status });

        var taskEntity = modelBuilder.Entity<TaskItem>();
        taskEntity.HasQueryFilter(task => !task.IsDeleted);
        taskEntity.Property(task => task.RowVersion)
            .IsConcurrencyToken()
            .HasDefaultValue(1L);
        taskEntity.HasIndex(task => task.Status);
        taskEntity.HasIndex(task => task.AssignedUserId);
        taskEntity.HasIndex(task => task.ProjectId);

        modelBuilder.Entity<TaskComment>().HasQueryFilter(comment => !comment.IsDeleted);
        modelBuilder.Entity<ChecklistItem>().HasQueryFilter(item => !item.IsDeleted);

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
            .HasQueryFilter(activity => !activity.TaskItem.IsDeleted);

        modelBuilder.Entity<TaskActivity>()
            .HasIndex(x => new { x.TaskItemId, x.CreatedAt });

        // Foreign key relationships
        modelBuilder.Entity<TaskComment>()
            .HasOne(tc => tc.Task)
            .WithMany()
            .HasForeignKey(tc => tc.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TaskComment>()
            .HasOne(tc => tc.Author)
            .WithMany()
            .HasForeignKey(tc => tc.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        // Label and TaskLabel Configuration
        modelBuilder.Entity<Label>().HasQueryFilter(label => !label.IsDeleted);
        modelBuilder.Entity<Label>()
            .HasMany(l => l.TaskLabels)
            .WithOne(tl => tl.Label)
            .HasForeignKey(tl => tl.LabelId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TaskLabel>()
            .HasKey(tl => new { tl.TaskId, tl.LabelId });

        modelBuilder.Entity<TaskLabel>()
            .HasQueryFilter(taskLabel => !taskLabel.Task.IsDeleted && !taskLabel.Label.IsDeleted);

        // TaskAttachment Configuration
        modelBuilder.Entity<TaskAttachment>().HasQueryFilter(ta => !ta.IsDeleted);
        modelBuilder.Entity<TaskAttachment>()
            .HasOne(ta => ta.Task)
            .WithMany()
            .HasForeignKey(ta => ta.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TaskAttachment>()
            .HasOne(ta => ta.UploadedByUser)
            .WithMany()
            .HasForeignKey(ta => ta.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // TaskWatcher Configuration (Many-to-Many)
        modelBuilder.Entity<TaskWatcher>()
            .HasKey(tw => new { tw.TaskId, tw.UserId });

        modelBuilder.Entity<TaskWatcher>()
            .HasQueryFilter(taskWatcher => !taskWatcher.Task.IsDeleted);

        modelBuilder.Entity<TaskWatcher>()
            .HasOne(tw => tw.Task)
            .WithMany()
            .HasForeignKey(tw => tw.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TaskWatcher>()
            .HasOne(tw => tw.User)
            .WithMany()
            .HasForeignKey(tw => tw.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Notification Configuration
        modelBuilder.Entity<Notification>().HasQueryFilter(n => !n.IsDeleted);
        modelBuilder.Entity<Notification>()
            .HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Notification>()
            .HasOne(n => n.Task)
            .WithMany()
            .HasForeignKey(n => n.TaskId)
            .OnDelete(DeleteBehavior.SetNull);
    }

    public override int SaveChanges()
    {
        ApplyTaskRowVersionUpdates();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyTaskRowVersionUpdates();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyTaskRowVersionUpdates()
    {
        foreach (var entry in ChangeTracker.Entries<TaskItem>())
        {
            if (entry.State == EntityState.Added && entry.Entity.RowVersion <= 0)
            {
                entry.Property(task => task.RowVersion).CurrentValue = 1;
                continue;
            }

            if (entry.State != EntityState.Modified)
                continue;

            var rowVersionProperty = entry.Property(task => task.RowVersion);
            var nextRowVersion = Math.Max(1, rowVersionProperty.OriginalValue) + 1;
            rowVersionProperty.CurrentValue = nextRowVersion;
        }
    }
}
