namespace Backend.Data;

using Microsoft.EntityFrameworkCore;
using Backend.Models.DTOs;
using Backend.Models.Entities;


public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Project> Projects { get; set; }
    public DbSet<TaskItem> Tasks { get; set; }
}
