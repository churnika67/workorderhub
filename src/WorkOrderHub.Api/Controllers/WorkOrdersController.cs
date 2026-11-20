using Microsoft.AspNetCore.Mvc;
using WorkOrderHub.Api.Contracts;
using WorkOrderHub.Api.Domain;
using WorkOrderHub.Api.Services;

namespace WorkOrderHub.Api.Controllers;

[ApiController]
[Route("api/work-orders")]
public class WorkOrdersController : ControllerBase
{
    private readonly WorkOrderService _service;
    private readonly IClock _clock;

    public WorkOrdersController(WorkOrderService service, IClock clock)
    {
        _service = service;
        _clock = clock;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<WorkOrderResponse>>> List(
        [FromQuery] WorkOrderStatus? status, [FromQuery] Priority? priority,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _service.ListAsync(status, priority, page, pageSize, ct);
        var now = _clock.UtcNow;
        return Ok(new PagedResponse<WorkOrderResponse>(
            result.Items.Select(w => WorkOrderResponse.From(w, now)).ToList(),
            result.Page, result.PageSize, result.TotalCount));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<WorkOrderResponse>> Get(int id, CancellationToken ct)
    {
        var order = await _service.FindAsync(id, ct);
        return order is null ? NotFound() : Ok(WorkOrderResponse.From(order, _clock.UtcNow));
    }

    [HttpPost]
    public async Task<ActionResult<WorkOrderResponse>> Create(CreateWorkOrderRequest request, CancellationToken ct)
    {
        var order = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = order.Id }, WorkOrderResponse.From(order, _clock.UtcNow));
    }

    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<WorkOrderResponse>> ChangeStatus(int id, ChangeStatusRequest request, CancellationToken ct)
    {
        var order = await _service.ChangeStatusAsync(id, request.Status, ct);
        return order is null ? NotFound() : Ok(WorkOrderResponse.From(order, _clock.UtcNow));
    }
}
