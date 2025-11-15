using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ABCRetailsFunctions.Models
{
    public record CustomerDto
    {
        public CustomerDto() { } // Parameterless constructor for object initialization

        public CustomerDto(string id, string name, string surname, string username, string email, string shippingAddress)
        {
            Id = id;
            Name = name;
            Surname = surname;
            Username = username;
            Email = email;
            ShippingAddress = shippingAddress;
        }

        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
    }

    public record ProductDto
    {
        public ProductDto() { } // Parameterless constructor for object initialization

        public ProductDto(string id, string productName, string description, decimal price, int stockAvailable, string imageUrl)
        {
            Id = id;
            ProductName = productName;
            Description = description;
            Price = price;
            StockAvailable = stockAvailable;
            ImageUrl = imageUrl;
        }

        public string Id { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockAvailable { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
    }

    public record OrderDto
    {
        public OrderDto() { } // Parameterless constructor for object initialization

        public OrderDto(
            string id, string customerId, string productId, string productName,
            int quantity, decimal unitPrice, decimal totalAmount, DateTimeOffset orderDateUtc, string status)
        {
            Id = id;
            CustomerId = customerId;
            ProductId = productId;
            ProductName = productName;
            Quantity = quantity;
            UnitPrice = unitPrice;
            TotalAmount = totalAmount;
            OrderDateUtc = orderDateUtc;
            Status = status;
        }

        public string Id { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTimeOffset OrderDateUtc { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}