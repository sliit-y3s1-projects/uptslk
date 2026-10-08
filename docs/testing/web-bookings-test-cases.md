# Component D Web Test Cases

## Results

| Test | What it checks | Expected result | Result |
|---|---|---|---|
| shows passenger names and narrows the list by search | Passenger list rendering and filtering | Matching passenger remains visible | Pass |
| shows restricted access for a commuter user | Protected route denial | Restricted message is shown | Pass |
| renders protected content for an authorised staff role | Protected route access | Dispatcher content is shown | Pass |
| shows nothing for an anonymous user | Anonymous protected route behavior | Protected content is hidden | Pass |
| rejects a fare rule without a selected route | Form validation | No request; route error is shown | Pass |
| rejects a fare rule with a non-positive amount | Form validation | No request; amount error is shown | Pass |
| loads seats, disables unavailable seats, and reports the selected seat | Component and API integration | Seat data and disabled state render correctly | Pass |
| shows an API error and lets the user retry | UI error state | Error and retry action are shown | Pass |
| renders a cancelled ticket with refund and wallet details | Ticket component rendering | Refund, reason, balance, and transaction render | Pass |
| shows a retryable error when the ticket API fails | Ticket UI error state | API error and retry action are shown | Pass |
| submits a wallet top-up and shows the success state | Payment API integration | Correct amount is posted and success feedback appears | Pass |
| shows the API error when a wallet top-up fails | Payment UI error state | API error feedback appears | Pass |
| selects a trip and seat, then creates a booking with the selected values | Booking API integration | Correct booking payload and callback are produced | Pass |
| renders booking route, passenger, fare, seat, and status from the API | Booking list component rendering | Booking data is visible | Pass |
| shows an empty state when no bookings match the filters | Booking empty UI state | Empty feedback is shown | Pass |
| requires a reason and sends it when cancelling a confirmed booking | Cancellation validation and API integration | Empty reason disables submit; DELETE includes reason | Pass |
| keeps saving disabled until name and phone are provided, then creates the passenger | Passenger form validation and API integration | Required fields enable save; correct POST is sent | Pass |
| sends the selected route, category, and amount when saving a fare rule | Fare-rule API integration | Correct POST payload is sent | Pass |

## Commands and Evidence

- `npx pnpm@12.3.2 test src/test/bookings`: 9 files, 19 tests passed.
- `npx pnpm@12.3.2 test:coverage`: 10 files, 29 tests passed.
- `npx pnpm@12.3.2 lint`: 0 errors, 21 pre-existing warnings.
- `npx pnpm@12.3.2 build`: passed.
- Test evidence: `docs/testing/evidence/web-bookings-tests.log`.
- Coverage evidence: `docs/testing/evidence/web-bookings-coverage.log`.

## Scope Note

The current checkout does not contain the assignment's commuter sources (`PublicBookingPage`, `CheckoutPage`, `MyTicketsPage`, `ticketGroup`) or payment-return routes. `App.tsx` also does not register those routes, so commuter grouping and payment-return tests could not be added without source code that is not present in this branch. Those pages were found on `origin/feat/platform-api-booking-integration`, but that branch changes hundreds of other web files and is not safe to partially copy into this focused branch; the source branch should be merged by the project team before adding those tests.

## AI Usage Declaration (CLEAR)

- **Context:** I provided the repository structure, assignment requirements, existing test foundation, and the target booking/passenger/fare source files.
- **Request:** I asked AI to inspect the implemented pages, write MSW-backed React tests one file at a time, run each focused suite, and fix only defects demonstrated by failing tests.
- **Execution:** AI added tests for passengers, wallets, bookings, tickets, seats, fare rules, protected access, validation, API integration, and UI error states. It ran Vitest, coverage, lint, and build commands.
- **Assessment:** I reviewed the generated test names, handlers, payload assertions, and UI queries. The tests use the shared React test harness and MSW rather than mocking the API client.
- **Revision:** A fare-rule validation defect and native browser validation conflict were fixed after failing tests exposed them. The shared test harness also required type alignment with the current `AuthContextType` before the production build passed.
