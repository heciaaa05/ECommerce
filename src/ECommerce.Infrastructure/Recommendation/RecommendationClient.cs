using System.Net;
using System.Net.Http.Json;
using ECommerce.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Recommendation;

public class RecommendationClient : IRecommendationService
{
    private readonly HttpClient _http;
    private readonly ILogger<RecommendationClient> _logger;

    public RecommendationClient(HttpClient http, ILogger<RecommendationClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public record RecItem(Guid ProductId, double Score, List<string>? Reasons);
    public record RecResponse(Guid ProductId, List<RecItem> Items);

    public async Task<List<SimilarProduct>?> GetSimilarAsync(Guid productId, int top, CancellationToken ct = default)
    {
        try
        {
            using var response = await _http.GetAsync($"recommend/{productId}?top={top}", ct);

            // Sản phẩm mới tạo chưa có trong index của service gợi ý: coi như chưa có gợi ý
            if (response.StatusCode == HttpStatusCode.NotFound)
                return new List<SimilarProduct>();

            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadFromJsonAsync<RecResponse>(cancellationToken: ct);
            return body?.Items
                .Select(i => new SimilarProduct(i.ProductId, i.Score, i.Reasons ?? new List<string>()))
                .ToList();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or NotSupportedException or System.Text.Json.JsonException)
        {
            _logger.LogWarning(ex, "Service gợi ý không phản hồi, dùng phương án dự phòng");
            return null;
        }
    }
}
