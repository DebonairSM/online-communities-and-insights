using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OnlineCommunities.Infrastructure.Data;

namespace OnlineCommunities.Integration.Tests;

/// <summary>
/// Custom WebApplicationFactory for integration tests.
/// Configures the test server with in-memory database and proper content root.
/// </summary>
public class WebAppFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set content root to the API project directory
        // From tests/Integration.Tests/bin/Debug/net9.0 navigate to src/Api
        var apiProjectPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "..", "..", "..", "..", "..",
            "src", "Api");
        
        var fullPath = Path.GetFullPath(apiProjectPath);
        builder.UseContentRoot(fullPath);
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove all existing DbContext and DbContextOptions registrations
            var descriptors = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions) ||
                d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                d.ServiceType == typeof(ApplicationDbContext)).ToList();

            foreach (var descriptor in descriptors)
            {
                services.Remove(descriptor);
            }

            // Add in-memory database for testing - use a shared name so all tests use same instance
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase("SharedTestDatabase");
            });
        });
    }
}

