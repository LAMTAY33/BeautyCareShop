using System.ComponentModel.DataAnnotations;

namespace BeautyCareShop.ViewModels;

public class LoginVm
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, DataType(DataType.Password)] public string Password { get; set; } = string.Empty;
    public bool RememberMe { get; set; }
}

public class RegisterVm
{
    [Required, MaxLength(100)] public string FullName { get; set; } = string.Empty;
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, MinLength(6), DataType(DataType.Password)] public string Password { get; set; } = string.Empty;
    [Required, DataType(DataType.Password), Compare(nameof(Password))] public string ConfirmPassword { get; set; } = string.Empty;
}

public class CheckoutVm
{
    [Required, MaxLength(120)] public string CustomerName { get; set; } = string.Empty;
    [Required, Phone, MaxLength(20)] public string Phone { get; set; } = string.Empty;
    [Required, MaxLength(250)] public string ShippingAddress { get; set; } = string.Empty;
    [Required] public string PaymentMethod { get; set; } = "COD";
    public string PaymentProvider { get; set; } = "VietQR";
    public string? VoucherCode { get; set; }
}

public class CardPaymentVm
{
    public int OrderId { get; set; }
    [Required] public string CardHolder { get; set; } = string.Empty;
    [Required] public string CardNumber { get; set; } = string.Empty;
    [Required, StringLength(5, MinimumLength = 4)] public string Expiry { get; set; } = string.Empty;
    [Required, StringLength(4, MinimumLength = 3)] public string CVV { get; set; } = string.Empty;
}

public class ChatVm
{
    [Required, MaxLength(1000)] public string Message { get; set; } = string.Empty;
}

public class InventoryAdjustmentVm
{
    [Required] public int ProductId { get; set; }
    [Required] public string Type { get; set; } = "IN";
    [Range(1, 100000)] public int Quantity { get; set; } = 1;
    [Required, MaxLength(300)] public string Reason { get; set; } = string.Empty;
}
