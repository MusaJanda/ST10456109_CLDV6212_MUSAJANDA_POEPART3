using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace ABCRetails.Models.ViewModels
{
    public class OrderCreateViewModel
    {
        [Required(ErrorMessage = "Please select a customer")]
        [Display(Name = "Customer")]
        public string CustomerId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Shipping address is required")]
        [Display(Name = "Shipping Address")]
        [StringLength(500, ErrorMessage = "Shipping address cannot exceed 500 characters")]
        public string ShippingAddress { get; set; } = string.Empty;

        [Display(Name = "Customer Notes")]
        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        public string? CustomerNotes { get; set; }

        [Display(Name = "Order Date")]
        [DataType(DataType.Date)]
        public DateTime OrderDate { get; set; } = DateTime.Today;

        public string Status { get; set; } = "Pending";

        // Cart items
        public List<CartItemViewModel> CartItems { get; set; } = new List<CartItemViewModel>();

        // Total amount calculated from cart
        [Display(Name = "Total Amount")]
        [DataType(DataType.Currency)]
        public decimal TotalAmount { get; set; }
    }

    
}