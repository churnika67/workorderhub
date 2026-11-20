using System.ComponentModel.DataAnnotations;
using WorkOrderHub.Api.Domain;

namespace WorkOrderHub.Api.Contracts;

public record CreateWorkOrderRequest(
    [Required, StringLength(120, MinimumLength = 3)] string Title,
    [Required, StringLength(40, MinimumLength = 2)] string AssetTag,
    [StringLength(2000)] string? Description,
    [EnumDataType(typeof(Priority))] Priority Priority,
    [Range(1, 720)] int SlaHours);

public record ChangeStatusRequest([EnumDataType(typeof(WorkOrderStatus))] WorkOrderStatus Status);

public record CreateTechnicianRequest(
    [Required, StringLength(80, MinimumLength = 2)] string Name,
    [Range(1, 10)] int MaxActiveOrders);

public record WorkOrderResponse(
    int Id,
    string Title,
    string AssetTag,
    string? Description,
    Priority Priority,
    WorkOrderStatus Status,
    DateTime CreatedAtUtc,
    DateTime DueAtUtc,
    DateTime? ResolvedAtUtc,
    int? TechnicianId,
    bool IsOverdue)
{
    public static WorkOrderResponse From(WorkOrder w, DateTime nowUtc) => new(
        w.Id, w.Title, w.AssetTag, w.Description, w.Priority, w.Status,
        w.CreatedAtUtc, w.DueAtUtc, w.ResolvedAtUtc, w.TechnicianId, w.IsOverdue(nowUtc));
}

public record TechnicianResponse(int Id, string Name, int MaxActiveOrders, int ActiveOrders);

public record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

public record DispatchAssignment(int WorkOrderId, int TechnicianId, Priority Priority, DateTime DueAtUtc);

public record DispatchResult(IReadOnlyList<DispatchAssignment> Assignments, int LeftUnassigned);

public record StatusCount(string Status, int Count);

public record PriorityBacklog(string Priority, int Open, int Overdue);

public record SummaryReport(
    IReadOnlyList<StatusCount> ByStatus,
    IReadOnlyList<PriorityBacklog> BacklogByPriority,
    double? AverageHoursToResolve);
