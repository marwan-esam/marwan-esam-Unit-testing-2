using FluentAssertions;
using Moq;
using Simulation.Checkout.Application;
using Simulation.Checkout.Application.Abstractions;
using Simulation.Checkout.Domain;
using Simulation.Checkout.Domain.Exceptions;

namespace Simulation.Checkout.Tests;

public class CheckoutServiceTests
{
  private readonly Mock<IOrderRepository> _orderRepositoryMock = new();
  private readonly Mock<IInventoryService> _inventoryServiceMock = new();
  private readonly Mock<INotificationService> _notificationServiceMock = new();
  private readonly CheckoutService _checkoutService;

  public static TheoryData<decimal, decimal> SubtotalDiscounts = new()
  {
    { 999.99m, 0m},
    {1000m, 100m},
    {1500m, 100m},
  };

  public static TheoryData<OrderItem[], decimal, decimal, decimal> Orders = new()
  {
    {
      [
        new OrderItem { ProductId = Guid.NewGuid(), ProductName = "Laptop", Quantity = 2, UnitPrice = 150m },
        new OrderItem { ProductId = Guid.NewGuid(), ProductName = "Keyboard", Quantity = 1, UnitPrice = 400m }
      ],
      700m, 0m, 700m
    },
    {
      [
        new OrderItem { ProductId = Guid.NewGuid(), ProductName = "Camera", Quantity = 3, UnitPrice = 500m }
      ],
      1500m, 100m, 1400m
    }
  };


