# Security review guide

This guide scopes security reviews for the UPTSLK academic prototype. It is a review checklist, not a security certification or a claim that every control has passed.

## Review boundaries

| Boundary | Data or action to protect | Review focus |
| --- | --- | --- |
| Web/mobile to API | Identity, passenger profiles, booking identifiers | Validate authorization at the API for each request, including requests made outside the UI. |
| Staff role and centre | Operational records and administrative actions | Check both allowed actions and denied actions across roles and centres. |
| Booking and wallet | Seat allocation, balances, cancellations | Check ownership, duplicate requests, transaction consistency, and refund amounts. |
| Payment provider | Checkout, webhook, refund events | Verify signatures, event replay handling, and server-side payment reconciliation. |
| External storage and AI | Uploaded content and recovery input | Review credentials, input restrictions, output validation, and human approval. |

The API and test projects are under `apps/backend/` on current `main`. Web and mobile remain under `apps/web/` and `apps/mobile/`.

## Before sharing review evidence

- Use synthetic passengers, test payment credentials, and an isolated database.
- Remove tokens, passwords, connection strings, payment details, and personal information from logs and screenshots.
- Record the commit, environment, command, expected result, actual result, and limitations.
- Report a suspected vulnerability privately to the repository maintainers through an available private channel. Do not put credentials or exploit details in a public issue. This guide does not establish a response deadline or supported-version policy.

## Completion criteria

A review is complete only when its scope and evidence are recorded, failures have an owner, and untested areas are listed explicitly. Passing client tests does not establish API authorization or payment-provider security.

Start with the existing [backend test guide](../../apps/backend/api.Tests/README.md) and [web test guide](../../apps/web/src/test/README.md). Keep dependency updates and security fixes in separately reviewed implementation changes.
