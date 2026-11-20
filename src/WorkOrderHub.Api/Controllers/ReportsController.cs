using Microsoft.AspNetCore.Mvc;
using WorkOrderHub.Api.Contracts;
using WorkOrderHub.Api.Services;

namespace WorkOrderHub.Api.Controllers;

[ApiController]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private readonly ReportService _reports;

    public ReportsController(ReportService reports) => _reports = reports;

    [HttpGet("summary")]
    public Task<SummaryReport> Summary(CancellationToken ct) => _reports.SummaryAsync(ct);
}
