using WorkOrderHub.Api.Domain;
using WorkOrderHub.Api.Services;

namespace WorkOrderHub.Tests;

public class DispatcherTests
{
    private static readonly DateTime Now = new(2026, 1, 15, 9, 0, 0, DateTimeKind.Utc);

    private static WorkOrder Order(int id, Priority p, int dueInHours) =>
        new() { Id = id, Title = $"Order {id}", AssetTag = $"A-{id}", Priority = p, DueAtUtc = Now.AddHours(dueInHours) };

    private static Dispatcher.Load Tech(int id, int active, int max = 3) =>
        new(new Technician { Id = id, Name = $"Tech {id}", MaxActiveOrders = max }, active);

    [Fact]
    public void Higher_priority_is_assigned_first()
    {
        var orders = new[] { Order(1, Priority.Low, 2), Order(2, Priority.Critical, 48), Order(3, Priority.High, 4) };

        var plan = Dispatcher.Plan(orders, new[] { Tech(1, 0, max: 2) });

        Assert.Equal(new[] { 2, 3 }, plan.Select(p => p.Order.Id));
    }

    [Fact]
    public void Same_priority_goes_by_earliest_due_date()
    {
        var orders = new[] { Order(1, Priority.High, 10), Order(2, Priority.High, 1), Order(3, Priority.High, 5) };

        var plan = Dispatcher.Plan(orders, new[] { Tech(1, 0) });

        Assert.Equal(new[] { 2, 3, 1 }, plan.Select(p => p.Order.Id));
    }

    [Fact]
    public void Work_is_spread_to_the_least_loaded_technician()
    {
        var orders = Enumerable.Range(1, 4).Select(i => Order(i, Priority.Medium, i)).ToList();

        var plan = Dispatcher.Plan(orders, new[] { Tech(1, 2), Tech(2, 0) });

        // Tech 2 starts empty, so it takes the first two; then both sit at 2 and the tie goes to the lower id.
        Assert.Equal(new[] { 2, 2, 1, 2 }, plan.Select(p => p.Technician.Id));
    }

    [Fact]
    public void Technicians_never_exceed_capacity()
    {
        var orders = Enumerable.Range(1, 10).Select(i => Order(i, Priority.Medium, i)).ToList();

        var plan = Dispatcher.Plan(orders, new[] { Tech(1, 1, max: 2), Tech(2, 0, max: 3) });

        Assert.Equal(4, plan.Count);
        Assert.Equal(1, plan.Count(p => p.Technician.Id == 1));
        Assert.Equal(3, plan.Count(p => p.Technician.Id == 2));
    }

    [Fact]
    public void Full_technicians_receive_nothing()
    {
        var plan = Dispatcher.Plan(new[] { Order(1, Priority.Critical, 1) }, new[] { Tech(1, 3, max: 3) });

        Assert.Empty(plan);
    }

    [Fact]
    public void Empty_inputs_return_an_empty_plan()
    {
        Assert.Empty(Dispatcher.Plan(Array.Empty<WorkOrder>(), new[] { Tech(1, 0) }));
        Assert.Empty(Dispatcher.Plan(new[] { Order(1, Priority.Low, 1) }, Array.Empty<Dispatcher.Load>()));
    }
}
