using Npgsql.EntityFrameworkCore.PostgreSQL;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Services.Interface;
using Backend.Services.Implementations;
using dotenv.net;
DotEnv.Load();

var builder = WebApplication.CreateBuilder(args);
var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Database connection string is missing or empty. Set the CONNECTION_STRING environment variable.");
}
// var connectionString = "Host=db;Port=5432;Database=sprintforge;Username=postgres;Password=****";

// var connectionString = builder.Configuration["CONNECTION_STRING"];
builder.WebHost.UseUrls("http://0.0.0.0:8080");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddControllers();

// builder.Services.AddDbContext<AppDbContext>(options =>
//     options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProjectService, ProjectService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    int retries = 10; // 🔥 increase this

    while (retries > 0)
    {
        try
        {
            db.Database.Migrate();
            Console.WriteLine("✅ Database migrated successfully");
            break;
        }
        catch (Exception ex)
        {
            retries--;
            Console.WriteLine($"❌ Migration failed. Retrying... {retries}");
            Console.WriteLine(ex.Message);

            Thread.Sleep(5000); // wait 5 sec
        }
    }
}
app.Run();
