# Component D: Passengers and Fares: Web Test Cases

**Owner:** Nadeesha-D-Shalom · **Test folder:** `apps/web/src/test/bookings/` · **Last run:** 2026-10-08
**Application code covered:** `apps/web/src/features/fares/**`, `riders/**` and `bookings/**`

## Result

**39 of 39 tests pass** in 9 test files (`pnpm test src/test/bookings`).

| Category (from the assignment) | Tests |
|---|---|
| Component rendering | 13 |
| Form validation | 4 |
| Protected route | 6 |
| API integration | 5 |
| UI state / error state | 11 |

Categories are assigned from the wording of each test name; a test can touch more than one area.

## How every test case works

- **Precondition:** the page is rendered inside the app's providers (router, React Query, auth context) as the stated role (or anonymous). Every API call is answered by a mock (MSW). A request without a mock fails the test, so no real server is used.
- **Steps:** render the page or route, then act as a user would (type, click, choose from a list) where the test name says so.
- **Expected result:** the behaviour stated in the test name (what is shown, which request is sent with which method, URL and body, or that no request is sent).
- **Actual result and Pass/Fail:** the automated assertions decide; the Result column shows the outcome of the last run.

## Test cases

| ID | Feature (file) | Test case: what is checked and the expected behaviour | Category | Result |
|---|---|---|---|---|
| WEB-D-001 | AccessAndFareRules.test.tsx · RequireRole | shows the restricted message for a commuter user | Protected route | Pass |
| WEB-D-002 | AccessAndFareRules.test.tsx · RequireRole | renders the protected content for an authorised staff role | Protected route | Pass |
| WEB-D-003 | AccessAndFareRules.test.tsx · RequireRole | shows nothing for an anonymous user | Protected route | Pass |
| WEB-D-004 | AccessAndFareRules.test.tsx · FareRules | rejects a fare rule without a selected route | Form validation | Pass |
| WEB-D-005 | AccessAndFareRules.test.tsx · FareRules | rejects a fare rule with a non-positive amount | Form validation | Pass |
| WEB-D-006 | BookingCancellation.test.tsx · Booking cancellation | requires a reason and sends it when cancelling a confirmed booking | Form validation | Pass |
| WEB-D-007 | BookingManagement.test.tsx · Booking management | renders booking route, passenger, fare, and status from the API | Component rendering | Pass |
| WEB-D-008 | BookingManagement.test.tsx · Booking management | shows an empty state when no bookings match the filters | UI state / error state | Pass |
| WEB-D-009 | BookingTicket.test.tsx · BookingTicket | renders a cancelled ticket with refund and wallet details | Component rendering | Pass |
| WEB-D-010 | BookingTicket.test.tsx · BookingTicket | shows a retryable error when the ticket API fails | UI state / error state | Pass |
| WEB-D-011 | CommuterTickets.test.tsx · ticketGroup (pure function) | puts a cancelled booking in Cancelled | Component rendering | Pass |
| WEB-D-012 | CommuterTickets.test.tsx · ticketGroup (pure function) | puts a ticket for a cancelled trip in Cancelled | Component rendering | Pass |
| WEB-D-013 | CommuterTickets.test.tsx · ticketGroup (pure function) | puts a completed booking or completed trip in Past | Component rendering | Pass |
| WEB-D-014 | CommuterTickets.test.tsx · ticketGroup (pure function) | puts a future departure in Upcoming and a past departure in Past | Component rendering | Pass |
| WEB-D-015 | CommuterTickets.test.tsx · ticketGroup (pure function) | keeps a trip that is boarding, delayed or dispatched Upcoming until its active window ends | Component rendering | Pass |
| WEB-D-016 | CommuterTickets.test.tsx · ticketGroup (pure function) | falls back to a two hour window when the server sends no activeUntil | API integration | Pass |
| WEB-D-017 | CommuterTickets.test.tsx · canBoard and ticketLabel (pure functions) | allows boarding only for a confirmed, upcoming ticket with a QR code that the server allows | Component rendering | Pass |
| WEB-D-018 | CommuterTickets.test.tsx · canBoard and ticketLabel (pure functions) | labels tickets by their state | Component rendering | Pass |
| WEB-D-019 | CommuterTickets.test.tsx · MyTicketsPage | requests the signed-in commuter's tickets and shows the Upcoming group first with counts on the filters | API integration | Pass |
| WEB-D-020 | CommuterTickets.test.tsx · MyTicketsPage | switches between Upcoming, Past and Cancelled | Component rendering | Pass |
| WEB-D-021 | CommuterTickets.test.tsx · MyTicketsPage | shows an empty message for a group that has no bookings | UI state / error state | Pass |
| WEB-D-022 | CommuterTickets.test.tsx · MyTicketsPage | shows a loading state and then an error message when the API fails | UI state / error state | Pass |
| WEB-D-023 | CommuterTickets.test.tsx · MyTicketsPage | opens a boarding ticket with the QR code for a valid upcoming ticket | Component rendering | Pass |
| WEB-D-024 | CommuterTickets.test.tsx · MyTicketsPage | shows booking details without a QR code for a ticket that cannot be used to board | UI state / error state | Pass |
| WEB-D-025 | CommuterTickets.test.tsx · BookingPaymentStatusPage (payment return) | confirms a successful payment and links to the commuter's tickets | Component rendering | Pass |
| WEB-D-026 | CommuterTickets.test.tsx · BookingPaymentStatusPage (payment return) | tells the commuter to wait while the payment is pending, without a tickets link | UI state / error state | Pass |
| WEB-D-027 | CommuterTickets.test.tsx · BookingPaymentStatusPage (payment return) | explains that a failed payment was not completed | UI state / error state | Pass |
| WEB-D-028 | CommuterTickets.test.tsx · BookingPaymentStatusPage (payment return) | shows the cancelled message on the cancel page without asking the API | UI state / error state | Pass |
| WEB-D-029 | CommuterTickets.test.tsx · BookingPaymentStatusPage (payment return) | shows an error when the payment status cannot be loaded | UI state / error state | Pass |
| WEB-D-030 | CommuterTickets.test.tsx · Commuter access in the app | lets a commuter open My tickets | Protected route | Pass |
| WEB-D-031 | CommuterTickets.test.tsx · Commuter access in the app | blocks a commuter from the staff booking and passenger pages | Protected route | Pass |
| WEB-D-032 | CommuterTickets.test.tsx · Commuter access in the app | lets a dispatcher open the staff booking page | Protected route | Pass |
| WEB-D-033 | CreateBooking.test.tsx · CreateBooking | selects a trip and passenger, then starts checkout with the selected values | API integration | Pass |
| WEB-D-034 | PassengerAndFareForms.test.tsx · Passenger form | keeps saving disabled until name and phone are provided, then creates the passenger | Form validation | Pass |
| WEB-D-035 | PassengerAndFareForms.test.tsx · Fare rule integration | sends the selected route, category, and amount when saving a fare rule | API integration | Pass |
| WEB-D-036 | RidersPage.test.tsx · RidersPage | shows passengers from the API and narrows the list by search | Component rendering | Pass |
| WEB-D-037 | RidersPage.test.tsx · RidersPage | shows an empty state when no passengers match the current filter | UI state / error state | Pass |
| WEB-D-038 | WalletPayments.test.tsx · Wallet payments | submits a wallet top-up and shows the success state | API integration | Pass |
| WEB-D-039 | WalletPayments.test.tsx · Wallet payments | shows the API error when a wallet top-up fails | UI state / error state | Pass |

