# Component C: Scheduling and Dispatch: Web Test Cases

**Owner:** chamals3n4 · **Test folder:** `apps/web/src/test/dispatch/` · **Last run:** 2026-10-08
**Application code covered:** `apps/web/src/features/operations/**`, `fleet/DriversPage.tsx` and the driver hooks/services

## Result

**92 of 92 tests pass** in 8 test files (`pnpm test src/test/dispatch`).

| Category (from the assignment) | Tests |
|---|---|
| Component rendering | 28 |
| Form validation | 9 |
| Protected route | 13 |
| API integration | 18 |
| UI state / error state | 24 |

Categories are assigned from the wording of each test name; a test can touch more than one area.

## How every test case works

- **Precondition:** the page is rendered inside the app's providers (router, React Query, auth context) as the stated role (or anonymous). Every API call is answered by a mock (MSW). A request without a mock fails the test, so no real server is used.
- **Steps:** render the page or route, then act as a user would (type, click, choose from a list) where the test name says so.
- **Expected result:** the behaviour stated in the test name (what is shown, which request is sent with which method, URL and body, or that no request is sent).
- **Actual result and Pass/Fail:** the automated assertions decide; the Result column shows the outcome of the last run.

## Test cases

| ID | Feature (file) | Test case: what is checked and the expected behaviour | Category | Result |
|---|---|---|---|---|
| WEB-C-001 | BayManagement.test.tsx · BayManagementPage | shows each bay with its live state and the summary counts | Component rendering | Pass |
| WEB-C-002 | BayManagement.test.tsx · BayManagementPage | shows the trip on a bay when it is selected | Component rendering | Pass |
| WEB-C-003 | BayManagement.test.tsx · BayManagementPage | shows a message when the centre has no bays | UI state / error state | Pass |
| WEB-C-004 | BayManagement.test.tsx · BayManagementPage | sends POST /api/v1/centres/{id}/bays with the new bay and selects it | API integration | Pass |
| WEB-C-005 | BayManagement.test.tsx · BayManagementPage | shows the API message and keeps the dialog open when the bay code already exists | Component rendering | Pass |
| WEB-C-006 | BayManagement.test.tsx · BayManagementPage | requires a bay code to create a bay | Form validation | Pass |
| WEB-C-007 | BayManagement.test.tsx · BayManagementPage | closes the selected bay with DELETE /api/v1/centres/bays/{id} | API integration | Pass |
| WEB-C-008 | BayManagement.test.tsx · BayManagementPage | releases a bay with PUT status Available | API integration | Pass |
| WEB-C-009 | BayManagement.test.tsx · BayManagementPage | tells the dispatcher when closing a bay is refused | UI state / error state | Pass |
| WEB-C-010 | BayManagement.test.tsx · BayManagementPage | shows a loading message and an error message when the bays cannot be loaded | UI state / error state | Pass |
| WEB-C-011 | DispatchAccess.test.tsx · Dispatch pages: protected routes | lets a CentreManager see the operations pages | Protected route | Pass |
| WEB-C-012 | DispatchAccess.test.tsx · Dispatch pages: protected routes | lets a Dispatcher see the operations pages | Protected route | Pass |
| WEB-C-013 | DispatchAccess.test.tsx · Dispatch pages: protected routes | lets a FleetOfficer see the operations pages | Protected route | Pass |
| WEB-C-014 | DispatchAccess.test.tsx · Dispatch pages: protected routes | lets a Driver see the operations pages | Protected route | Pass |
| WEB-C-015 | DispatchAccess.test.tsx · Dispatch pages: protected routes | blocks a Commuter from the centre staff area with the Access Restricted message | Protected route | Pass |
| WEB-C-016 | DispatchAccess.test.tsx · Dispatch pages: protected routes | blocks a Admin from the centre staff area with the Access Restricted message | Protected route | Pass |
| WEB-C-017 | DispatchAccess.test.tsx · Dispatch pages: protected routes | shows nothing to an anonymous visitor | Protected route | Pass |
| WEB-C-018 | DispatchAccess.test.tsx · Dispatch pages: protected routes | shows the not-found page for an unknown address, even for a signed-in dispatcher | Protected route | Pass |
| WEB-C-019 | DispatchAccess.test.tsx · Dispatch pages: protected routes | lets a dispatcher open the dispatch board in the real app | Protected route | Pass |
| WEB-C-020 | DispatchAccess.test.tsx · Dispatch pages: protected routes | does not give a commuter /operations/dispatch in the real app | Protected route | Pass |
| WEB-C-021 | DispatchAccess.test.tsx · Dispatch pages: protected routes | does not give a commuter /operations/incidents in the real app | Protected route | Pass |
| WEB-C-022 | DispatchAccess.test.tsx · Dispatch pages: protected routes | does not give a commuter /operations/bays in the real app | Protected route | Pass |
| WEB-C-023 | DispatchAccess.test.tsx · Dispatch pages: protected routes | does not give a commuter /fleet/drivers in the real app | Protected route | Pass |
| WEB-C-024 | DispatchBoard.test.tsx · DispatchPage (dispatch board) | places each active trip in the column for its status and shows route, vehicle, bay and driver | Component rendering | Pass |
| WEB-C-025 | DispatchBoard.test.tsx · DispatchPage (dispatch board) | does not show completed or cancelled trips on the board | Component rendering | Pass |
| WEB-C-026 | DispatchBoard.test.tsx · DispatchPage (dispatch board) | narrows the board when searching by vehicle | Component rendering | Pass |
| WEB-C-027 | DispatchBoard.test.tsx · DispatchPage (dispatch board) | links each card to its trip details and offers to schedule a new trip | Component rendering | Pass |
| WEB-C-028 | DispatchBoard.test.tsx · DispatchPage (dispatch board) | starts boarding for the selected trip with PATCH /api/v1/trips/{id}/status | API integration | Pass |
| WEB-C-029 | DispatchBoard.test.tsx · DispatchPage (dispatch board) | only allows dispatching a bus that is boarding | Form validation | Pass |
| WEB-C-030 | DispatchBoard.test.tsx · DispatchPage (dispatch board) | does not offer Flag delay or boarding actions that the trip's state forbids | Form validation | Pass |
| WEB-C-031 | DispatchBoard.test.tsx · DispatchPage (dispatch board) | shows a loading message, then an empty board when there are no trips | UI state / error state | Pass |
| WEB-C-032 | DispatchBoard.test.tsx · DispatchPage (dispatch board) | shows an error message when the trips cannot be loaded | UI state / error state | Pass |
| WEB-C-033 | DispatchBoard.test.tsx · DispatchPage (dispatch board) | tells the dispatcher why the API rejected a status change | UI state / error state | Pass |
| WEB-C-034 | Drivers.test.tsx · DriversPage | lists the centre's drivers with licence, phone and status | Component rendering | Pass |
| WEB-C-035 | Drivers.test.tsx · DriversPage | asks the API to search when the dispatcher types a name or licence | API integration | Pass |
| WEB-C-036 | Drivers.test.tsx · DriversPage | shows a loading message and then an empty message | UI state / error state | Pass |
| WEB-C-037 | Drivers.test.tsx · DriversPage | shows the error and loads the drivers again when Retry is clicked | UI state / error state | Pass |
| WEB-C-038 | Drivers.test.tsx · DriverFormPage | rejects a driver with no licence number | Form validation | Pass |
| WEB-C-039 | Drivers.test.tsx · DriverFormPage | rejects a driver with no email | Form validation | Pass |
| WEB-C-040 | Drivers.test.tsx · DriverFormPage | rejects a driver with a short password | Form validation | Pass |
| WEB-C-041 | Drivers.test.tsx · DriverFormPage | sends POST /api/v1/drivers with a lower-case email and upper-case licence, then opens the profile | API integration | Pass |
| WEB-C-042 | Drivers.test.tsx · DriverFormPage | shows the API message when the licence or email already exists | Component rendering | Pass |
| WEB-C-043 | Drivers.test.tsx · DriverDetailPage | shows the driver profile | Component rendering | Pass |
| WEB-C-044 | Drivers.test.tsx · DriverDetailPage | deactivates the driver with DELETE /api/v1/drivers/{id} after confirmation and returns to the list | API integration | Pass |
| WEB-C-045 | Drivers.test.tsx · DriverDetailPage | does nothing when the dispatcher declines the confirmation | Component rendering | Pass |
| WEB-C-046 | Drivers.test.tsx · DriverDetailPage | hides Deactivate for an inactive driver and shows an error for an unknown driver | API integration | Pass |
| WEB-C-047 | Drivers.test.tsx · DriverDetailPage | shows the API message when deactivation is refused | API integration | Pass |
| WEB-C-048 | Incidents.test.tsx · IncidentsPage | groups incidents as Open, Investigating and Resolved with counts | Component rendering | Pass |
| WEB-C-049 | Incidents.test.tsx · IncidentsPage | shows the route of a trip incident, 'Centre-wide incident' otherwise, and who owns it | Component rendering | Pass |
| WEB-C-050 | Incidents.test.tsx · IncidentsPage | starts an investigation with PUT /api/v1/incidents/{id}, keeping the other fields | API integration | Pass |
| WEB-C-051 | Incidents.test.tsx · IncidentsPage | resolves an investigated incident, and offers no action on a resolved one | API integration | Pass |
| WEB-C-052 | Incidents.test.tsx · IncidentsPage | tells the dispatcher when an incident update is rejected | UI state / error state | Pass |
| WEB-C-053 | Incidents.test.tsx · IncidentsPage | shows a loading message and an error message when the incidents cannot be loaded | UI state / error state | Pass |
| WEB-C-054 | Incidents.test.tsx · IncidentFormPage | requires a summary and details before it can be submitted | Form validation | Pass |
| WEB-C-055 | Incidents.test.tsx · IncidentFormPage | sends POST /api/v1/incidents linked to the trip in the link, then returns to the incident list | API integration | Pass |
| WEB-C-056 | Incidents.test.tsx · IncidentFormPage | can report a centre-wide incident without a trip | API integration | Pass |
| WEB-C-057 | Incidents.test.tsx · IncidentFormPage | tells the dispatcher when the incident cannot be saved and stays on the form | UI state / error state | Pass |
| WEB-C-058 | TripDetail.test.tsx · TripDetailPage: details | shows the route, bay, vehicle, driver, capacity and notes | Component rendering | Pass |
| WEB-C-059 | TripDetail.test.tsx · TripDetailPage: details | links to the incident form for this trip, and to the edit form while the trip is active | Component rendering | Pass |
| WEB-C-060 | TripDetail.test.tsx · TripDetailPage: details | explains a Scheduled trip in the lifecycle panel | UI state / error state | Pass |
| WEB-C-061 | TripDetail.test.tsx · TripDetailPage: details | explains a Boarding trip in the lifecycle panel | UI state / error state | Pass |
| WEB-C-062 | TripDetail.test.tsx · TripDetailPage: details | explains a Dispatched trip in the lifecycle panel | UI state / error state | Pass |
| WEB-C-063 | TripDetail.test.tsx · TripDetailPage: details | explains a Completed trip in the lifecycle panel | UI state / error state | Pass |
| WEB-C-064 | TripDetail.test.tsx · TripDetailPage: details | explains a Delayed trip in the lifecycle panel | UI state / error state | Pass |
| WEB-C-065 | TripDetail.test.tsx · TripDetailPage: details | explains a Cancelled trip in the lifecycle panel | UI state / error state | Pass |
| WEB-C-066 | TripDetail.test.tsx · TripDetailPage: details | offers the next step for a Scheduled trip: Mark ready | Component rendering | Pass |
| WEB-C-067 | TripDetail.test.tsx · TripDetailPage: details | offers the next step for a Ready trip: Mark boarding | Component rendering | Pass |
| WEB-C-068 | TripDetail.test.tsx · TripDetailPage: details | offers the next step for a Delayed trip: Mark boarding | Component rendering | Pass |
| WEB-C-069 | TripDetail.test.tsx · TripDetailPage: details | offers the next step for a Boarding trip: Mark dispatched | Component rendering | Pass |
| WEB-C-070 | TripDetail.test.tsx · TripDetailPage: details | offers the next step for a Dispatched trip: Mark completed | Component rendering | Pass |
| WEB-C-071 | TripDetail.test.tsx · TripDetailPage: details | offers no actions on a Completed trip and links back to the history | UI state / error state | Pass |
| WEB-C-072 | TripDetail.test.tsx · TripDetailPage: details | offers no actions on a Cancelled trip and links back to the history | UI state / error state | Pass |
| WEB-C-073 | TripDetail.test.tsx · TripDetailPage: details | shows a loading message and then 'Trip not found.' for an unknown trip | UI state / error state | Pass |
| WEB-C-074 | TripDetail.test.tsx · TripDetailPage: status changes and cancellation | sends PATCH /api/v1/trips/{id}/status with the next status when the dispatcher marks it | API integration | Pass |
| WEB-C-075 | TripDetail.test.tsx · TripDetailPage: status changes and cancellation | tells the dispatcher why the API rejected a status change | UI state / error state | Pass |
| WEB-C-076 | TripDetail.test.tsx · TripDetailPage: status changes and cancellation | requires a reason before the cancellation can be confirmed | Form validation | Pass |
| WEB-C-077 | TripDetail.test.tsx · TripDetailPage: status changes and cancellation | sends DELETE /api/v1/trips/{id} with the reason and opens the trip history | API integration | Pass |
| WEB-C-078 | TripDetail.test.tsx · TripDetailPage: status changes and cancellation | keeps the dispatcher on the trip and explains why when the cancellation is refused | UI state / error state | Pass |
| WEB-C-079 | TripForm.test.tsx · TripFormPage: create | shows the schedule form with the first route, direction, vehicle and driver already chosen | Component rendering | Pass |
| WEB-C-080 | TripForm.test.tsx · TripFormPage: create | only offers bays that are available | Component rendering | Pass |
| WEB-C-081 | TripForm.test.tsx · TripFormPage: create | does not save a trip without a service date and departure time, and tells the dispatcher | Form validation | Pass |
| WEB-C-082 | TripForm.test.tsx · TripFormPage: create | sends POST /api/v1/trips with the chosen date, time and resources, then opens the new trip | API integration | Pass |
| WEB-C-083 | TripForm.test.tsx · TripFormPage: edit | loads the existing trip and shows its notes | Component rendering | Pass |
| WEB-C-084 | TripForm.test.tsx · TripFormPage: edit | sends PUT /api/v1/trips/{id} with the changed notes and returns to the trip | API integration | Pass |
| WEB-C-085 | TripForm.test.tsx · TripFormPage: edit | shows every conflict the API reports (vehicle, driver, bay) when the trip clashes | Component rendering | Pass |
| WEB-C-086 | TripForm.test.tsx · TripFormPage: edit | shows the API message and stays on the form when the API refuses the change | Component rendering | Pass |
| WEB-C-087 | TripForm.test.tsx · TripFormPage: edit | shows a loading message, and 'Trip not found.' when the trip does not exist | UI state / error state | Pass |
| WEB-C-088 | TripHistory.test.tsx · TripHistoryPage | lists completed and cancelled trips, newest first, with passenger load | Component rendering | Pass |
| WEB-C-089 | TripHistory.test.tsx · TripHistoryPage | narrows the list by search and by status | Component rendering | Pass |
| WEB-C-090 | TripHistory.test.tsx · TripHistoryPage | shows ten trips per page and moves between pages | Component rendering | Pass |
| WEB-C-091 | TripHistory.test.tsx · TripHistoryPage | opens the trip when its row is clicked | API integration | Pass |
| WEB-C-092 | TripHistory.test.tsx · TripHistoryPage | shows an empty message and an error message | UI state / error state | Pass |

