namespace ECommerce.Application.DTOs.Categories;

public record CreateCategoryRequest(
    string Name,
    Guid? ParentCategoryId
);
