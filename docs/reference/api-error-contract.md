# API error handling contract

**Audience:** client developers and API reviewers.

**Baseline:** `main` commit `c2e792bfb0ec21db632e1f85815d7c1380e8a432`, reviewed 2026-10-16. This reference describes existing behavior; it does not add response fields or change error handling.

## Service errors

Services throw typed exceptions. [GlobalExceptionHandler](../../apps/backend/api/Common/Middleware/GlobalExceptionHandler.cs) converts an [AppException](../../apps/backend/api/Exceptions/AppException.cs) into ProblemDetails with the exception's status and a caller-visible message in both `detail` and `error`.

| Exception | HTTP status | Client interpretation |
| --- | --- | --- |
| BadRequestException | 400 | Input or business precondition failed; inspect the message. |
| UnauthorizedException | 401 | Authentication is missing or invalid. |
| ForbiddenException | 403 | The caller lacks permission for the operation. |
| NotFoundException | 404 | The requested record was not found by the service. |
| ConflictException | 409 | Current state conflicts with the requested operation. |
| BadGatewayException | 502 | A service-reported upstream failure, such as refund rejection. |
| ServiceUnavailableException | 503 | A required service/configuration is unavailable. |

For example, the service-generated body for a missing booking includes the following fields. Framework-added fields are omitted here rather than prescribed:

```json
{"status":404,"detail":"Booking not found.","error":"Booking not found."}
```

A BadRequestException can also supply an `errors` list. `AppException.Details` adds operation-specific fields to the response. There is no universal application error-code catalogue: a client may read `code` when present, but cannot assume every response supplies it.

Unexpected exceptions are logged server-side and become 500 with `An unexpected error occurred.` The handler does not expose their original exception messages to the caller.

## Errors outside the service handler

Do not assume every non-2xx response has this exact JSON shape. Automatic controller/model validation can return an `errors` dictionary keyed by field. Authentication/authorization middleware can reject a request before a service runs. Rate limiting, a proxy, or an unavailable listener can produce another response or no HTTP response at all.

Preserve the status separately from the display text. A failed network request is not an HTTP 500, and a 404 quote response is not by itself proof that an endpoint is absent: a service can return Not Found for missing business data.

## Existing web client behavior

The shared [api-client](../../apps/web/src/lib/api/api-client.ts) sends credentials and, on most 401 responses, attempts refresh once and retries the original request if refresh succeeds. Login and refresh paths are excluded from that automatic refresh attempt.

For a JSON error, message priority is `error`, combined `errors`, `detail`, `title`, then `Request failed`. Parsed API errors retain HTTP status and optional code in ApiError. A 204 response returns no value. Malformed/non-JSON error bodies can escape as parsing errors or plain Error objects, so consumers cannot assume every caught error is ApiError.

## Verification checklist

Exercise a domain 400, unknown-record 404, capacity 409, anonymous 401, wrong-role 403, and upstream failure in a controlled harness. Check both HTTP status and visible feedback. Include a field-validation response, empty/non-JSON failure, and network failure when reviewing client error presentation. Inspect state after failed mutations before retrying money-moving operations.

Use synthetic data and omit cookies/tokens from evidence. Existing response assertions are in [BookingApiTests](../../apps/backend/api.Tests/Bookings/BookingApiTests.cs), [PassengerAccessControlTests](../../apps/backend/api.Tests/Bookings/PassengerAccessControlTests.cs), and [PaymentRefundTests](../../apps/backend/api.Tests/Bookings/PaymentRefundTests.cs). This checklist is a review procedure, not a record that every case was newly executed.
