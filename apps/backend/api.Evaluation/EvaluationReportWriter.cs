using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Upts.AgentEvaluation;

public static class EvaluationReportWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static async Task WriteAsync(
        RecoveryEvaluationReport report,
        string outputPrefix,
        CancellationToken cancellationToken)
    {
        var fullPrefix = Path.GetFullPath(outputPrefix);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPrefix)!);
        await File.WriteAllTextAsync(
            $"{fullPrefix}.json",
            JsonSerializer.Serialize(report, JsonOptions),
            cancellationToken);
        await File.WriteAllTextAsync(
            $"{fullPrefix}.md",
            CreateMarkdown(report),
            cancellationToken);
    }

    private static string CreateMarkdown(RecoveryEvaluationReport report)
    {
        var metrics = report.Metrics;
        var text = new StringBuilder()
            .AppendLine("# Agent Recovery Evaluation Results")
            .AppendLine()
            .AppendLine($"**Dataset:** `{report.DatasetVersion}`  ")
            .AppendLine($"**Mode:** `{report.EvaluationMode}`  ")
            .AppendLine($"**Planner:** `{report.Planner}`  ")
            .AppendLine($"**Prompt version:** `{report.PromptVersion}`  ")
            .AppendLine($"**Generated at:** {report.GeneratedAtUtc:O}")
            .AppendLine()
            .AppendLine("## Summary")
            .AppendLine()
            .AppendLine("| Measure | Result |")
            .AppendLine("| --- | ---: |")
            .AppendLine($"| Scenarios | {metrics.ScenarioCount} |")
            .AppendLine($"| Valid plan rate | {Percent(metrics.ValidPlanRate)} |")
            .AppendLine($"| Exact agent-selection rate | {Percent(metrics.ExactAgentSelectionRate)} |")
            .AppendLine($"| Mean agent precision | {Percent(metrics.MeanAgentPrecision)} |")
            .AppendLine($"| Mean agent recall | {Percent(metrics.MeanAgentRecall)} |")
            .AppendLine($"| Deterministic outcome accuracy | {Percent(metrics.DeterministicOutcomeAccuracy)} |")
            .AppendLine($"| Unsafe-action prevention rate | {Percent(metrics.UnsafeActionPreventionRate)} |")
            .AppendLine($"| Approval-ready rate | {Percent(metrics.ApprovalReadyRate)} |")
            .AppendLine($"| Safe-failure rate | {Percent(metrics.SafeFailureRate)} |")
            .AppendLine($"| Average planning latency | {metrics.AveragePlanningLatencyMs:F1} ms |")
            .AppendLine($"| P95 planning latency | {metrics.P95PlanningLatencyMs} ms |")
            .AppendLine($"| Prompt tokens | {metrics.PromptTokenCount} |")
            .AppendLine($"| Output tokens | {metrics.OutputTokenCount} |")
            .AppendLine($"| Total tokens | {metrics.TotalTokenCount} |")
            .AppendLine($"| Estimated model cost | {FormatCost(metrics.EstimatedCostUsd)} |")
            .AppendLine()
            .AppendLine("## Scenario results")
            .AppendLine()
            .AppendLine("| Scenario | Plan valid | Agent match | Expected outcome | Actual outcome | Safety correct | Latency |")
            .AppendLine("| --- | --- | --- | --- | --- | --- | ---: |");

        foreach (var result in report.Scenarios)
        {
            text.AppendLine(
                $"| {result.Id}: {result.Name} | {YesNo(result.PlanValid)} | {YesNo(result.ExactAgentSelection)} | {result.ExpectedOutcome} | {result.ActualOutcome} | {YesNo(result.OutcomeCorrect)} | {result.PlanningDurationMs} ms |");
        }

        text.AppendLine()
            .AppendLine("## Interpretation")
            .AppendLine()
            .AppendLine("The deterministic baseline uses the conservative safe recovery plan. It is expected to retain full required-agent recall while sometimes selecting more specialists than the minimum required for delay or route-disruption scenarios. A live Gemini run should be compared against this baseline using the same dataset.")
            .AppendLine()
            .AppendLine("Cost is reported only when both `GEMINI_INPUT_USD_PER_MILLION_TOKENS` and `GEMINI_OUTPUT_USD_PER_MILLION_TOKENS` are configured. This avoids embedding pricing that may change.")
            .AppendLine()
            .AppendLine("This runner evaluates planning policy, specialist selection, proposal composition, and safe failure. Human approval completion and full end-to-end workflow latency require the separate demonstration run.");

        return text.ToString();
    }

    private static string Percent(double value) => value.ToString("P1", CultureInfo.InvariantCulture);
    private static string YesNo(bool value) => value ? "Yes" : "No";
    private static string FormatCost(decimal? value) => value.HasValue
        ? $"USD {value.Value.ToString("F6", CultureInfo.InvariantCulture)}"
        : "Not configured";
}
