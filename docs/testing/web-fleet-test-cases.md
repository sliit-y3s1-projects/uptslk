# Component B: Fleet and Maintenance: Web Test Cases

**Owner:** Rashmik0119 · **Test folder:** `apps/web/src/test/fleet/` · **Last run:** 2026-10-08
**Application code covered:** `apps/web/src/features/fleet/**`

## Result

**29 of 29 tests pass** in 5 test files (`pnpm test src/test/fleet`).

| Category (from the assignment) | Tests |
|---|---|
| Component rendering | 5 |
| Form validation | 8 |
| Protected route | 4 |
| API integration | 8 |
| UI state / error state | 4 |

Categories are assigned from the wording of each test name; a test can touch more than one area.

## How every test case works

- **Precondition:** the page is rendered inside the app's providers (router, React Query, auth context) as the stated role (or anonymous). Every API call is answered by a mock (MSW). A request without a mock fails the test, so no real server is used.
- **Steps:** render the page or route, then act as a user would (type, click, choose from a list) where the test name says so.
- **Expected result:** the behaviour stated in the test name (what is shown, which request is sent with which method, URL and body, or that no request is sent).
- **Actual result and Pass/Fail:** the automated assertions decide; the Result column shows the outcome of the last run.

## Test cases

| ID | Feature (file) | Test case: what is checked and the expected behaviour | Category | Result |
|---|---|---|---|---|
| WEB-B-001 | FleetRoutes.test.tsx · Fleet protected routes (RequireRole) | RequireRole allows FleetOfficer, CentreManager, and Admin to view VehiclesPage and MaintenancePage | Protected route | Pass |
| WEB-B-002 | FleetRoutes.test.tsx · Fleet protected routes (RequireRole) | RequireRole blocks a Commuter and a Driver from /fleet/vehicles and /fleet/maintenance with the Access Restricted message | Protected route | Pass |
| WEB-B-003 | FleetRoutes.test.tsx · Fleet protected routes (RequireRole) | RequireRole renders nothing for an anonymous visitor (user: null) on /fleet/vehicles | Protected route | Pass |
| WEB-B-004 | MaintenanceForm.test.tsx · MaintenanceFormPage | MaintenanceFormPage shows 'Please select a vehicle.' when submitted without a selected vehicle and sends no POST request | Form validation | Pass |
| WEB-B-005 | MaintenanceForm.test.tsx · MaintenanceFormPage | MaintenanceFormPage shows validation errors when maintenance type or description is whitespace and sends no POST request | Form validation | Pass |
| WEB-B-006 | MaintenanceForm.test.tsx · MaintenanceFormPage | MaintenanceFormPage in edit mode requires a completion time when status is set to Completed and sends no PUT request | Form validation | Pass |
| WEB-B-007 | MaintenanceForm.test.tsx · MaintenanceFormPage | MaintenanceFormPage sends POST /api/v1/maintenance-records with the selected vehicle, type, description, and ISO timestamp, then navigates to the detail page | API integration | Pass |
| WEB-B-008 | MaintenanceForm.test.tsx · MaintenanceFormPage | MaintenanceFormPage in edit mode sends PUT /api/v1/maintenance-records/:recordId with Completed status and completedAt timestamp | API integration | Pass |
| WEB-B-009 | MaintenanceForm.test.tsx · MaintenanceFormPage | MaintenanceFormPage displays the 409 Conflict message when POST /api/v1/maintenance-records returns a trip clash with conflictingTripIds and disables buttons while submitting | API integration | Pass |
| WEB-B-010 | MaintenancePage.test.tsx · MaintenancePage and MaintenanceDetailPage | MaintenancePage renders scheduled and completed maintenance records and filters them by the search box | Component rendering | Pass |
| WEB-B-011 | MaintenancePage.test.tsx · MaintenancePage and MaintenanceDetailPage | MaintenanceDetailPage renders the maintenance record reference, vehicle details, schedule timestamps, and description | Component rendering | Pass |
| WEB-B-012 | MaintenancePage.test.tsx · MaintenancePage and MaintenanceDetailPage | MaintenanceDetailPage sends DELETE /api/v1/maintenance-records/:recordId when cancellation is confirmed and navigates back to /fleet/maintenance | API integration | Pass |
| WEB-B-013 | MaintenancePage.test.tsx · MaintenancePage and MaintenanceDetailPage | MaintenancePage shows an empty state when no records exist and an error banner when GET /api/v1/maintenance-records returns 500 | UI state / error state | Pass |
| WEB-B-014 | VehicleForm.test.tsx · VehicleFormPage | VehicleFormPage shows an error when registration number or model is blank and does not send a POST request | Form validation | Pass |
| WEB-B-015 | VehicleForm.test.tsx · VehicleFormPage | VehicleFormPage rejects a zero or negative passenger capacity and does not send a POST request | Form validation | Pass |
| WEB-B-016 | VehicleForm.test.tsx · VehicleFormPage | VehicleFormPage rejects a passenger capacity above 200 and does not send a POST request | Form validation | Pass |
| WEB-B-017 | VehicleForm.test.tsx · VehicleFormPage | VehicleFormPage rejects an unsupported image file type with 'Choose a JPG, PNG, or WebP vehicle image.' | Form validation | Pass |
| WEB-B-018 | VehicleForm.test.tsx · VehicleFormPage | VehicleFormPage rejects an image file over 5 MB and sends no image upload | Form validation | Pass |
| WEB-B-019 | VehicleForm.test.tsx · VehicleFormPage | VehicleFormPage sends a normalized uppercase plate number in POST /api/v1/vehicles, uploads the selected image, and navigates to the new vehicle profile | API integration | Pass |
| WEB-B-020 | VehicleForm.test.tsx · VehicleFormPage | VehicleFormPage in edit mode loads existing data, sends PUT /api/v1/vehicles/:vehicleId with updated fields, and navigates back to the profile | API integration | Pass |
| WEB-B-021 | VehicleForm.test.tsx · VehicleFormPage | VehicleFormPage displays the 409 Conflict message (A vehicle with this plate number already exists.) and disables the submit button while saving | UI state / error state | Pass |
| WEB-B-022 | VehiclesPage.test.tsx · VehiclesPage and VehicleProfilePage | VehiclesPage renders the list of vehicles with plate number, model, capacity, accessibility, centre, and status from the API | Component rendering | Pass |
| WEB-B-023 | VehiclesPage.test.tsx · VehiclesPage and VehicleProfilePage | VehiclesPage narrows the list when searching by registration or model | Component rendering | Pass |
| WEB-B-024 | VehiclesPage.test.tsx · VehiclesPage and VehicleProfilePage | VehiclesPage hides the Register vehicle action when rendered in read-only mode | Protected route | Pass |
| WEB-B-025 | VehiclesPage.test.tsx · VehiclesPage and VehicleProfilePage | VehicleProfilePage renders the vehicle details, assigned centre code, and maintenance history from the API | Component rendering | Pass |
| WEB-B-026 | VehiclesPage.test.tsx · VehiclesPage and VehicleProfilePage | VehicleProfilePage sends DELETE /api/v1/vehicles/:vehicleId when deactivation is confirmed and returns to the vehicle list | API integration | Pass |
| WEB-B-027 | VehiclesPage.test.tsx · VehiclesPage and VehicleProfilePage | VehiclesPage shows a loading indicator while fetching and an empty-state message when the centre has no vehicles | UI state / error state | Pass |
| WEB-B-028 | VehiclesPage.test.tsx · VehiclesPage and VehicleProfilePage | VehiclesPage shows the error banner when GET /api/v1/vehicles fails with 500 and reloads when Retry is clicked | UI state / error state | Pass |
| WEB-B-029 | VehiclesPage.test.tsx · VehiclesPage and VehicleProfilePage | VehicleProfilePage displays a 403 Forbidden error message when a FleetOfficer tries to deactivate another centre's vehicle | API integration | Pass |

