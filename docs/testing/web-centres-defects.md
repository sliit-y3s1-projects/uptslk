# Component A Web Defects

Found while reviewing and extending the web tests for Centres and Network (2026-10-08). Both are small user-experience problems. They are **open**: they were recorded, not fixed, because the correct message wording is a product decision for the component owner.

## WEB-A-1: A duplicate centre code is reported with a generic message (Low, Open)

- **Where:** `features/centres/CentresPage.tsx`, `CentreFormPage`.
- **Steps:** Create a centre with a code that already exists. The API answers `409` with `{ "error": "A centre with this code already exists." }`.
- **Expected:** The user is told the code already exists.
- **Actual:** The form always shows "Failed to save centre. Please try again.", so the user cannot tell what to change.
- **Evidence:** `src/test/centres/CentreFormPage.test.tsx`, test "displays an error message when the API returns a 409 Conflict (duplicate code)" documents the current message.
- **Suggested fix:** show `createMutation.error.message` (the shared API client already extracts it).

## WEB-A-2: The timetables page does not report that the route list failed to load (Low, Open)

- **Where:** `features/network/TimetablesPage.tsx`.
- **Steps:** Open `/network/timetables` while `GET /api/v1/routes` fails (HTTP 500).
- **Expected:** An error message.
- **Actual:** The page shows the normal prompt "Choose a route to manage its recurring timetables." and nothing tells the user the list could not be loaded.
- **Evidence:** `src/test/centres/TimetablesPage.test.tsx`, test "shows the route list failure as the default prompt because the page does not report the error".
- **Suggested fix:** show an alert when the routes query has an error.

## Review findings fixed in the tests (not application defects)

- The merged test named "shows an error message if the timetable generation API fails" did not test generation at all (it only mocked a failing routes request and asserted the default prompt). It was replaced by real generation tests in `TimetableActions.test.tsx` (success summary, failure message, deactivate, schedule load error).
