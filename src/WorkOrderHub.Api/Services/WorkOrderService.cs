using Microsoft.EntityFrameworkCore;
using WorkOrderHub.Api.Contracts;
using WorkOrderHub.Api.Data;
using WorkOrderHub.Api.Domain;

namespace WorkOrderHub.Api.Services;

public class WorkOrderService
{
    private readonly AppDbContext _db;
    private readonly IClock _clock;

    public WorkOrderService(AppDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<WorkOrder> CreateAsync(CreateWorkOrderRequest request, CancellationToken ct)
    {
        var now = _clock.UtcNow;
        var order = new WorkOrder
        {
            Title = request.Title.Trim(),
            AssetTag = request.AssetTag.Trim().ToUpperInvariant(),
            Description = request.Description?.Trim(),
            Priority = request.Priority,
            CreatedAtUtc = now,
            DueAtUtc = now.AddHours(request.SlaHours),
        };

        _db.WorkOrders.Add(order);
        await _db.SaveChangesAsync(ct);
        return order;
    }

    public Task<WorkOrder?> FindAsync(int id, CancellationToken ct) =>
        _db.WorkOrders.FirstOrDefaultAsync(w => w.Id == id, ct);

    public async Task<PagedResponse<WorkOrder>> ListAsync(
        WorkOrderStatus? status, Priority? priority, int page, int pageSize, CancellationToken ct)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.WorkOrders.AsNoTracking();
        if (status is not null) query = query.Where(w => w.Status == status);
        if (priority is not null) query = query.Where(w => w.Priority == priority);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(w => w.Priority)
            .ThenBy(w => w.DueAtUtc)
            .ThenBy(w => w.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResponse<WorkOrder>(items, page, pageSize, total);
    }

    public async Task<WorkOrder?> ChangeStatusAsync(int id, WorkOrderStatus next, CancellationToken ct)
    {
        var order = await FindAsync(id, ct);
        if (order is null) return null;

        if (next == WorkOrderStatus.Assigned)
        {
            throw new BusinessRuleException("Use the dispatch endpoint to assign work orders.");
        }

        order.TransitionTo(next, _clock.UtcNow);
        await _db.SaveChangesAsync(ct);
        return order;
    }
}
