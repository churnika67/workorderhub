using WorkOrderHub.Api.Domain;

namespace WorkOrderHub.Api.Services;

/// <summary>
/// Pure assignment logic, separate from the database so it can be unit tested.
/// Work orders are taken highest priority first, then earliest due date.
/// Each one goes to the technician with the fewest active orders who still has capacity.
/// </summary>
public static class Dispatcher
{
    public sealed record Load(Technician Technician, int Active);

    public static IReadOnlyList<(WorkOrder Order, Technician Technician)> Plan(
        IEnumerable<WorkOrder> openOrders, IEnumerable<Load> technicianLoads)
    {
        // Max-heap on priority, then min on due date (PriorityQueue is a min-heap, so negate priority).
        var orders = new PriorityQueue<WorkOrder, (int NegPriority, DateTime Due, int Id)>();
        foreach (var order in openOrders)
        {
            orders.Enqueue(order, (-(int)order.Priority, order.DueAtUtc, order.Id));
        }

        // Min-heap on current load; ties broken by technician id so results are repeatable.
        var technicians = new PriorityQueue<Load, (int Active, int Id)>();
        foreach (var load in technicianLoads)
        {
            if (load.Active < load.Technician.MaxActiveOrders)
            {
                technicians.Enqueue(load, (load.Active, load.Technician.Id));
            }
        }

        var plan = new List<(WorkOrder, Technician)>();
        while (orders.Count > 0 && technicians.Count > 0)
        {
            var order = orders.Dequeue();
            var tech = technicians.Dequeue();
            plan.Add((order, tech.Technician));

            var updated = tech with { Active = tech.Active + 1 };
            if (updated.Active < updated.Technician.MaxActiveOrders)
            {
                technicians.Enqueue(updated, (updated.Active, updated.Technician.Id));
            }
        }

        return plan;
    }
}
