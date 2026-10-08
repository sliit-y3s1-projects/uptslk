# Component B Web Defects

## WEB-B-1: The vehicle form accepted a capacity above 200 (Medium, Fixed)

- **Where:** `features/fleet/VehiclesPage.tsx`, `VehicleFormInner`.
- **Steps:** Register a vehicle with a passenger capacity of 250.
- **Expected:** The form rejects it (the API allows only 1 to 200).
- **Actual (before the fix):** The form sent the request and the API answered `400`, so the user saw a server error instead of a clear message.
- **Fix:** Added a check that shows "Passenger capacity must be 200 or fewer." and sends no request (commit `caaa311` by Rashmik0119). The indentation of the new lines was corrected afterwards.
- **Evidence:** `docs/testing/evidence/web-fleet-defect-capacity-before-fix.log` (1 failed, 7 passed) and `web-fleet-defect-capacity-after-fix.log` (8 passed). Test: "VehicleFormPage rejects a passenger capacity above 200 and does not send a POST request".
