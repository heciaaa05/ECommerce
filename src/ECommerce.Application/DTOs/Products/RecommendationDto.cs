namespace ECommerce.Application.DTOs.Products;

public record RecommendationDto(ProductDto Product, double Score, List<string> Reasons);
