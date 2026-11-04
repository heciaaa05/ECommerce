using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ECommerce.Infrastructure.Persistence;

// Design-time factory: giúp `dotnet ef` tạo DbContext độc lập với DI runtime của API
// (tránh lỗi "Unable to resolve service for type DbContextOptions<ApplicationDbContext>")
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var appSettingsPath = FindAppSettings();

        var configuration = new ConfigurationBuilder()
            .AddJsonFile(appSettingsPath, optional: false)
            .AddJsonFile(Path.Combine(Path.GetDirectoryName(appSettingsPath)!, "appsettings.Development.json"), optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Thiếu ConnectionStrings:DefaultConnection trong appsettings.json");

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new ApplicationDbContext(optionsBuilder.Options);
    }

    // Tìm src/ECommerce.API/appsettings.json dù chạy lệnh từ thư mục gốc solution hay từ thư mục project
    private static string FindAppSettings()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "src", "ECommerce.API", "appsettings.json");
            if (File.Exists(candidate)) return candidate;

            candidate = Path.Combine(dir.FullName, "ECommerce.API", "appsettings.json");
            if (File.Exists(candidate)) return candidate;

            dir = dir.Parent;
        }

        throw new FileNotFoundException("Không tìm thấy src/ECommerce.API/appsettings.json. Hãy chạy lệnh dotnet ef từ thư mục gốc của solution.");
    }
}
