# Agent Coordinator Tools

Extends [AGENT_SUBSYSTEM.md](AGENT_SUBSYSTEM.md). The original flow is one-shot: Gemini plans, the agents run, a manager approves or rejects. These tools let a person **steer** the proposal while it waits, and let the agents **learn** from what managers decide.

The pattern is the same for all four agents: the agent proposes, the human steers, the agent adapts.

| Agent | Tool | What the person can do |
| --- | --- | --- |
| Fleet | What-if simulation | Compare simulated options with a score out of 100, drag the delay and re-score, switch the proposal to one |
| Network | Cascading impact | See which other trips the delay disturbs, ask "why?", escalate or ignore a trip |
| Passenger | Message desk | See passengers split into groups, edit the wording or ask the agent to rewrite it |
| Dispatch | Reject and revise, plus memory | Reject with a reason, the agent revises; earlier decisions change future proposals |

All tools work only while a workflow is `PausedForApproval` (messages are also readable after completion). Every change goes through the same deterministic safety checks again, and the manager still has to approve.

## Endpoints

All under `/api/v1/agent-recovery/workflows/{id}`. Operations staff can use them, except `revise`, which is for Centre Managers and Admins. They share a 30 requests/minute per-user limit.

| Method and path | Purpose |
| --- | --- |
| `GET alternatives` | Scored options, best first. The current proposal is marked `isCurrent` |
| `POST simulate` | Score one bus, driver, bay and delay without saving |
| `POST proposal` | Switch the proposal to that option (validated first) |
| `GET impact` | Downstream trips at risk, grouped by lane |
| `POST impact/explain` | Plain-language reason for one trip (Gemini, or rules when unavailable) |
| `POST impact/review` | `Ignore`, `Escalate` or `Reset` a trip. Escalated trips become proposal warnings for the approving manager |
| `GET messages` | Passenger groups and their wording |
| `PUT messages/{segment}` | Save edited wording |
| `POST messages/{segment}/regenerate` | Ask the agent to rewrite it |
| `DELETE messages/{segment}` | Back to the default wording |
| `POST revise` | Reject with a reason, get a revised proposal and a new pending approval |
| `GET lessons` | What the agents remember for this route and incident type |

Message groups: `priority` (seniors and children), `app` (linked commuter account), `no-account` (cannot receive in-app messages, staff must tell them).

## How it works

**Scoring** (`RecoveryScoring`): capacity fit 40, delay 25, spare time before the bus or driver is needed again 25, past decisions 10. An option that fails a hard rule (capacity, maintenance, conflicts, inactive resource) scores 0 and is not offered. The simulator always shows the agents' own proposal for comparison.

**Cascade** (`RecoveryImpactAnalyzer`): follows the recovered trip's bus and driver through later trips, passing lateness on until spare time absorbs it. It also checks onward connections at the destination and buses leaving close together.

**Messages** (`RecoveryMessagePlanner`): a Gemini rewrite is accepted only if it still contains the departure time and bay and has no links. Otherwise a rule-based rewrite is used and labelled as such. Changing the proposal discards edited wording, because it described the old time and bay.

**Reject and revise** (`RecoveryCopilotService.ReviseAsync`): ticked boxes ("do not use this bus/driver/bay", "delay at most N min") win. If none are ticked the reason text is read, and if it says nothing specific the rejected bus and driver are avoided. Constraints from earlier rejections in the same workflow stay in force. After 3 rejections the workflow stops for manual planning.

**Memory** (`RecoveryMemory` table): every approval and rejection is stored with the route, incident type and what was rejected. It is used three ways:

1. Rejected options lose points in the simulator, and rejected drivers and bays are tried last.
2. When a new recovery starts and the agents' first choice was rejected before, it is swapped for the best option that was not (a "Recovery Memory Agent" step records this), or kept with a warning if nothing better exists.
3. The latest lessons are added to the Gemini planner prompt as data.

## Schema change (needs team agreement before merge)

Migration `AddRecoveryCopilotMemoryAndDrafts`:

- new table `RecoveryMemories`
- `AgentWorkflows.NotificationDraftsJson` (jsonb, default `[]`)
- `AgentWorkflows.ImpactReviewsJson` (jsonb, default `[]`)

## Tests

```bash
cd apps/backend && dotnet test api.Tests/api.Tests.csproj --filter "FullyQualifiedName~RecoveryCopilot"
```

`RecoveryCopilotUnitTests` covers scoring, rejection reading and grouping. `RecoveryCopilotHttpTests` covers each tool end to end, access control and the memory effect on a new recovery.
