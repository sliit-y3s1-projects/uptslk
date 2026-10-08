# Component D Web Defects

## Fare-rule validation was blocked by native browser constraints

- **Severity:** Medium
- **Observed:** Submitting a create fare rule with no route or a zero amount did not reach the component's custom validation handler, so the expected messages were not rendered.
- **Cause:** Native `required` and `min` constraints prevented the submit event before React validation ran.
- **Fix:** Removed the native constraints that blocked custom validation and kept route/amount validation in the submit handler. Numeric amount validation runs before route validation.
- **Evidence:** `src/test/bookings/AccessAndFareRules.test.tsx` now covers both invalid cases; the booking suite passes.
