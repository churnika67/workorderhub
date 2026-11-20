using Microsoft.EntityFrameworkCore;
using WorkOrderHub.Api.Contracts;
using WorkOrderHub.Api.Data;
using WorkOrderHub.Api.Domain;

namespace WorkOrderHub.Api.Services;

public class DispatchService
{
    private readonly AppDbContext _db;
    private readonly IClock _clock;
    private readonly ILogger<DispatchService> _logger;

    public DispatchService(AppDbContext db, IClock clock, ILogger<DispatchService> logger)
    {
        _db = db;
        _clock = clock;
        _logger = logger;
    }

    public async Task<DispatchResult> RunAsync(CancellationToken ct)
    {
        var open = await _db.WorkOrders
            .Where(w => w.Status == WorkOrderStatus.Open)
            .ToListAsync(ct);

        var loads = await _db.Technicians
            .Select(t => new
            {
                Technician = t,
                Active = t.WorkOrders.Count(w =>
                    w.Status == WorkOrderStatus.Assigned || w.Status == WorkOrderStatus.InProgress),
            })
            .ToListAsync(ct);

        var plan = Dispatcher.Plan(open, loads.Select(l => new Dispatcher.Load(l.Technician, l.Active)));

        var now = _clock.UtcNow;
        foreach (var (order, technician) in plan)
        {
            order.AssignTo(technician, now);
        }

        await _db.SaveChangesAsync(ct);

        var unassigned = open.Count - plan.Count;
        _logger.LogInformation("Dispatch assigned {Assigned} work orders, {Unassigned} left open",
            plan.Count, unassigned);

        return new DispatchResult(
            plan.Select(p => new DispatchAssignment(p.Order.Id, p.Technician.Id, p.Order.Priority, p.Order.DueAtUtc)).ToList(),
            unassigned);
    }
}
