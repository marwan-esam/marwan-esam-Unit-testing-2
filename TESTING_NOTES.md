# Testing Notes

## 1. Total tests, passing, failing
- **Total test methods:** 16 (`13` `[Fact]` tests + `3` `[Theory]` tests across `CheckoutServiceTests`, `ReceiptSnapshotTests`, and `ArchitectureTests`)
- **Total test case executions (`dotnet test`):** 20 total — **20 passing**, **0 failing**

## 2. Line coverage % (from `coverage-report/Summary.txt`)
- **Overall Line Coverage:** `90.5%` (`106` of `117` coverable lines)
- **`Simulation.Checkout.Domain`:** `100%`
- **`Simulation.Checkout.Application`:** `95.8%`
- **`Simulation.Checkout.Infrastructure`:** `0%` (mocked via `IOrderRepository` in unit tests)

## 3. Where Moq, AutoFixture, and Bogus were used
- **Moq:** Used in `CheckoutServiceTests` and `ReceiptSnapshotTests` to mock `IOrderRepository`, `IInventoryService`, and `INotificationService`, control inventory availability responses, and verify call counts (`Times.Once` and `Times.Never`).
- **AutoFixture:** Used in `OrderItemCustomization` and `CheckoutServiceTests` (Task 3.1 & 3.2) to automatically generate valid `Order` and `OrderItem` graphs customized with `Quantity > 0` and `UnitPrice >= 0`.
- **Bogus:** Used in `BogusCustomerCustomization` (`CheckoutServiceTests`, Task 3.2) with a fixed seed (`2026`) to generate realistic, deterministic customer names and email addresses for the order.

## 4. Fairness of Examples B and C (999.99 vs. 1,000)
It is not fair to the customer because the flat 10% discount applies to the entire subtotal starting at 1,000 EGP rather than only the amount above the threshold, creating a boundary "cliff" where spending 0.01 EGP more reduces the final total from 999.99 EGP to 900.00 EGP.

## 5. Any bug found
None in the implementation of the stated business rules (`BR-01` through `BR-09`), aside from the business-rule design quirk at the 1,000 EGP discount cliff noted above (and the fact that `Order.CustomerName` / `Order.CustomerEmail` and null elements inside `Order.Items` are not explicitly validated against empty/whitespace strings).