  public CheckoutServiceTests()
  {
    _inventoryServiceMock
    .Setup(x => x.IsAvailableAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
    .ReturnsAsync(true);

    _checkoutService = new(
      _orderRepositoryMock.Object,
      _inventoryServiceMock.Object,
      _notificationServiceMock.Object
    );
  }

  [Fact]
  public async Task CheckoutAsync_WhenOrderIsEmpty_ShouldThrowEmptyOrderException()
  {
    // Arrange
    var order = CreateOrderWithItems();
    
    // Act 
    var act = () => _checkoutService.CheckoutAsync(order);

    // Assert
    await act.Should().ThrowAsync<EmptyOrderException>();
  }

  [Theory]
  [InlineData(0)]
  [InlineData(-1)]
  public async Task CheckoutAsync_WhenQuantityIsZeroOrNegative_ShouldThrowInvalidQuantityException(int quantity)
  {
    // Arrange
    var order = CreateOrderWithItems(new OrderItem() { ProductId = Guid.NewGuid(), ProductName = "Laptop", Quantity = quantity, UnitPrice = 500m});
    
    // Act
    var act = () => _checkoutService.CheckoutAsync(order);
    
    // Assert
    await act.Should().ThrowAsync<InvalidQuantityException>().WithMessage("*Laptop*");
  }

  [Fact]
  public async Task CheckoutAsync_WhenPriceIsNegative_ShouldThrowInvalidPriceException()
  {
    // Arrange
    var order = CreateOrderWithItems(new OrderItem() { ProductId = Guid.NewGuid(), ProductName = "Laptop", Quantity = 3, UnitPrice = -0.01m});
    
    // Act
    var act = () => _checkoutService.CheckoutAsync(order);

    // Assert
    await act.Should().ThrowAsync<InvalidPriceException>().WithMessage("*Laptop*");
  }

  [Fact]
  public async Task CheckoutAsync_WhenPriceIsZero_ShouldSucceed()
  {
    // Arrange
    var order = CreateOrderWithItems(new OrderItem() { ProductId = Guid.NewGuid(), ProductName = "Laptop", Quantity = 3, UnitPrice = 0});
    
    // Act
    var receipt = await _checkoutService.CheckoutAsync(order);
    
    // Assert
    receipt.Should().NotBeNull();
    receipt.Total.Should().Be(0m);
  }

  [Theory]
  [MemberData(nameof(SubtotalDiscounts))]
  public async Task CheckoutAsync_WhenGivenSubtotal_ShouldReturnExpectedDiscount(decimal subtotal, decimal discount)
  {
    // Arrange
    var order = CreateOrderWithItems(new OrderItem() { ProductId = Guid.NewGuid(), ProductName = "Laptop", Quantity = 1, UnitPrice = subtotal});

    // Act
    var receipt = await _checkoutService.CheckoutAsync(order);
    
    // Assert
    receipt.Discount.Should().Be(discount);
  }

  [Theory]
  [MemberData(nameof(Orders))]
  public async Task CheckoutAsync_WhenOrderIsValid_ShouldReturnSubtotalDiscountAndTotal(OrderItem[] items, decimal subtotal, decimal discount, decimal total)
  {
    // Arrange
    var order = CreateOrderWithItems(items);

    // Act
    var receipt = await _checkoutService.CheckoutAsync(order);
    
    // Assert
    receipt.Subtotal.Should().Be(subtotal);
    receipt.Discount.Should().Be(discount);
    receipt.Total.Should().Be(total);
  }

  [Fact]
  public async Task CheckoutAsync_WhenOrderIsValidAndInStock_ShouldSaveOrderAndSendConfirmation()
  {
    // Arrange
    var order = CreateOrderWithItems(
      new OrderItem() { ProductId = Guid.NewGuid(), ProductName = "keyboard", Quantity = 2, UnitPrice = 500m}
    );

    // Act
    var receipt = await _checkoutService.CheckoutAsync(order);

    
    // Assert
    receipt.Should().NotBeNull();
    _orderRepositoryMock.Verify(orderRepo => orderRepo.SaveAsync(order, It.IsAny<CancellationToken>()), Times.Once);
    _notificationServiceMock.Verify(notificationService => notificationService.SendOrderConfirmationAsync(receipt, It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task CheckoutAsync_WhenOneItemIsOutOfStock_ShouldThrowAndNotSaveOrSendConfirmation()
  {
    // Arrange
    _inventoryServiceMock.Setup(inventoryService => inventoryService.IsAvailableAsync(
      It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()
    )).ReturnsAsync(false);
    var order = CreateOrderWithItems(
      new OrderItem() { ProductId = Guid.NewGuid(), ProductName = "keyboard", Quantity = 2, UnitPrice = 500m}
    );

    // Act
    var act = () => _checkoutService.CheckoutAsync(order);
    
    // Assert
    await act.Should().ThrowAsync<InsufficientInventoryException>();
    _orderRepositoryMock.Verify(orderRepo => orderRepo.SaveAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
    _notificationServiceMock.Verify(notificationServ => notificationServ.SendOrderConfirmationAsync(It.IsAny<CheckoutReceipt>(), It.IsAny<CancellationToken>()), Times.Never);
  }

  [Fact]
  public async Task CheckoutAsync_WhenOrderFailsValidation_ShouldNotCallInventorySaveOrNotification()
  {
    // Arrange
    var order = CreateOrderWithItems(
      new OrderItem() { ProductId = Guid.NewGuid(), ProductName = "Mouse", Quantity = 0, UnitPrice = 500m}
    );

    // Act
    var act = () => _checkoutService.CheckoutAsync(order);

    // Assert
    await act.Should().ThrowAsync<InvalidQuantityException>();
    _inventoryServiceMock.Verify(inventoryServ => inventoryServ.IsAvailableAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    _orderRepositoryMock.Verify(orderRepo => orderRepo.SaveAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
    _notificationServiceMock.Verify(notificationServ => notificationServ.SendOrderConfirmationAsync(It.IsAny<CheckoutReceipt>(), It.IsAny<CancellationToken>()), Times.Never);
  }
  private static Order CreateOrderWithItems(params OrderItem[] items) => new Order()
  {
    CustomerName = "Marwan Mohamed",
    CustomerEmail = "marwan@gmail.com",
    Items = items
  };
}