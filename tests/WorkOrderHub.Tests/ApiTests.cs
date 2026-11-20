using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using WorkOrderHub.Api.Contracts;
using WorkOrderHub.Api.Domain;

namespace WorkOrderHub.Tests;

public class ApiTests : IDisposable
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly ApiFactory _factory = new();
    private readonly HttpClient _client;

    public ApiTests() => _client = _factory.CreateClient();

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private async Task<WorkOrderResponse> CreateOrder(string title, Priority priority, int slaHours)
    {
        var response = await _client.PostAsJsonAsync("/api/work-orders",
            new CreateWorkOrderRequest(title, "asset-1", null, priority, slaHours), Json);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<WorkOrderResponse>(Json))!;
    }

    private async Task<TechnicianResponse> CreateTech(string name, int max)
    {
        var response = await _client.PostAsJsonAsync("/api/technicians", new CreateTechnicianRequest(name, max), Json);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<TechnicianResponse>(Json))!;
    }

    private Task<HttpResponseMessage> ChangeStatus(int id, WorkOrderStatus status) =>
        _client.PatchAsJsonAsync($"/api/work-orders/{id}/status", new ChangeStatusRequest(status), Json);

    [Fact]
    public async Task Create_sets_due_date_from_sla_and_normalizes_asset_tag()
    {
        var created = await CreateOrder("Compressor overheating", Priority.High, 8);

        Assert.Equal(WorkOrderStatus.Open, created.Status);
        Assert.Equal("ASSET-1", created.AssetTag);
        Assert.Equal(_factory.Clock.UtcNow.AddHours(8), created.DueAtUtc);
    }

    [Fact]
    public async Task Create_rejects_invalid_input_with_400()
    {
        var response = await _client.PostAsJsonAsync("/api/work-orders",
            new CreateWorkOrderRequest("x", "", null, Priority.Low, 0), Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_unknown_id_returns_404()
    {
        var response = await _client.GetAsync("/api/work-orders/999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task List_filters_by_priority_and_pages_results()
    {
        await CreateOrder("Aaa", Priority.Low, 10);
        await CreateOrder("Bbb", Priority.High, 5);
        await CreateOrder("Ccc", Priority.High, 2);

        var page = await _client.GetFromJsonAsync<PagedResponse<WorkOrderResponse>>(
            "/api/work-orders?priority=High&page=1&pageSize=1", Json);

        Assert.Equal(2, page!.TotalCount);
        Assert.Single(page.Items);
        Assert.Equal("Ccc", page.Items[0].Title);
    }

    [Fact]
    public async Task Dispatch_assigns_critical_work_first_and_respects_capacity()
    {
        var low = await CreateOrder("Squeaky door", Priority.Low, 2);
        var critical = await CreateOrder("Boiler failure", Priority.Critical, 24);
        var tech = await CreateTech("Ana Lopez", 1);

        var result = await (await _client.PostAsync("/api/dispatch/run", null))
            .Content.ReadFromJsonAsync<DispatchResult>(Json);

        var assignment = Assert.Single(result!.Assignments);
        Assert.Equal(critical.Id, assignment.WorkOrderId);
        Assert.Equal(tech.Id, assignment.TechnicianId);
        Assert.Equal(1, result.LeftUnassigned);

        var stillOpen = await _client.GetFromJsonAsync<WorkOrderResponse>($"/api/work-orders/{low.Id}", Json);
        Assert.Equal(WorkOrderStatus.Open, stillOpen!.Status);
    }

    [Fact]
    public async Task Invalid_status_change_returns_409_problem_details()
    {
        var order = await CreateOrder("Leaking pipe", Priority.Medium, 4);

        var response = await ChangeStatus(order.Id, WorkOrderStatus.Closed);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Manual_assignment_through_status_endpoint_is_rejected()
    {
        var order = await CreateOrder("Broken light", Priority.Low, 4);

        var response = await ChangeStatus(order.Id, WorkOrderStatus.Assigned);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Full_lifecycle_feeds_the_summary_report()
    {
        var order = await CreateOrder("Conveyor jam", Priority.High, 2);
        await CreateOrder("Filter change", Priority.Low, 1);
        await CreateTech("Raj Patel", 2);
        await _client.PostAsync("/api/dispatch/run", null);

        Assert.True((await ChangeStatus(order.Id, WorkOrderStatus.InProgress)).IsSuccessStatusCode);
        _factory.Clock.Advance(TimeSpan.FromHours(3));
        Assert.True((await ChangeStatus(order.Id, WorkOrderStatus.Resolved)).IsSuccessStatusCode);

        var report = await _client.GetFromJsonAsync<SummaryReport>("/api/reports/summary", Json);

        Assert.Contains(report!.ByStatus, s => s.Status == "Resolved" && s.Count == 1);
        var lowBacklog = Assert.Single(report.BacklogByPriority);
        Assert.Equal("Low", lowBacklog.Priority);
        Assert.Equal(1, lowBacklog.Open);
        Assert.Equal(1, lowBacklog.Overdue);
        Assert.Equal(3.0, report.AverageHoursToResolve);
    }

    [Fact]
    public async Task Technician_list_reports_active_load()
    {
        await CreateOrder("Motor noise", Priority.Medium, 6);
        var tech = await CreateTech("Mei Chen", 3);
        await _client.PostAsync("/api/dispatch/run", null);

        var techs = await _client.GetFromJsonAsync<List<TechnicianResponse>>("/api/technicians", Json);

        var row = Assert.Single(techs!);
        Assert.Equal(tech.Id, row.Id);
        Assert.Equal(1, row.ActiveOrders);
    }

    [Fact]
    public async Task Health_endpoint_responds_ok()
    {
        var response = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