## Commands and results (2026-10-08)

| Command (run in `apps/web`) | Result |
|---|---|
| `pnpm test src/test/fleet` | 29 passed, 0 failed |
| `pnpm test` (all web tests) | 206 passed (29 files) |
| `pnpm lint` | 0 errors, 20 warnings that were already in the project |
| `pnpm build` | passed |

### Coverage of this component's code (`apps/web/src/features/fleet/**`)

| Statements | Branches | Functions | Lines |
|---|---|---|---|
| 63.34% (299/472) | 54.86% (282/514) | 62.58% (92/147) | 65.01% (288/443) |

## Evidence files (`docs/testing/evidence/`)

- `web-fleet-tests.log`: full list of tests with results.
- `web-fleet-coverage.log` and `web-fleet-coverage.png`: coverage output and a screenshot of the HTML report (`apps/web/coverage/fleet/index.html` after running `pnpm test src/test/fleet --coverage ...`).
- Defects and their before/after runs: see `docs/testing/web-fleet-defects.md`.

## AI usage declaration (CLEAR)

### Tests written by the member (29 tests, merged in PR #18)
Written by Rashmik0119. **Whether she used AI for these is not recorded here. She must add her own declaration below.**

### Changes added afterwards with AI
- **Tool:** Claude Code (model Claude Sonnet 5.5), in a chat session run by Chamal Senarathna on 2026-10-08.
- **What was done:** no tests were added or changed. The assistant only generated the evidence files (test log, coverage log and screenshot), fixed the indentation of the capacity check in `VehiclesPage.tsx` (the logic is unchanged and is Rashmi's), recorded her defect, and produced before/after evidence for it by temporarily removing the check and restoring it.
- **Concise / Logical / Explicit / Adaptive / Reflective:** the work was a short, scoped instruction (finish the missing evidence), carried out as a review against the issue checklist. The only adaptation was reading the existing tests to confirm they already cover all five categories. The before/after run was used to prove the defect and the test are real.
- **The member must read this document and add her own account of AI use.**
