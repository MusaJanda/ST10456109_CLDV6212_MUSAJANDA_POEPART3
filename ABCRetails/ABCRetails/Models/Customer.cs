using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ABCRetails.Models
{
    public class Customer
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        // Add computed property for compatibility
        [NotMapped]
        public string CustomerId => Id;

            [Required]
        [Display(Name = "First Name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Last Name")]
        public string Surname { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Phone")]
        public string Phone { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Shipping Address")]
        public string ShippingAddress { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public string Role { get; set; } = "Customer"; // "Customer" or "Admin"
        public string Status { get; set; } = "Active";
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public int OrdersCount { get; set; }
        public DateTime? LastLogin { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ShoppingCart? ShoppingCart { get; set; }
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

        // Helper properties
        [NotMapped]
        public string FullName => $"{Name} {Surname}";
    }
}