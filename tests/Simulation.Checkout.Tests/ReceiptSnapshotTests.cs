using Moq;
using Simulation.Checkout.Application;
using Simulation.Checkout.Application.Abstractions;
using Simulation.Checkout.Domain;

namespace Simulation.Checkout.Tests;

public class ReceiptSnapshotTests
{

  private readonly Mock<IOrderRepository> _orderRepositoryMock = new();
  private readonly Mock<IInventoryService> _inventoryServiceMock = new();
  private readonly Mock<INotificationService> _notificationServiceMock = new();

  private readonly CheckoutService _checkoutService;

  public ReceiptSnapshotTests()
  {
    _inventoryServiceMock.Setup(
      obj => obj.IsAvailableAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>())
    ).ReturnsAsync(true);

    _checkoutService = new (
      _orderRepositoryMock.Object,
      _inventoryServiceMock.Object,
      _notificationServiceMock.Object
    );
  }

  [Fact]
  public async Task CheckoutAsync_WhenGivenExampleC_ShouldMatchReceiptSnapshot()
  {
    // Arrange
    var order = new Order
    {
      CustomerName = "Marwan Mohamed",
      CustomerEmail = "marwan@gmail.com",
      Items = [
        new OrderItem()
          {
            ProductId = Guid.NewGuid(), // Guid is automatically scrubbed by Verify. same goes for DateTimeOffset
            ProductName = "Laptop",
            Quantity = 3,
            UnitPrice = 500m
          }
      ]
    };

    // Act
    var receipt = await _checkoutService.CheckoutAsync(order);

    // Assert
    await Verify(receipt);
  }
}