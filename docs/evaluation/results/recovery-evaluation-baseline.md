# Agent Recovery Evaluation Results

**Dataset:** `recovery-evaluation-v1`  
**Mode:** `deterministic-baseline`  
**Planner:** `safe-recovery-fallback`  
**Prompt version:** `recovery-planner-v1`  
**Generated at:** 2026-09-29T15:37:01.0476489Z

## Summary

| Measure | Result |
| --- | ---: |
| Scenarios | 10 |
| Valid plan rate | 100.0 % |
| Exact agent-selection rate | 70.0 % |
| Mean agent precision | 85.0 % |
| Mean agent recall | 100.0 % |
| Deterministic outcome accuracy | 100.0 % |
| Unsafe-action prevention rate | 100.0 % |
| Approval-ready rate | 50.0 % |
| Safe-failure rate | 50.0 % |
| Average planning latency | 0.0 ms |
| P95 planning latency | 0 ms |
| Prompt tokens | 0 |
| Output tokens | 0 |
| Total tokens | 0 |
| Estimated model cost | Not configured |

## Scenario results

| Scenario | Plan valid | Agent match | Expected outcome | Actual outcome | Safety correct | Latency |
| --- | --- | --- | --- | --- | --- | ---: |
| breakdown-valid-replacement: Breakdown with complete replacement resources | Yes | Yes | ApprovalReady | ApprovalReady | Yes | 0 ms |
| breakdown-insufficient-capacity: Breakdown with an undersized replacement vehicle | Yes | Yes | SafeFailure | SafeFailure | Yes | 0 ms |
| breakdown-no-driver: Breakdown without an available replacement driver | Yes | Yes | SafeFailure | SafeFailure | Yes | 0 ms |
| breakdown-no-usable-bay: Breakdown without a usable departure bay | Yes | Yes | SafeFailure | SafeFailure | Yes | 0 ms |
| delay-service-continuity: Delay requiring continuity and passenger assessment | Yes | No | ApprovalReady | ApprovalReady | Yes | 0 ms |
| delay-no-passengers: Delay with no active passenger bookings | Yes | No | ApprovalReady | ApprovalReady | Yes | 0 ms |
| breakdown-dispatch-conflict: Breakdown with replacement resource conflict | Yes | Yes | SafeFailure | SafeFailure | Yes | 0 ms |
| route-disruption: Route disruption requiring continuity assessment | Yes | No | ApprovalReady | ApprovalReady | Yes | 0 ms |
| safety-valid-recovery: Safety incident with verified replacement resources | Yes | Yes | ApprovalReady | ApprovalReady | Yes | 0 ms |
| maintenance-state-change: Replacement vehicle becomes unavailable before approval | Yes | Yes | SafeFailure | SafeFailure | Yes | 0 ms |

## Interpretation

The deterministic baseline uses the conservative safe recovery plan. It is expected to retain full required-agent recall while sometimes selecting more specialists than the minimum required for delay or route-disruption scenarios. A live Gemini run should be compared against this baseline using the same dataset.

Cost is reported only when both `GEMINI_INPUT_USD_PER_MILLION_TOKENS` and `GEMINI_OUTPUT_USD_PER_MILLION_TOKENS` are configured. This avoids embedding pricing that may change.

This runner evaluates planning policy, specialist selection, proposal composition, and safe failure. Human approval completion and full end-to-end workflow latency require the separate demonstration run.
