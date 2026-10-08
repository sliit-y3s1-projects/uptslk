# Component D Web Defects

## WEB-D-1: The fare rule form sent a request without a route (Medium, Fixed)

- **Where:** `features/fares/components/FareRules.tsx`, `FareForm`.
- **Steps:** Open "Create fare rule", type an amount, leave the route unselected, press "Save fare rule".
- **Expected:** The form says a route must be selected and sends nothing.
- **Actual (before the fix):** The form sent `POST /api/v1/fare-rules` with an empty `routeId`. The API rejects it, so the user saw a failed save and no explanation. The existing test named "rejects a fare rule without a selected route" asserted the opposite of its title (`expect(postSpy).toHaveBeenCalledWith()`), so it hid the bug.
- **Fix:** The submit handler now checks `routeId` and shows "Select an active route." through the existing error alert; the test now asserts that no request is sent and the message is shown.
- **Evidence:** `docs/testing/evidence/web-bookings-defect-fare-route-before-fix.log` (1 failed, 4 passed) and `web-bookings-defect-fare-route-after-fix.log` (5 passed).

## Correction to the earlier version of this file

The earlier note said native `required` and `min` constraints were removed and custom validation was added. That change was **not** in the merged code: the amount field still uses the native `required`/`min` constraints (the amount test checks `toBeInvalid()`), and the route check did not exist until WEB-D-1 above was fixed.

## Scope gap closed

The earlier test-case document said the commuter pages (`MyTicketsPage`, `ticketGroup`, payment return) were not present in the checkout. They are on `main`, so `src/test/bookings/CommuterTickets.test.tsx` (22 tests) now covers them: ticket grouping rules, My tickets, the payment status page and commuter access.
