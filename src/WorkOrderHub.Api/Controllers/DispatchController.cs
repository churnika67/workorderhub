using Microsoft.AspNetCore.Mvc;
using WorkOrderHub.Api.Contracts;
using WorkOrderHub.Api.Services;

namespace WorkOrderHub.Api.Controllers;

[ApiController]
[Route("api/dispatch")]
public class DispatchController : ControllerBase
{
    private readonly DispatchService _dispatch;

    public DispatchController(DispatchService dispatch) => _dispatch = dispatch;

    [HttpPost("run")]
    public Task<DispatchResult> Run(CancellationToken ct) => _dispatch.RunAsync(ct);
}
