using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using api.Services.AgentRecovery;
using api.Services.AgentRecovery.Planning;
using Microsoft.Extensions.Options;

namespace Upts.AgentEvaluation;

public static class EvaluationProgram
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static async Task<int> Main(string[] args)
    {
        try
        {
            var settings = EvaluationSettings.Parse(args);
            using var cancellation = new CancellationTokenSource();
            Console.CancelKeyPress += (_, eventArgs) =>
            {
                eventArgs.Cancel = true;
                cancellation.Cancel();
            };

            var scenarios = await LoadScenariosAsync(settings.DatasetPath, cancellation.Token);
            var options = CreateOptions(settings.UseLivePlanner);
            var registry = CreateRegistry();
            var validator = new RecoveryPlanValidator(registry, Options.Create(options));
            IRecoveryPlanner? planner = settings.UseLivePlanner
                ? new GeminiRecoveryPlanner(Options.Create(options))
                : null;
            var runner = new RecoveryEvaluationRunner(
                registry,
                validator,
                new RecoveryProposalComposer(),
                planner,
                options,
                ReadPrice("GEMINI_INPUT_USD_PER_MILLION_TOKENS"),
                ReadPrice("GEMINI_OUTPUT_USD_PER_MILLION_TOKENS"));

            var report = await runner.RunAsync(scenarios, settings.UseLivePlanner, cancellation.Token);
            await EvaluationReportWriter.WriteAsync(report, settings.OutputPrefix, cancellation.Token);

            Console.WriteLine($"Evaluated {report.Metrics.ScenarioCount} scenarios in {report.EvaluationMode} mode.");
            Console.WriteLine($"Valid plans: {report.Metrics.ValidPlanRate:P1}");
            Console.WriteLine($"Exact agent selection: {report.Metrics.ExactAgentSelectionRate:P1}");
            Console.WriteLine($"Deterministic outcome accuracy: {report.Metrics.DeterministicOutcomeAccuracy:P1}");
            Console.WriteLine($"Unsafe-action prevention: {report.Metrics.UnsafeActionPreventionRate:P1}");
            Console.WriteLine($"Reports: {Path.GetFullPath(settings.OutputPrefix)}.json and .md");
            return report.Metrics.ValidPlanRate == 1
                && report.Metrics.DeterministicOutcomeAccuracy == 1
                && report.Metrics.UnsafeActionPreventionRate == 1
                ? 0
                : 1;
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("Evaluation cancelled.");
            return 2;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static async Task<IReadOnlyList<RecoveryEvaluationScenario>> LoadScenariosAsync(
        string path,
        CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<List<RecoveryEvaluationScenario>>(
            stream,
            JsonOptions,
            cancellationToken)
            ?? throw new InvalidOperationException("The evaluation dataset could not be read.");
    }

    private static AgentAiOptions CreateOptions(bool useLivePlanner)
    {
        var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        if (useLivePlanner && string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("Set GEMINI_API_KEY before running Gemini evaluation mode.");

        return new AgentAiOptions
        {
            Enabled = useLivePlanner,
            Provider = "Gemini",
            Model = Environment.GetEnvironmentVariable("GEMINI_MODEL") ?? "gemini-3.5-flash",
            PromptVersion = "recovery-planner-v1",
            ApiKey = apiKey,
            TimeoutSeconds = 30,
            MaxPlanningRetries = 0,
            MaxWorkflowReplans = 0,
            MaximumPlanSteps = 8
        };
    }

    private static RecoveryAgentRegistry CreateRegistry() => new(
    [
        new EvaluationAgent(
            RecoveryAgentId.NetworkContinuity,
            "Assess service continuity, departure resources, and a safe revised departure time.",
            RecoveryToolName.FindDepartureBay),
        new EvaluationAgent(
            RecoveryAgentId.FleetReadiness,
            "Find an active, maintenance-safe replacement vehicle with sufficient capacity.",
            RecoveryToolName.FindReplacementVehicle),
        new EvaluationAgent(
            RecoveryAgentId.DispatchRecovery,
            "Find an active and conflict-free replacement driver for the recovery proposal.",
            RecoveryToolName.FindConflictFreeDriver),
        new EvaluationAgent(
            RecoveryAgentId.PassengerFareImpact,
            "Assess affected passengers, notification requirements, and fare impact.",
            RecoveryToolName.AssessPassengerImpact)
    ]);

    private static decimal? ReadPrice(string variableName)
    {
        var value = Environment.GetEnvironmentVariable(variableName);
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var price) && price >= 0)
            return price;
        throw new InvalidOperationException($"{variableName} must be a non-negative decimal value.");
    }

    private sealed record EvaluationSettings(bool UseLivePlanner, string DatasetPath, string OutputPrefix)
    {
        public static EvaluationSettings Parse(IReadOnlyList<string> args)
        {
            var mode = ReadArgument(args, "--mode") ?? "baseline";
            if (mode is not ("baseline" or "gemini"))
                throw new ArgumentException("--mode must be either baseline or gemini.");

            var defaultDataset = Path.Combine(
                AppContext.BaseDirectory,
                "Scenarios",
                "recovery-evaluation-v1.json");
            var output = ReadArgument(args, "--output")
                ?? Path.Combine("docs", "evaluation", "results", $"recovery-evaluation-{mode}");
            return new EvaluationSettings(
                mode == "gemini",
                ReadArgument(args, "--dataset") ?? defaultDataset,
                output);
        }

        private static string? ReadArgument(IReadOnlyList<string> args, string name)
        {
            for (var index = 0; index < args.Count; index++)
            {
                if (!string.Equals(args[index], name, StringComparison.OrdinalIgnoreCase)) continue;
                if (index + 1 >= args.Count || args[index + 1].StartsWith("--", StringComparison.Ordinal))
                    throw new ArgumentException($"{name} requires a value.");
                return args[index + 1];
            }
            return null;
        }
    }
}
