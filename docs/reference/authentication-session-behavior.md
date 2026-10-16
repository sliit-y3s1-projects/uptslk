# Authentication and session behavior

**Audience:** web/mobile integrators and authentication reviewers.

**Baseline:** `main` commit `c2e792bfb0ec21db632e1f85815d7c1380e8a432`, reviewed 2026-10-16. The scope is the implemented session flow, not a security certification.

## Sign-in and caller identity

[AuthController](../../apps/backend/api/Controllers/AuthController.cs) exposes register, login, refresh, logout, and authenticated `me` routes under `/api/v1/auth`. Register creates a Commuter account, linked passenger profile, and empty wallet. If saving that passenger fails with a database update exception, the service clears tracked changes and deletes the newly created account before returning an error.

[AuthService](../../apps/backend/api/Services/AuthService.cs) returns a JWT in the login/register response and sets sign-in cookies. Unknown email and wrong password return Unauthorized with the same `Invalid credentials` message. A disabled account encountered during login returns Forbidden before the password check. Do not describe all failed logins as indistinguishable.

[JwtTokenService](../../apps/backend/api/Services/JwtTokenService.cs) includes user ID, email, name, role, and an optional centre ID in signed tokens. Its default token lifetime is eight hours. Issuer, audience, signature, and lifetime are validated by the configured JWT authentication handler.

## Web cookies and mobile tokens

| Mechanism | Implemented behavior |
| --- | --- |
| Access cookie | `upts_access_token`, HTTP-only, path `/`, eight-hour expiry. |
| Refresh cookie | `upts_refresh_token`, HTTP-only, path `/`, 30-day expiry. |
| Local development HTTP | Cookies use SameSite Lax without Secure. |
| HTTPS or a non-Development environment | Cookies use SameSite None with Secure. |
| Browser API requests | Shared web client sends `credentials: include`. |
| Mobile API requests | Clients can send the returned token as a Bearer credential. |
| Notification hub | Authentication callback accepts `access_token` in the query on `/hubs/notifications`; it also supports the access cookie fallback. |

Sources: [AuthCookieService](../../apps/backend/api/Services/AuthCookieService.cs), [AuthCookies](../../apps/backend/api/Common/Constants/AuthCookies.cs), [authentication setup](../../apps/backend/api/Common/Extensions/AuthServiceExtensions.cs), and [mobile booking client](../../apps/mobile/lib/services/booking_api_service.dart).

## Refresh and logout boundaries

Refresh reads the refresh cookie, validates its JWT, looks up the user, rejects a missing/inactive user, and issues new cookies. It returns 204 rather than a token response body. The shared [web client](../../apps/web/src/lib/api/api-client.ts) tries this flow once after most 401 responses, then retries the original request if refresh succeeded; login and refresh requests are excluded.

Logout clears the two browser cookies and returns 204. The reviewed implementation does not maintain a server-side token revocation list or refresh-token session table. Cookie deletion does not revoke a previously copied Bearer token. Likewise, rejecting a disabled user at login/refresh does not establish immediate rejection of every previously issued access token: the JWT handler itself validates token claims and signature, not the current account row on every request.

Role/centre claims are snapshots at issuance. Reissuing cookies through refresh uses the current user record. Do not promise immediate claim changes in an already-held mobile token or an existing hub connection.

## Troubleshooting and review

For a browser session problem, check actual web/API origins, cookie attributes, HTTPS/environment selection, request credentials, and refresh status. For mobile, distinguish a missing/expired Bearer token from a web cookie issue; the reviewed refresh endpoint depends on a cookie and is not a mobile refresh-token exchange.

Record status and redacted timing evidence rather than JWTs, cookies, or credentials. Existing [AuthenticationTests](../../apps/backend/api.Tests/Auth/AuthenticationTests.cs) cover real JWT login, access cookies, tampering/expiry, roles, refresh, disabled-account refresh, and logout. They do not by themselves validate a deployed browser's cross-site cookie policy or mobile session persistence.
