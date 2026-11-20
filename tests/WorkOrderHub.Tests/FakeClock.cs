using WorkOrderHub.Api.Services;

namespace WorkOrderHub.Tests;

public sealed class FakeClock : IClock
{
    public FakeClock(DateTime start) => UtcNow = start;
    public DateTime UtcNow { get; private set; }
    public void Advance(TimeSpan by) => UtcNow = UtcNow.Add(by);
}
