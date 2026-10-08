# Component A: Centres and Network: Web Test Cases

**Owner:** kishan-ahamed45 · **Test folder:** `apps/web/src/test/centres/` · **Last run:** 2026-10-08
**Application code covered:** `apps/web/src/features/centres/**` and `apps/web/src/features/network/**`

## Result

**36 of 36 tests pass** in 6 test files (`pnpm test src/test/centres`).

| Category (from the assignment) | Tests |
|---|---|
| Component rendering | 8 |
| Form validation | 2 |
| Protected route | 10 |
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
| WEB-A-001 | CentreFormPage.test.tsx · CentreFormPage | shows validation errors when required fields are empty | Form validation | Pass |
| WEB-A-002 | CentreFormPage.test.tsx · CentreFormPage | sends a POST request and navigates on successful creation | API integration | Pass |
| WEB-A-003 | CentreFormPage.test.tsx · CentreFormPage | displays an error message when the API returns a 409 Conflict (duplicate code) | UI state / error state | Pass |
| WEB-A-004 | CentreProfileAndAccess.test.tsx · CentreProfilePage | shows the centre details, bay count, manager and employee count | Component rendering | Pass |
| WEB-A-005 | CentreProfileAndAccess.test.tsx · CentreProfilePage | offers View operations only for an operating centre | Component rendering | Pass |
| WEB-A-006 | CentreProfileAndAccess.test.tsx · CentreProfilePage | links an operating centre to its operations | Component rendering | Pass |
| WEB-A-007 | CentreProfileAndAccess.test.tsx · CentreProfilePage | shows 'Unassigned' when the centre has no manager and a message when the centre cannot be loaded | UI state / error state | Pass |
| WEB-A-008 | CentreProfileAndAccess.test.tsx · CentreFormPage in edit mode | loads the existing centre into the form and locks the centre code | Component rendering | Pass |
| WEB-A-009 | CentreProfileAndAccess.test.tsx · CentreFormPage in edit mode | sends PUT /api/v1/centres/{id} with the changed name and returns to the profile | API integration | Pass |
| WEB-A-010 | CentreProfileAndAccess.test.tsx · CentreFormPage in edit mode | shows a message and stays on the form when saving fails | UI state / error state | Pass |
| WEB-A-011 | CentreProfileAndAccess.test.tsx · CentreFormPage in edit mode | shows a message when the centre to edit cannot be loaded | UI state / error state | Pass |
| WEB-A-012 | CentreProfileAndAccess.test.tsx · Centre pages: protected routes | shows the page to an Admin | Protected route | Pass |
| WEB-A-013 | CentreProfileAndAccess.test.tsx · Centre pages: protected routes | blocks a CentreManager with the Access Restricted message | Protected route | Pass |
| WEB-A-014 | CentreProfileAndAccess.test.tsx · Centre pages: protected routes | blocks a Dispatcher with the Access Restricted message | Protected route | Pass |
| WEB-A-015 | CentreProfileAndAccess.test.tsx · Centre pages: protected routes | blocks a FleetOfficer with the Access Restricted message | Protected route | Pass |
| WEB-A-016 | CentreProfileAndAccess.test.tsx · Centre pages: protected routes | blocks a Driver with the Access Restricted message | Protected route | Pass |
| WEB-A-017 | CentreProfileAndAccess.test.tsx · Centre pages: protected routes | blocks a Commuter with the Access Restricted message | Protected route | Pass |
| WEB-A-018 | CentreProfileAndAccess.test.tsx · Centre pages: protected routes | shows nothing to an anonymous visitor | Protected route | Pass |
| WEB-A-019 | CentreProfileAndAccess.test.tsx · Centre pages: protected routes | lets an Admin open /admin/centres in the real app | Protected route | Pass |
| WEB-A-020 | CentreProfileAndAccess.test.tsx · Centre pages: protected routes | does not give a commuter the centres admin page in the real app | Protected route | Pass |
| WEB-A-021 | CentresPage.test.tsx · CentresPage | displays the names and districts of centres from the API | Component rendering | Pass |
| WEB-A-022 | CentresPage.test.tsx · CentresPage | displays an empty message when no centres are found | UI state / error state | Pass |
| WEB-A-023 | CentresPage.test.tsx · CentresPage | displays an error message when the API returns a 500 error | UI state / error state | Pass |
| WEB-A-024 | CentresPage.test.tsx · CentresPage | blocks access for Commuters and displays an Access Restricted message | Protected route | Pass |
| WEB-A-025 | RoutesPage.test.tsx · RoutesPage | displays the route numbers and names from the API | Component rendering | Pass |
| WEB-A-026 | RoutesPage.test.tsx · RoutesPage | displays an empty message when no routes are found | UI state / error state | Pass |
| WEB-A-027 | RoutesPage.test.tsx · RoutesPage | displays an error message when the API returns a 500 error | UI state / error state | Pass |
| WEB-A-028 | RoutesPage.test.tsx · RouteDetailPage | displays the route details and status | Component rendering | Pass |
| WEB-A-029 | RoutesPage.test.tsx · RouteFormPage | prevents form submission if required fields are missing | Form validation | Pass |
| WEB-A-030 | TimetableActions.test.tsx · TimetablesPage actions | lists the recurring timetables of the selected route and direction | Component rendering | Pass |
| WEB-A-031 | TimetableActions.test.tsx · TimetablesPage actions | sends POST generate-trips with the service date and shows the generation summary | API integration | Pass |
| WEB-A-032 | TimetableActions.test.tsx · TimetablesPage actions | tells the user when trips cannot be generated and keeps the dialog open | API integration | Pass |
| WEB-A-033 | TimetableActions.test.tsx · TimetablesPage actions | sends DELETE for a timetable when it is deactivated | API integration | Pass |
| WEB-A-034 | TimetableActions.test.tsx · TimetablesPage actions | shows the timetable load error when the schedules request fails | UI state / error state | Pass |
| WEB-A-035 | TimetablesPage.test.tsx · TimetablesPage | displays an empty message when no routes are configured for timetables | UI state / error state | Pass |
| WEB-A-036 | TimetablesPage.test.tsx · TimetablesPage | shows the route list failure as the default prompt because the page does not report the error | UI state / error state | Pass |