## Commands and results (2026-10-08)

| Command (run in `apps/web`) | Result |
|---|---|
| `pnpm test src/test/bookings` | 39 passed, 0 failed |
| `pnpm test` (all web tests) | 206 passed (29 files) |
| `pnpm lint` | 0 errors, 20 warnings that were already in the project |
| `pnpm build` | passed |

### Coverage of this component's code (`apps/web/src/features/fares/**`, `riders/**` and `bookings/**`)

| Statements | Branches | Functions | Lines |
|---|---|---|---|
| 46.24% (701/1516) | 48.98% (481/982) | 48.27% (182/377) | 45.7% (639/1398) |

## Evidence files (`docs/testing/evidence/`)

- `web-bookings-tests.log`: full list of tests with results.
- `web-bookings-coverage.log` and `web-bookings-coverage.png`: coverage output and a screenshot of the HTML report (`apps/web/coverage/bookings/index.html` after running `pnpm test src/test/bookings --coverage ...`).
- Defects and their before/after runs: see `docs/testing/web-bookings-defects.md`.

## AI usage declaration (CLEAR)

### Declaration written by the member (kept exactly as submitted in PR #19)

- **Context:** I provided the repository structure, assignment requirements, existing test foundation, and the target booking/passenger/fare source files.
- **Request:** I asked AI to inspect the implemented pages, write MSW-backed React tests one file at a time, run each focused suite, and fix only defects demonstrated by failing tests.
- **Execution:** AI added tests for passengers, wallets, bookings, tickets, seats, fare rules, protected access, validation, API integration, and UI error states. It ran Vitest, coverage, lint, and build commands.
- **Assessment:** I reviewed the generated test names, handlers, payload assertions, and UI queries. The tests use the shared React test harness and MSW rather than mocking the API client.
- **Revision:** A fare-rule validation defect and native browser validation conflict were fixed after failing tests exposed them. The shared test harness also required type alignment with the current `AuthContextType` before the production build passed.

### Tests and fixes added afterwards with AI
- **Tool:** Claude Code (model Claude Sonnet 5.5), in a chat session run by Chamal Senarathna on 2026-10-08.
- **Concise:** Chamal's instruction was short: finish the remaining items from the review of the merged work (no need to ask the member).
- **Logical:** The assistant compared the merged work with the issue checklist and found: a fare-rule test whose assertion contradicted its title (it hid a real bug), evidence and documents that no longer matched the merged code (19 tests / 9 files in the notes against 17 / 8 on `main`), and the commuter items of the issue that were untested although the pages exist on `main`. It then fixed the bug, added the missing tests, and regenerated the evidence.
- **Explicit:** Rules: mock the network only with MSW; write only in `apps/web/src/test/bookings/`; no changes to the shared helpers; change application code only for a defect proven by a failing test.
- **Adaptive:** The first version of the fixed test was run against the old code to prove it fails (before/after logs). The new app-level tests for commuter access were strengthened after the assistant noticed one could pass before the page finished rendering.
- **Reflective:** All new tests were run, then the full suite, lint and build. **The member must read these changes (the `FareRules.tsx` fix and `CommuterTickets.test.tsx`) and confirm she understands them.** The paragraph above this one is her own account and was not changed.
