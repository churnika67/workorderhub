using WorkOrderHub.Api.Domain;

namespace WorkOrderHub.Tests;

public class LifecycleTests
{
    private static readonly DateTime Now = new(2026, 1, 15, 9, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(WorkOrderStatus.Open, WorkOrderStatus.Assigned, true)]
    [InlineData(WorkOrderStatus.Open, WorkOrderStatus.Cancelled, true)]
    [InlineData(WorkOrderStatus.Open, WorkOrderStatus.Resolved, false)]
    [InlineData(WorkOrderStatus.Assigned, WorkOrderStatus.InProgress, true)]
    [InlineData(WorkOrderStatus.InProgress, WorkOrderStatus.Resolved, true)]
    [InlineData(WorkOrderStatus.InProgress, WorkOrderStatus.Cancelled, false)]
    [InlineData(WorkOrderStatus.Resolved, WorkOrderStatus.Closed, true)]
    [InlineData(WorkOrderStatus.Resolved, WorkOrderStatus.InProgress, true)]
    [InlineData(WorkOrderStatus.Closed, WorkOrderStatus.Open, false)]
    [InlineData(WorkOrderStatus.Cancelled, WorkOrderStatus.Open, false)]
    public void CanTransition_follows_the_lifecycle_table(WorkOrderStatus from, WorkOrderStatus to, bool expected)
    {
        Assert.Equal(expected, WorkOrderLifecycle.CanTransition(from, to));
    }

    [Fact]
    public void Invalid_transition_throws_and_keeps_status()
    {
        var order = new WorkOrder { Title = "Pump leak", AssetTag = "PMP-1" };

        var ex = Assert.Throws<InvalidTransitionException>(() => order.TransitionTo(WorkOrderStatus.Closed, Now));

        Assert.Equal(WorkOrderStatus.Open, ex.From);
        Assert.Equal(WorkOrderStatus.Open, order.Status);
    }

    [Fact]
    public void Resolving_records_the_resolution_time()
    {
        var order = new WorkOrder { Title = "Belt worn", AssetTag = "CNV-2" };
        order.AssignTo(new Technician { Id = 1, Name = "Ana" }, Now);
        order.TransitionTo(WorkOrderStatus.InProgress, Now);

        order.TransitionTo(WorkOrderStatus.Resolved, Now.AddHours(3));

        Assert.Equal(Now.AddHours(3), order.ResolvedAtUtc);
        Assert.False(order.IsActive);
    }

    [Fact]
    public void Returning_to_open_clears_the_technician()
    {
        var order = new WorkOrder { Title = "Sensor fault", AssetTag = "SNS-9" };
        order.AssignTo(new Technician { Id = 7, Name = "Raj" }, Now);

        order.TransitionTo(WorkOrderStatus.Open, Now);

        Assert.Null(order.TechnicianId);
    }

    [Fact]
    public void Overdue_only_applies_to_active_orders()
    {
        var order = new WorkOrder { Title = "Valve stuck", AssetTag = "VLV-3", DueAtUtc = Now.AddHours(-1) };
        Assert.True(order.IsOverdue(Now));

        order.TransitionTo(WorkOrderStatus.Cancelled, Now);
        Assert.False(order.IsOverdue(Now));
    }
}
