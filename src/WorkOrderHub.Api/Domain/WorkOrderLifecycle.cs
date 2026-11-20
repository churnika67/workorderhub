namespace WorkOrderHub.Api.Domain;

/// <summary>
/// Allowed status transitions, kept as a lookup table instead of scattered if-statements.
/// </summary>
public static class WorkOrderLifecycle
{
    private static readonly IReadOnlyDictionary<WorkOrderStatus, WorkOrderStatus[]> Allowed =
        new Dictionary<WorkOrderStatus, WorkOrderStatus[]>
        {
            [WorkOrderStatus.Open] = new[] { WorkOrderStatus.Assigned, WorkOrderStatus.Cancelled },
            [WorkOrderStatus.Assigned] = new[] { WorkOrderStatus.InProgress, WorkOrderStatus.Open, WorkOrderStatus.Cancelled },
            [WorkOrderStatus.InProgress] = new[] { WorkOrderStatus.Resolved, WorkOrderStatus.Open },
            [WorkOrderStatus.Resolved] = new[] { WorkOrderStatus.Closed, WorkOrderStatus.InProgress },
            [WorkOrderStatus.Closed] = Array.Empty<WorkOrderStatus>(),
            [WorkOrderStatus.Cancelled] = Array.Empty<WorkOrderStatus>(),
        };

    public static bool CanTransition(WorkOrderStatus from, WorkOrderStatus to) =>
        Allowed.TryGetValue(from, out var targets) && targets.Contains(to);

    public static bool IsActive(WorkOrderStatus status) =>
        status is WorkOrderStatus.Open or WorkOrderStatus.Assigned or WorkOrderStatus.InProgress;
}

public class InvalidTransitionException : Exception
{
    public InvalidTransitionException(WorkOrderStatus from, WorkOrderStatus to)
        : base($"Cannot move a work order from {from} to {to}.")
    {
        From = from;
        To = to;
    }

    public WorkOrderStatus From { get; }
    public WorkOrderStatus To { get; }
}

/// <summary>A request that breaks a business rule (returned as 400, not 500).</summary>
public class BusinessRuleException : Exception
{
    public BusinessRuleException(string message) : base(message) { }
}
