namespace WorkOrderHub.Api.Domain;

/// <summary>
/// A maintenance request for a piece of equipment. Status changes go through
/// <see cref="TransitionTo"/> so the lifecycle rules live in one place.
/// </summary>
public class WorkOrder
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string AssetTag { get; set; } = string.Empty;
    public Priority Priority { get; set; }
    public WorkOrderStatus Status { get; private set; } = WorkOrderStatus.Open;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime DueAtUtc { get; set; }
    public DateTime? ResolvedAtUtc { get; private set; }

    public int? TechnicianId { get; private set; }
    public Technician? Technician { get; private set; }

    public bool IsActive => WorkOrderLifecycle.IsActive(Status);

    public bool IsOverdue(DateTime nowUtc) => IsActive && DueAtUtc < nowUtc;

    public void TransitionTo(WorkOrderStatus next, DateTime nowUtc)
    {
        if (!WorkOrderLifecycle.CanTransition(Status, next))
        {
            throw new InvalidTransitionException(Status, next);
        }

        if (next == WorkOrderStatus.Open)
        {
            TechnicianId = null;
            Technician = null;
        }

        if (next == WorkOrderStatus.Resolved)
        {
            ResolvedAtUtc = nowUtc;
        }

        Status = next;
    }

    public void AssignTo(Technician technician, DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(technician);
        TransitionTo(WorkOrderStatus.Assigned, nowUtc);
        TechnicianId = technician.Id;
        Technician = technician;
    }
}
