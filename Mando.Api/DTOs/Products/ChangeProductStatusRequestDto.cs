using System.ComponentModel.DataAnnotations;
using Mando.Api.Enums;

namespace Mando.Api.DTOs.Products;

public class ChangeProductStatusRequestDto
{
    [EnumDataType(typeof(ProductStatus))]
    public ProductStatus Status { get; set; }

    [Required]
    public string RowVersion { get; set; } = string.Empty;
}