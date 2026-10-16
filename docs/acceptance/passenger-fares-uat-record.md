# Passenger and fares acceptance execution record

**Template status: NOT EXECUTED.** Copy this file into an intentionally named results document when running the [acceptance plan](passenger-fares-uat-plan.md). Empty fields and planned cases are not evidence of passing tests.

## Run identification

| Field | Value to enter |
| --- | --- |
| Run ID / date / timezone | Pending |
| Tester / reviewer | Pending |
| Commit SHA / branch | Pending |
| OS / browser or device / runtime versions | Pending |
| Web and API origins / local database identifier | Pending; exclude credentials |
| Provider mode / webhook delivery method | Pending; use test mode |
| Test fixture creation time / trip departure UTC | Pending |
| Automated checks used as supporting evidence | Pending; include exact commands and exit codes |

## Case results

Allowed outcomes: Not run, Pass, Fail, Blocked. Enter Pass only after checking the plan's expected state. For a blocked case, record the missing prerequisite and owner; do not count it as executed.

| Case ID | Outcome | Actual result and before/after values | Evidence / defect reference |
| --- | --- | --- | --- |
| D-UAT-01 | Not run | Pending | Pending |
| D-UAT-02 | Not run | Pending | Pending |
| D-UAT-03 | Not run | Pending | Pending |
| D-UAT-04 | Not run | Pending | Pending |
| D-UAT-05 | Not run | Pending | Pending |
| D-UAT-06 | Not run | Pending | Pending |
| D-UAT-07 | Not run | Pending | Pending |
| D-UAT-08 | Not run | Pending | Pending |
| D-UAT-09 | Not run | Pending | Pending |
| D-UAT-10 | Not run | Pending | Pending |
| D-UAT-11 | Not run | Pending | Pending |
| D-UAT-12 | Not run | Pending | Pending |

## Detailed case evidence

Repeat this block for each executed case, particularly money/capacity actions:

```text
Case ID:
Start/end time (with timezone):
Caller role and synthetic fixture references:
Initial booking/payment/refund state:
Initial wallet balance and occupied/remaining capacity:
Action and request method/path (omit authentication data):
Response status and sanitized relevant fields:
Final state after reload:
Final wallet balance and occupied/remaining capacity:
Provider test evidence, if applicable:
Expected versus actual:
Outcome and evidence location:
Defect/owner/next action, if needed:
```

## Run disposition

Enter totals for Pass, Fail, Blocked, and Not run; they must sum to the 12 planned cases unless a documented scope revision changes the case list. List unresolved High-priority cases, known limitations, and any provider/mobile workflows omitted from the run. Record the reviewer's disposition and date; leave approval pending until an actual reviewer provides it.

Keep synthetic financial evidence available for review. Do not remove ledger rows or edit balances to clean up a run. Document fixture retention/deactivation and any agreed cleanup in the isolated environment. Redact passwords, tokens, connection strings, personal information, and full ticket QR references from shared artifacts.
