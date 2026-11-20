namespace WorkOrderHub.Api.Domain;

public class Technician
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>Most work orders this technician may hold at once.</summary>
    public int MaxActiveOrders { get; set; } = 3;

    public List<WorkOrder> WorkOrders { get; set; } = new();
}
