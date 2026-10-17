namespace api.Services.AgentRecovery.Copilot;

// Free-text generation for the coordinator tools: explanations, rewritten passenger messages and revision rationales.
public interface IRecoveryNarrator
{
    // Returns null when the AI is disabled, fails or times out, so callers fall back to rule-based wording.
    Task<string?> GenerateAsync(string instruction, string prompt, CancellationToken cancellationToken);
}
