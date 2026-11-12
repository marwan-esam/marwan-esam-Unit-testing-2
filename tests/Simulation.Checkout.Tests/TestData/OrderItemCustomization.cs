using AutoFixture;
using Simulation.Checkout.Domain;

namespace Simulation.Checkout.Tests.TestData;

public class OrderItemCustomization : ICustomization
{
  public void Customize(IFixture fixture)
  {
    fixture.Customize<OrderItem>(composer => composer
      .With(orderItem => orderItem.Quantity, () => Math.Max(1, fixture.Create<int>())) // I know default already starts from 1, I just wanted to make it explicit
      .With(orderItem => orderItem.UnitPrice, () => Math.Max(0, fixture.Create<decimal>() - 1m)) // Random.Shared.Next() should also work here
    );
  }
}