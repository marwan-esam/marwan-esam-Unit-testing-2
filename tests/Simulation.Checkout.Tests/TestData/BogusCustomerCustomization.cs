using AutoFixture;
using Bogus;
using Simulation.Checkout.Domain;

namespace Simulation.Checkout.Tests.TestData;

public class BogusCustomerCustomization(int seed = 2026) : ICustomization
{
  public void Customize(IFixture fixture)
  {
    fixture.Customize(new OrderItemCustomization()); // this is mainly to fill the order items
    
    var faker = new Faker { Random = new Randomizer(seed)};

    fixture.Customize<Order>(composer => composer
      .With(order => order.CustomerName, faker.Name.FullName())
      .With(order => order.CustomerEmail, faker.Internet.Email())
    );
  }
}