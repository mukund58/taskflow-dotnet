using AspNetCoreRateLimit;
using Backend.Middleware;
using Serilog;

namespace Backend.Extensions;

public static class WebApplicationExtensions
{
    public static IHostBuilder AddSerilogConfig(this IHostBuilder host)
    {
        return host.UseSerilog((context, services, configuration) =>
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext());
    }

    public static WebApplication UseApplicationMiddleware(this WebApplication app)
    {
        app.UseSerilogRequestLogging();

        app.UseCors("AllowAllOrigins");
        app.UseMiddleware<GlobalExceptionMiddleware>();

        // HTTPS Redirection can be enabled for production if you have proper certs, but we leave it out/commented for local docker
        // app.UseHttpsRedirection();

        app.UseIpRateLimiting();
        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }

    public static WebApplication UseSwaggerDocumentation(this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Task Flow API v1");
        });

        return app;
    }
}
