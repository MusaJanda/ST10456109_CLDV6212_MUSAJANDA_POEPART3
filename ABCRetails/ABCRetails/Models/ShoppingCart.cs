// Models/ShoppingCart.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ABCRetails.Models
{
    public class ShoppingCart
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string CustomerId { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Customer Customer { get; set; } = null!;
        public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

        // Calculated properties
        [NotMapped]
        public int TotalItems => CartItems?.Sum(item => item.Quantity) ?? 0;

        [NotMapped]
        public decimal TotalAmount => CartItems?.Sum(item => item.TotalPrice) ?? 0;
    }

    // Cart Item Model
    public class CartItem
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string CartId { get; set; } = string.Empty;

        [Required]
        public string ProductId { get; set; } = string.Empty;

        [Required]
        public int Quantity { get; set; } = 1;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        public DateTime AddedDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ShoppingCart ShoppingCart { get; set; } = null!;
        public virtual Product Product { get; set; } = null!;

        // Calculated property - FIXED: Make it a regular property
        [NotMapped]
        public decimal TotalPrice => UnitPrice * Quantity;

        [NotMapped]
        public string ProductName => Product?.ProductName ?? string.Empty;
    }

}