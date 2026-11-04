using System.Text;
using ECommerce.Application.Interfaces;
using ECommerce.Infrastructure.Caching;
using ECommerce.Infrastructure.Identity;
using ECommerce.Infrastructure.PaymentGateways;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Infrastructure.Recommendation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;

namespace ECommerce.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        // ---- Database ----
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(config.GetConnectionString("DefaultConnection")));

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        // ---- Redis ----
        var redisConnection = config.GetConnectionString("Redis") ?? "localhost:6379";

        services.AddStackExchangeRedisCache(options => options.Configuration = redisConnection);

        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var options = ConfigurationOptions.Parse(redisConnection);
            options.AbortOnConnectFail = false; // Redis chưa lên thì app vẫn khởi động được, tự kết nối lại sau
            return ConnectionMultiplexer.Connect(options);
        });

        services.AddScoped<ICacheService, RedisCacheService>();
        services.AddScoped<ICartService, CartService>();

        // ---- Recommendation service (Python FastAPI) ----
        var recommenderUrl = config["Recommender:BaseUrl"] ?? "http://localhost:8000";
        services.AddHttpClient<IRecommendationService, RecommendationClient>(client =>
        {
            client.BaseAddress = new Uri(recommenderUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(3); // service lỗi thì fail nhanh để dùng phương án dự phòng
        });

        // ---- Identity ----
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();

        var jwtSettings = config.GetSection("JwtSettings");
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSettings["Secret"]!))
                };
            });

        // ---- Payment (VNPay sandbox) ----
        services.Configure<VnPaySettings>(config.GetSection("VnPay"));
        services.AddScoped<IPaymentGateway, VnPayGateway>();

        return services;
    }
}
