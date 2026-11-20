using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkOrderHub.Api.Contracts;
using WorkOrderHub.Api.Data;
using WorkOrderHub.Api.Domain;

namespace WorkOrderHub.Api.Controllers;

[ApiController]
[Route("api/technicians")]
public class TechniciansController : ControllerBase
{
    private readonly AppDbContext _db;

    public TechniciansController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IReadOnlyList<TechnicianResponse>> List(CancellationToken ct) =>
        await _db.Technicians
            .OrderBy(t => t.Id)
            .Select(t => new TechnicianResponse(
                t.Id, t.Name, t.MaxActiveOrders,
                t.WorkOrders.Count(w => w.Status == WorkOrderStatus.Assigned || w.Status == WorkOrderStatus.InProgress)))
            .ToListAsync(ct);

    [HttpPost]
    public async Task<ActionResult<TechnicianResponse>> Create(CreateTechnicianRequest request, CancellationToken ct)
    {
        var tech = new Technician { Name = request.Name.Trim(), MaxActiveOrders = request.MaxActiveOrders };
        _db.Technicians.Add(tech);
        await _db.SaveChangesAsync(ct);
        return Created($"/api/technicians/{tech.Id}", new TechnicianResponse(tech.Id, tech.Name, tech.MaxActiveOrders, 0));
    }
}