## Commands and results (2026-10-08)

| Command (run in `apps/web`) | Result |
|---|---|
| `pnpm test src/test/centres` | 36 passed, 0 failed |
| `pnpm test` (all web tests) | 206 passed (29 files) |
| `pnpm lint` | 0 errors, 20 warnings that were already in the project |
| `pnpm build` | passed |

### Coverage of this component's code (`apps/web/src/features/centres/**` and `apps/web/src/features/network/**`)

| Statements | Branches | Functions | Lines |
|---|---|---|---|
| 45.19% (235/520) | 43.56% (247/567) | 34.2% (92/269) | 47.13% (230/488) |

## Evidence files (`docs/testing/evidence/`)

- `web-centres-tests.log`: full list of tests with results.
- `web-centres-coverage.log` and `web-centres-coverage.png`: coverage output and a screenshot of the HTML report (`apps/web/coverage/centres/index.html` after running `pnpm test src/test/centres --coverage ...`).
- Defects and their before/after runs: see `docs/testing/web-centres-defects.md`.

## AI usage declaration (CLEAR)

### Tests written by the member (14 tests, merged in PR #17)
Written by kishan-ahamed45. **Whether he used AI for these is not recorded here. He must add his own declaration below.**

### Tests and fixes added afterwards with AI (22 tests)
- **Tool:** Claude Code (model Claude Sonnet 5.5), in a chat session run by Chamal Senarathna on 2026-10-08.
- **Concise:** Chamal's instruction was short: finish the remaining items from the review of the merged work (no need to ask the member).
- **Logical:** The assistant compared the merged tests with the issue checklist (five categories, at least 14 tests, evidence files), listed the gaps (only 1 protected-route test, only 1 API-integration test, no centre profile, edit or timetable tests, no evidence), then wrote tests category by category and ran the suite after each file.
- **Explicit:** Rules given to the assistant: mock the network only with MSW, write only inside `apps/web/src/test/centres/`, do not change the shared test helpers, follow the project style, and change application code only when a failing test proves a bug.
- **Adaptive:** Two mistakes were corrected during the work: the links on the centre profile are rendered as buttons by the UI library (queries were changed), and a default success handler was registered ahead of an error override so MSW used the wrong one (the override order was fixed). A misleading test in `TimetablesPage.test.tsx` (named "generation API fails" but only checking the default prompt) was replaced.
- **Reflective:** Every new test was run, then the whole web suite, lint and build. The tests were not changed to hide problems: two real behaviours found are recorded as open defects (`web-centres-defects.md`). **The member must read these tests, confirm he understands them, and add his own account of AI use.**
