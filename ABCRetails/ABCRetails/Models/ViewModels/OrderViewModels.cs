using System.ComponentModel.DataAnnotations;

namespace ABCRetails.Models.ViewModels
{
    public class OrderViewModel
    {
        public string Id { get; set; } = string.Empty;

        public string CustomerId { get; set; } = string.Empty; // ADD THIS LINE

        [Display(Name = "Order Date")]
        public DateTime OrderDate { get; set; }

        [Display(Name = "Customer")]
        public string CustomerName { get; set; } = string.Empty;

        public string Username { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        [Display(Name = "Total Amount")]
        [DataType(DataType.Currency)]
        public decimal TotalAmount { get; set; }

        public string Status { get; set; } = string.Empty;

        [Display(Name = "Shipping Address")]
        public string ShippingAddress { get; set; } = string.Empty;

        [Display(Name = "Items Count")]
        public int ItemsCount { get; set; }

        [Display(Name = "Total Quantity")]
        public int TotalQuantity { get; set; }

        [Display(Name = "Products")]
        public string ProductNames { get; set; } = string.Empty;
    }

    public class OrderDetailViewModel
    {
        public string Id { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
        public string? CustomerNotes { get; set; }
        public DateTime? ProcessedDate { get; set; }
        public string? ProcessedBy { get; set; }

        public List<OrderItemViewModel> OrderItems { get; set; } = new List<OrderItemViewModel>();
    }

    public class OrderItemViewModel
    {
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }
}