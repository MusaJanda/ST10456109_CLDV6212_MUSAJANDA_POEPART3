using System.Collections.Generic;
using ABCRetails.Models;

namespace ABCRetails.Models.ViewModels
{
    public class CustomerDashboardViewModel
    {
        public int Id { get; set; }
        public List<Product> FeaturedProducts { get; set; } = new List<Product>();
        public List<Product> Products { get; set; } = new List<Product>();
        public int OrderCount { get; set; }
        public int PendingOrders { get; set; }
        public int CartItemCount { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;

        public decimal TotalSpent { get; set; }
    }
}