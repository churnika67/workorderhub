using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using WorkOrderHub.Api.Contracts;
using WorkOrderHub.Api.Data;
using WorkOrderHub.Api.Domain;

namespace WorkOrderHub.Api.Services;

/// <summary>
/// Reporting queries written in plain SQL (GROUP BY, CASE, a CTE) against the work_orders table.
/// </summary>
public class ReportService
{
    private readonly AppDbContext _db;
    private readonly IClock _clock;

    public ReportService(AppDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<SummaryReport> SummaryAsync(CancellationToken ct)
    {
        var conn = _db.Database.GetDbConnection();
        var opened = conn.State != ConnectionState.Open;
        if (opened) await conn.OpenAsync(ct);

        try
        {
            var byStatus = new List<StatusCount>();
            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = """
                    SELECT Status, COUNT(*) AS Total
                    FROM work_orders
                    GROUP BY Status
                    ORDER BY Status;
                    """;
                await using var reader = await cmd.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                {
                    byStatus.Add(new StatusCount(((WorkOrderStatus)reader.GetInt32(0)).ToString(), reader.GetInt32(1)));
                }
            }

            var backlog = new List<PriorityBacklog>();
            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = """
                    WITH active AS (
                        SELECT Priority, DueAtUtc
                        FROM work_orders
                        WHERE Status IN (0, 1, 2)
                    )
                    SELECT Priority,
                           COUNT(*) AS OpenCount,
                           SUM(CASE WHEN DueAtUtc < $now THEN 1 ELSE 0 END) AS OverdueCount
                    FROM active
                    GROUP BY Priority
                    ORDER BY Priority DESC;
                    """;
                AddParameter(cmd, "$now", _clock.UtcNow);
                await using var reader = await cmd.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                {
                    backlog.Add(new PriorityBacklog(
                        ((Priority)reader.GetInt32(0)).ToString(), reader.GetInt32(1), reader.GetInt32(2)));
                }
            }

            double? avgHours;
            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = """
                    SELECT AVG((julianday(ResolvedAtUtc) - julianday(CreatedAtUtc)) * 24.0)
                    FROM work_orders
                    WHERE ResolvedAtUtc IS NOT NULL;
                    """;
                var value = await cmd.ExecuteScalarAsync(ct);
                avgHours = value is null or DBNull ? null : Math.Round(Convert.ToDouble(value), 2);
            }

            return new SummaryReport(byStatus, backlog, avgHours);
        }
        finally
        {
            if (opened) await conn.CloseAsync();
        }
    }

    private static void AddParameter(DbCommand cmd, string name, object value)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Value = value;
        cmd.Parameters.Add(p);
    }
}
