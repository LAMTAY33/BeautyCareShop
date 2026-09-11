using System.ComponentModel.DataAnnotations;

namespace BeautyCareShop.Models;

public class Category
{
    public int Id { get; set; }
    [Required, MaxLength(120)] public string Name { get; set; } = string.Empty;
    [Required, MaxLength(140)] public string Slug { get; set; } = string.Empty;
    [MaxLength(500)] public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<Product> Products { get; set; } = new List<Product>();
}

public class Product
{
    public int Id { get; set; }
    [Required, MaxLength(50)] public string SKU { get; set; } = string.Empty;
    [Required, MaxLength(200)] public string Name { get; set; } = string.Empty;
    [Required, MaxLength(220)] public string Slug { get; set; } = string.Empty;
    [MaxLength(500)] public string ShortDescription { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Ingredients { get; set; } = string.Empty;
    public string Usage { get; set; } = string.Empty;
    public string Warnings { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? SalePrice { get; set; }
    public decimal CostPrice { get; set; }
    public int StockQuantity { get; set; }
    public int MinimumStockLevel { get; set; } = 5;
    [MaxLength(50)] public string Unit { get; set; } = "Sản phẩm";
    public decimal Weight { get; set; }
    public string? ThumbnailUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsFeatured { get; set; }
    public bool IsNew { get; set; }
    public decimal RatingAverage { get; set; }
    public int ReviewCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }
    public int CategoryId { get; set; }
    public Category? Category { get; set; }
    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    public ICollection<ProductReview> Reviews { get; set; } = new List<ProductReview>();
}

public class ProductImage
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    [Required, MaxLength(500)] public string Url { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
}

public class ProductReview
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public string? UserId { get; set; }
    [Required, MaxLength(120)] public string CustomerName { get; set; } = string.Empty;
    [Range(1, 5)] public int Rating { get; set; }
    [MaxLength(200)] public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsApproved { get; set; } = true;
}

public class CartItem
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal => (Product?.SalePrice ?? Product?.Price ?? 0m) * Quantity;
}

public class Order
{
    public int Id { get; set; }
    [Required, MaxLength(30)] public string OrderCode { get; set; } = string.Empty;
    public string? UserId { get; set; }
    [Required, MaxLength(120)] public string CustomerName { get; set; } = string.Empty;
    [Required, MaxLength(200)] public string ShippingAddress { get; set; } = string.Empty;
    [Required, MaxLength(50)] public string Phone { get; set; } = string.Empty;
    [MaxLength(30)] public string PaymentMethod { get; set; } = "COD";
    [MaxLength(30)] public string Status { get; set; } = "Pending";
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaymentDeadline { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime? InventoryReleasedAt { get; set; }
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order? Order { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    [Required, MaxLength(200)] public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal { get; set; }
}

public class Voucher
{
    public int Id { get; set; }
    [Required, MaxLength(40)] public string Code { get; set; } = string.Empty;
    public decimal DiscountValue { get; set; }
    [MaxLength(20)] public string DiscountType { get; set; } = "Percentage";
    public decimal MinimumOrderValue { get; set; }
    public decimal MaximumDiscount { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int UsageLimit { get; set; }
    public int UsedCount { get; set; }
    public bool IsActive { get; set; } = true;
}

public class InventoryTransaction
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    [Required, MaxLength(20)] public string Type { get; set; } = "IN";
    public int Quantity { get; set; }
    public int BeforeStock { get; set; }
    public int AfterStock { get; set; }
    [MaxLength(300)] public string Reason { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class SupportMessage
{
    public int Id { get; set; }
    public string? UserId { get; set; }
    [Required, MaxLength(30)] public string SenderType { get; set; } = "Customer";
    [Required] public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
