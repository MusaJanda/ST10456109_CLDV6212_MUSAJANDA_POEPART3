using ABCRetailsFunctions.Entities;
using ABCRetailsFunctions.Models;

namespace ABCRetailsFunctions.Helpers;

public static class Map
{
    /// <summary>
    /// Maps a CustomerEntity from Azure Table Storage to a CustomerDto.
    /// </summary>
    public static CustomerDto ToDto(CustomerEntity e)
        => new CustomerDto
        {
            Id = e.RowKey,
            Name = e.Name ?? string.Empty,
            Surname = e.Surname ?? string.Empty,
            Username = e.Username ?? string.Empty,
            Email = e.Email ?? string.Empty,
            ShippingAddress = e.ShippingAddress ?? string.Empty
        };

    /// <summary>
    /// Maps a ProductEntity from Azure Table Storage to a ProductDto.
    /// Handles conversion of the Price from double (Table Storage) to decimal (DTO).
    /// </summary>
    public static ProductDto ToDto(ProductEntity e)
        => new ProductDto
        {
            Id = e.RowKey,
            ProductName = e.ProductName ?? string.Empty,
            Description = e.Description ?? string.Empty,
            Price = Convert.ToDecimal(e.Price),
            StockAvailable = e.StockAvailable,
            ImageUrl = e.ImageUrl ?? string.Empty
        };

    /// <summary>
    /// Maps an OrderEntity from Azure Table Storage to an OrderDto.
    /// Handles conversion of UnitPrice and TotalPrice from double to decimal.
    /// </summary>
    public static OrderDto ToDto(OrderEntity e)
    {
        var unitPrice = Convert.ToDecimal(e.UnitPrice);
        var totalPrice = Convert.ToDecimal(e.TotalPrice);

        return new OrderDto
        {
            Id = e.RowKey,
            CustomerId = e.CustomerId ?? string.Empty,
            ProductId = e.ProductId ?? string.Empty,
            ProductName = e.ProductName ?? string.Empty,
            Quantity = e.Quantity,
            UnitPrice = unitPrice,
            TotalAmount = totalPrice,
            OrderDateUtc = e.OrderDate,
            Status = e.Status ?? "Submitted"
        };
    }

    /// <summary>
    /// Maps a CustomerDto to a CustomerEntity for Azure Table Storage.
    /// </summary>
    public static CustomerEntity ToEntity(CustomerDto dto, string? rowKey = null)
    {
        var entity = new CustomerEntity(rowKey ?? dto.Id ?? Guid.NewGuid().ToString("N"))
        {
            Name = dto.Name,
            Surname = dto.Surname,
            Username = dto.Username,
            Email = dto.Email,
            ShippingAddress = dto.ShippingAddress
        };

        // Set both RowKey and Id to the same value
        entity.Id = entity.RowKey;
        return entity;
    }

    /// <summary>
    /// Maps a ProductDto to a ProductEntity for Azure Table Storage.
    /// Handles conversion of the Price from decimal (DTO) to double (Table Storage).
    /// </summary>
    public static ProductEntity ToEntity(ProductDto dto, string? rowKey = null)
    {
        var entity = new ProductEntity(rowKey ?? dto.Id ?? Guid.NewGuid().ToString("N"))
        {
            ProductName = dto.ProductName,
            Description = dto.Description,
            Price = Convert.ToDouble(dto.Price),
            StockAvailable = dto.StockAvailable,
            ImageUrl = dto.ImageUrl
        };

        // Set both RowKey and Id to the same value
        entity.Id = entity.RowKey;
        return entity;
    }

