namespace ECommerce.Application.Interfaces;

public record SimilarProduct(Guid ProductId, double Score, List<string> Reasons);

public interface IRecommendationService
{
    // Trả về null khi service gợi ý không phản hồi (để API tự dùng phương án dự phòng)
    Task<List<SimilarProduct>?> GetSimilarAsync(Guid productId, int top, CancellationToken ct = default);
}
