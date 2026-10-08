# Component C Web Defects

Found by the Component C web tests on 2026-10-08. All seven are fixed. Evidence: `docs/testing/evidence/web-dispatch-defects-before-fix.log` (92 tests: 8 failed, 84 passed) and `web-dispatch-defects-after-fix.log` (92 passed).

## WEB-C-1: Trip conflict reasons were shown as "Request failed" (Medium, Fixed)

- **Where:** shared `lib/api/api-client.ts`, seen on the trip form.
- **Steps:** Edit or schedule a trip that clashes with another (same vehicle, driver or bay). The API answers `400` with `{ "errors": ["Vehicle is already assigned to route 101 at this time.", ...] }`.
- **Expected:** The dispatcher reads every reason.
- **Actual:** The client only read `error`, `detail` or `title`, so the form showed "Request failed" and the reasons were lost. This hid the main purpose of conflict detection.
- **Fix:** The client now reads an `errors` list (and the ASP.NET `{ field: [..] }` shape). It is shared code, so every page benefits; all 206 web tests still pass.
- **Test:** `TripForm.test.tsx`, "shows every conflict the API reports (vehicle, driver, bay) when the trip clashes".

## WEB-C-2: Saving a trip without a date or time crashed instead of showing a message (Medium, Fixed)

- **Where:** `features/operations/TripFormPage.tsx`.
- **Steps:** Open "Schedule trip" and press the button without choosing a service date and departure time.
- **Expected:** "Select a service date and a departure time." and no request.
- **Actual:** The code built an invalid date and threw `RangeError: Invalid time value`; the user saw nothing.
- **Fix:** The submit handler checks both values first.
- **Test:** `TripForm.test.tsx`, "does not save a trip without a service date and departure time, and tells the dispatcher".

## WEB-C-3: The dispatch board hid failed status changes (Medium, Fixed)

- **Where:** `features/operations/DispatchPage.tsx` (Start boarding, Flag delay, Dispatch bus).
- **Actual:** When the API refused a change (for example `400 Cannot change a Ready trip to Boarding.`) nothing was shown.
- **Fix:** The API message is shown in an alert above the board.
- **Test:** `DispatchBoard.test.tsx`, "tells the dispatcher why the API rejected a status change".

## WEB-C-4: The trip page hid failed status changes and failed cancellations (Medium, Fixed)

- **Where:** `features/operations/TripDetailPage.tsx`.
- **Actual:** A refused status change or cancellation showed nothing. For cancellation the confirmation dialog stays open and hides the page behind it, so the message had to be placed inside the dialog.
- **Fix:** Status errors show in an alert on the page; cancellation errors show inside the dialog.
- **Tests:** `TripDetail.test.tsx`, "tells the dispatcher why the API rejected a status change" and "keeps the dispatcher on the trip and explains why when the cancellation is refused".

## WEB-C-5: Failed incident reports were silent (Medium, Fixed)

- **Where:** `features/operations/IncidentFormPage.tsx` (`onError` did nothing).
- **Actual:** If the API refused the report (for example a trip of another centre) the form stayed as it was with no message, so the dispatcher could think it was saved.
- **Fix:** The API message is shown above the buttons.
- **Test:** `Incidents.test.tsx`, "tells the dispatcher when the incident cannot be saved and stays on the form".

## WEB-C-6: Failed incident updates were silent (Low, Fixed)

- **Where:** `features/operations/IncidentsPage.tsx` (Begin investigation / Resolve incident).
- **Fix:** The API message is shown in an alert above the lists.
- **Test:** `Incidents.test.tsx`, "tells the dispatcher when an incident update is rejected".

## WEB-C-7: Closing or releasing a bay failed silently (Medium, Fixed)

- **Where:** `features/operations/BayManagementPage.tsx` (Close bay, Release bay, Mark occupied). Create and Edit already showed errors.
- **Actual:** A refusal such as `403 You can only change bays of your own centre.` showed nothing.
- **Fix:** The API message is shown in an alert above the summary.
- **Test:** `BayManagement.test.tsx`, "tells the dispatcher when closing a bay is refused".
