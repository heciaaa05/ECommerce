using ECommerce.API.BackgroundJobs;
using ECommerce.API.Health;
using ECommerce.Application;
using ECommerce.Application.Interfaces;
using ECommerce.Infrastructure;
using ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Đăng ký các layer
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swashbuckle 10 + Microsoft.OpenApi 2.x: namespace là Microsoft.OpenApi (không còn .Models)
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Dán accessToken vào đây (Swagger tự thêm tiền tố Bearer)"
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

builder.Services.AddHostedService<OrderExpirationJob>();

builder.Services.AddHealthChecks()
    .AddCheck<ReadinessHealthCheck>("ready", tags: new[] { "ready" });

var app = builder.Build();

// Chạy trong Docker: tự áp migration khi khởi động (bật bằng Database__AutoMigrate=true)
if (app.Configuration.GetValue<bool>("Database:AutoMigrate"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();

    // Nạp ~30 sản phẩm gaming gear mẫu (chỉ khi DB chưa có sản phẩm nào): Database__SeedDemoData=true
    if (app.Configuration.GetValue<bool>("Database:SeedDemoData"))
    {
        await DbSeeder.SeedDemoDataAsync(db);

        // Seeder ghi thẳng vào DB nên phải xoá cache cũ (vd danh mục rỗng đã bị cache)
        var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();
        await cache.RemoveAsync("categories:all");
        await cache.RemoveByPrefixAsync("products:");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Liveness: process còn sống. Readiness: DB + Redis còn kết nối được.
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = async (ctx, report) =>
    {
        ctx.Response.ContentType = "application/json";
        var body = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.ToDictionary(e => e.Key, e => e.Value.Data)
        };
        await ctx.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(body));
    }
});

app.Run();
