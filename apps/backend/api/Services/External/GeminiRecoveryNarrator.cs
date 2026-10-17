using api.Services.AgentRecovery.Copilot;
using api.Services.AgentRecovery.Planning;
using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.Extensions.Options;

namespace api.Services.External;

public sealed class GeminiRecoveryNarrator(
    IOptions<AgentAiOptions> options,
    ILogger<GeminiRecoveryNarrator> logger) : IRecoveryNarrator
{
    private readonly AgentAiOptions _options = options.Value;

    public async Task<string?> GenerateAsync(string instruction, string prompt, CancellationToken cancellationToken)
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.ApiKey)) return null;

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));
            var client = new Client(apiKey: _options.ApiKey);
            var response = await client.Models.GenerateContentAsync(
                model: _options.Model,
                contents: prompt,
                config: new GenerateContentConfig
                {
                    SystemInstruction = new Content { Parts = [new Part { Text = instruction }] },
                    MaxOutputTokens = 768
                },
                cancellationToken: timeout.Token);
            return string.IsNullOrWhiteSpace(response.Text) ? null : response.Text.Trim();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            // The caller has a rule-based answer, so a failed AI call is only logged.
            logger.LogWarning(exception, "Gemini narration failed; using the rule-based wording.");
            return null;
        }
    }
}