## Commands and results (2026-10-08)

| Command (run in `apps/web`) | Result |
|---|---|
| `pnpm test src/test/dispatch` | 92 passed, 0 failed |
| `pnpm test` (all web tests) | 206 passed (29 files) |
| `pnpm lint` | 0 errors, 20 warnings that were already in the project |
| `pnpm build` | passed |

### Coverage of this component's code (`apps/web/src/features/operations/**`, `fleet/DriversPage.tsx` and the driver hooks/services)

| Statements | Branches | Functions | Lines |
|---|---|---|---|
| 84.76% (423/499) | 73.79% (428/580) | 80.45% (177/220) | 85.1% (400/470) |

## Evidence files (`docs/testing/evidence/`)

- `web-dispatch-tests.log`: full list of tests with results.
- `web-dispatch-coverage.log` and `web-dispatch-coverage.png`: coverage output and a screenshot of the HTML report (`apps/web/coverage/dispatch/index.html` after running `pnpm test src/test/dispatch --coverage ...`).
- Defects and their before/after runs: see `docs/testing/web-dispatch-defects.md`.

## AI usage declaration (CLEAR)

### All tests and fixes for this component were written with AI
- **Tool:** Claude Code (model Claude Sonnet 5.5), in a chat session run by Chamal Senarathna on 2026-10-08, for his Component C.
- **Concise:** Chamal asked for his part of the web tests to be done after the other members' work was verified.
- **Logical:** The assistant read the Component C pages (dispatch board, trip form and detail, history, incidents, bays, drivers), listed the behaviours per category (rendering, validation, protected routes, API integration, UI states), wrote the tests file by file, and ran each file before moving on.
- **Explicit:** Rules: mock the network only with MSW; write only in `apps/web/src/test/dispatch/`; do not change the shared test helpers; change application code only when a failing test proves a bug, then show the failing and the passing run.
- **Adaptive:** Corrections made during the work: one test the assistant first wrote (a status-change rejection) asserted nothing and was rewritten to check the message shown to the user; selectors were adjusted for the UI library's pickers; error overrides were registered before default handlers; the cancel-error message was moved inside the confirmation dialog because the page behind it is hidden while it is open.
- **Reflective:** 8 tests failed before the fixes, each exposing a real defect (see `web-dispatch-defects.md`), and all 92 pass after. The full web suite (206 tests), lint (0 errors) and the production build were run at the end. **Chamal must read the tests and confirm he understands them before submission.**
