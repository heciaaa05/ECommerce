namespace ECommerce.Application.DTOs.Categories;

public record CategoryDto(
    Guid Id,
    string Name,
    string Slug,
    Guid? ParentCategoryId
);
