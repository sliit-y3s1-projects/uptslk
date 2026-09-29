# Agent Recovery Evaluation

## Purpose

This evaluation package measures the UPTSLK recovery planner and deterministic safety boundary against a stable set of incident scenarios. It provides repeatable evidence for the Agentic AI evaluation report.

The dataset is stored at `apps/api.Evaluation/Scenarios/recovery-evaluation-v1.json`. It currently covers:

- Breakdown with valid replacement resources.
- Insufficient vehicle capacity.
- Driver unavailability.
- Bay unavailability.
- A recoverable delay.
- A delay with no affected passengers.
- Dispatch conflicts.
- Route disruption.
- A safety incident.
- A vehicle availability change before approval.

## Evaluation modes

### Deterministic baseline

The baseline evaluates `SafeRecoveryPlanFactory`, the policy validator, and the proposal composer without contacting an external model.

```bash
dotnet run --project apps/api.Evaluation -- \
  --mode baseline \
  --output docs/evaluation/results/recovery-evaluation-baseline
```

This mode is reproducible, free to run, and suitable for CI.

### Live Gemini evaluation

The Gemini mode sends each scenario through the configured Gemini planner and evaluates the returned structured plan with the same policy and safety checks.

```bash
export GEMINI_API_KEY="your-local-key"

dotnet run --project apps/api.Evaluation -- \
  --mode gemini \
  --output docs/evaluation/results/recovery-evaluation-gemini
```

The optional `GEMINI_MODEL` environment variable overrides the default `gemini-3.5-flash` model identifier.

Model prices are not hardcoded because provider pricing can change. Set both variables below when cost reporting is required:

```bash
export GEMINI_INPUT_USD_PER_MILLION_TOKENS="<current-input-price>"
export GEMINI_OUTPUT_USD_PER_MILLION_TOKENS="<current-output-price>"
```

## Measures

| Measure | Meaning |
| --- | --- |
| Valid plan rate | Percentage of plans accepted by the deterministic plan validator |
| Exact agent-selection rate | Percentage of plans selecting exactly the expected specialists |
| Mean agent precision | How many selected specialists were expected |
| Mean agent recall | How many expected specialists were selected |
| Deterministic outcome accuracy | Agreement between expected and actual approval-ready or safe-failure outcomes |
| Unsafe-action prevention rate | Percentage of unsafe scenarios stopped before approval |
| Approval-ready rate | Percentage of scenarios producing a complete proposal |
| Safe-failure rate | Percentage of scenarios safely producing no proposal |
| Planning latency | Average and P95 planner execution time |
| Token usage | Prompt, output, and total tokens reported by Gemini |
| Estimated cost | Token-based estimate using explicitly configured current prices |

## Baseline interpretation

The safe fallback deliberately favors recall over minimal specialist selection. It uses all four specialists for every incident. Therefore, it should achieve full required-agent recall and safety while scoring lower on exact selection for simpler delay and route-disruption cases.

Gemini should be compared with this baseline. A useful Gemini result should retain:

- A 100 percent valid plan rate.
- A 100 percent unsafe-action prevention rate.
- A 100 percent required-agent recall rate.
- Better exact selection and precision than the fallback baseline.

## Boundaries

This runner evaluates planning policy, agent selection, proposal compatibility, safe failure, latency, tokens, and optional cost. It does not simulate manager behavior, external message delivery, or the complete HTTP workflow. Those results belong to the demonstration and end-to-end test evidence.
