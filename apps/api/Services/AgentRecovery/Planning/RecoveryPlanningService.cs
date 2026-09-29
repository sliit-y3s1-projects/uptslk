using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace api.Services.AgentRecovery.Planning;

public sealed class RecoveryPlanningService(
    IRecoveryPlanner planner,
    RecoveryPlanValidator validator,
    IOptions<AgentAiOptions> options,
    ILogger<RecoveryPlanningService> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly AgentAiOptions _options = options.Value;

    public async Task<RecoveryPlanningResult> CreatePlanAsync(
        RecoveryPlanningInput input,
        CancellationToken cancellationToken) =>
        await CreatePlanAsync(input, [], cancellationToken);

    public async Task<RecoveryPlanningResult> CreateRevisedPlanAsync(
        RecoveryPlanningInput input,
        IReadOnlyCollection<string> executionFeedback,
        CancellationToken cancellationToken)
    {
        if (executionFeedback.Count == 0)
            throw new ArgumentException("At least one execution failure is required for replanning.", nameof(executionFeedback));

        return await CreatePlanAsync(input, executionFeedback, cancellationToken);
    }

    private async Task<RecoveryPlanningResult> CreatePlanAsync(
        RecoveryPlanningInput input,
        IReadOnlyCollection<string> initialFeedback,
        CancellationToken cancellationToken)
    {
        var timer = Stopwatch.StartNew();
        var inputJson = JsonSerializer.Serialize(input, JsonOptions);
        var validationFeedback = initialFeedback.Distinct(StringComparer.Ordinal).ToList();
        string? fallbackReason = null;

        if (_options.Enabled && !string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            for (var attempt = 0; attempt <= _options.MaxPlanningRetries; attempt++)
            {
                try
                {
                    using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    timeout.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));
                    var response = await planner.CreatePlanAsync(input, validationFeedback, timeout.Token);
                    var errors = validator.Validate(response.Plan, input.Incident.Type);
                    if (errors.Count == 0)
                    {
                        var outputJson = JsonSerializer.Serialize(response.Plan, JsonOptions);
                        return new RecoveryPlanningResult(
                            response.Plan,
                            "Gemini",
                            _options.Provider,
                            response.Model,
                            _options.PromptVersion,
                            inputJson,
                            outputJson,
                            (int)timer.ElapsedMilliseconds,
                            response.PromptTokenCount,
                            response.OutputTokenCount,
                            response.TotalTokenCount,
                            null);
                    }

                    validationFeedback = initialFeedback
                        .Concat(errors)
                        .Distinct(StringComparer.Ordinal)
                        .ToList();
                    fallbackReason = $"Gemini plan validation failed: {string.Join(" ", errors)}";
                    logger.LogWarning(
                        "Gemini recovery plan for workflow {WorkflowId} failed policy validation on attempt {Attempt}: {Errors}",
                        input.WorkflowId,
                        attempt + 1,
                        string.Join(" | ", errors));
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    fallbackReason = $"Gemini planning exceeded the {_options.TimeoutSeconds}-second timeout.";
                    logger.LogWarning(
                        "Gemini recovery planning timed out for workflow {WorkflowId} on attempt {Attempt}",
                        input.WorkflowId,
                        attempt + 1);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    fallbackReason = $"Gemini planning failed: {exception.Message}";
                    logger.LogWarning(
                        exception,
                        "Gemini recovery planning failed for workflow {WorkflowId} on attempt {Attempt}",
                        input.WorkflowId,
                        attempt + 1);
                }
            }
        }
        else
        {
            fallbackReason = !_options.Enabled
                ? "Gemini planning is disabled by configuration."
                : "GEMINI_API_KEY is not configured.";
        }

        var fallbackPlan = SafeRecoveryPlanFactory.Create(input.Objective);
        var fallbackErrors = validator.Validate(fallbackPlan, input.Incident.Type);
        if (fallbackErrors.Count > 0)
            throw new InvalidOperationException($"The safe recovery fallback plan is invalid: {string.Join(" ", fallbackErrors)}");

        logger.LogWarning(
            "Workflow {WorkflowId} is using the recorded fallback recovery plan. Reason: {Reason}",
            input.WorkflowId,
            fallbackReason);

        return new RecoveryPlanningResult(
            fallbackPlan,
            "Fallback",
            _options.Provider,
            _options.Model,
            _options.PromptVersion,
            inputJson,
            JsonSerializer.Serialize(fallbackPlan, JsonOptions),
            (int)timer.ElapsedMilliseconds,
            0,
            0,
            0,
            fallbackReason);
    }
}