    /// <summary>
    /// Maps an OrderDto to an OrderEntity for Azure Table Storage.
    /// Handles conversion of UnitPrice and TotalAmount from decimal to double.
    /// </summary>
    public static OrderEntity ToEntity(OrderDto dto, string? rowKey = null)
    {
        var unitPrice = Convert.ToDouble(dto.UnitPrice);
        var totalPrice = Convert.ToDouble(dto.TotalAmount);

        var entity = new OrderEntity(rowKey ?? dto.Id ?? Guid.NewGuid().ToString("N"))
        {
            CustomerId = dto.CustomerId,
            ProductId = dto.ProductId,
            ProductName = dto.ProductName,
            Quantity = dto.Quantity,
            UnitPrice = unitPrice,
            TotalPrice = totalPrice,
            OrderDate = dto.OrderDateUtc.UtcDateTime,
            Status = dto.Status
        };

        // Set both RowKey and Id to the same value
        entity.Id = entity.RowKey;
        return entity;
    }

    /// <summary>
    /// Creates an OrderEntity from customer, product, and quantity.
    /// </summary>
    public static OrderEntity ToOrderEntity(string customerId, string productId, string productName, int quantity, double unitPrice, string? rowKey = null)
    {
        var totalPrice = unitPrice * quantity;
        var entityId = rowKey ?? Guid.NewGuid().ToString("N");

        return new OrderEntity(entityId)
        {
            CustomerId = customerId,
            ProductId = productId,
            ProductName = productName,
            Quantity = quantity,
            UnitPrice = unitPrice,
            TotalPrice = totalPrice,
            OrderDate = DateTime.UtcNow,
            Status = "Submitted",
            Id = entityId
        };
    }

    /// <summary>
    /// Updates an existing CustomerEntity with values from CustomerDto.
    /// </summary>
    public static void UpdateEntity(CustomerEntity entity, CustomerDto dto)
    {
        if (!string.IsNullOrEmpty(dto.Name)) entity.Name = dto.Name;
        if (!string.IsNullOrEmpty(dto.Surname)) entity.Surname = dto.Surname;
        if (!string.IsNullOrEmpty(dto.Username)) entity.Username = dto.Username;
        if (!string.IsNullOrEmpty(dto.Email)) entity.Email = dto.Email;
        if (!string.IsNullOrEmpty(dto.ShippingAddress)) entity.ShippingAddress = dto.ShippingAddress;

        // Update Id if provided
        if (!string.IsNullOrEmpty(dto.Id)) entity.Id = dto.Id;
    }

    /// <summary>
    /// Updates an existing ProductEntity with values from ProductDto.
    /// </summary>
    public static void UpdateEntity(ProductEntity entity, ProductDto dto)
    {
        if (!string.IsNullOrEmpty(dto.ProductName)) entity.ProductName = dto.ProductName;
        if (!string.IsNullOrEmpty(dto.Description)) entity.Description = dto.Description;
        if (dto.Price > 0) entity.Price = Convert.ToDouble(dto.Price);
        entity.StockAvailable = dto.StockAvailable;
        if (!string.IsNullOrEmpty(dto.ImageUrl)) entity.ImageUrl = dto.ImageUrl;

        // Update Id if provided
        if (!string.IsNullOrEmpty(dto.Id)) entity.Id = dto.Id;
    }

    /// <summary>
    /// Updates an existing OrderEntity with values from OrderDto.
    /// </summary>
    public static void UpdateEntity(OrderEntity entity, OrderDto dto)
    {
        if (!string.IsNullOrEmpty(dto.CustomerId)) entity.CustomerId = dto.CustomerId;
        if (!string.IsNullOrEmpty(dto.ProductId)) entity.ProductId = dto.ProductId;
        if (!string.IsNullOrEmpty(dto.ProductName)) entity.ProductName = dto.ProductName;
        if (dto.Quantity > 0) entity.Quantity = dto.Quantity;
        if (dto.UnitPrice > 0) entity.UnitPrice = Convert.ToDouble(dto.UnitPrice);
        if (dto.TotalAmount > 0) entity.TotalPrice = Convert.ToDouble(dto.TotalAmount);
        if (!string.IsNullOrEmpty(dto.Status)) entity.Status = dto.Status;

        // Update Id if provided
        if (!string.IsNullOrEmpty(dto.Id)) entity.Id = dto.Id;
    }
}