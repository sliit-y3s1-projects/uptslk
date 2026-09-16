# Identity and Access Management (IAM)

This document is the IAM plan for UPTSLK. It separates the public commuter identity experience from the operations console while keeping one account and one authorization model. The goal is a predictable identity lifecycle: register, verify, sign in, access the right scope, manage credentials, and eventually deactivate or restore accounts with an audit trail.

## Current state

| Area | Current implementation | Status | Gap to close |
| --- | --- | :---: | --- |
| Authentication | Local ASP.NET Identity users with JWT login, HttpOnly access/refresh cookies, and `/auth/me` | ⚠️ PARTIAL | Add refresh rotation/revocation and production cookie policy |
| Registration | Public commuter registration creates a Commuter account and signs the user in | ✅ DONE | Add email verification, consent, rate limits, and duplicate-account UX |
| Admin bootstrap | Configured bootstrap SuperAdmin is ensured on API startup | ✅ DONE | Move bootstrap credentials to a one-time provisioning flow |
| Admin sign-in | Shared sign-in form with role-based redirect | ✅ DONE | Provide consistent post-login routing and session-expiry handling |
| Public commuter sign-in | Same auth service as staff accounts | ⚠️ PARTIAL | Remove confusing separate login/sign-in paths and preserve SPA navigation |
| RBAC | Roles are carried in JWT and checked by `RequireRole` | ⚠️ PARTIAL | Centralize permissions and enforce them in API policies, not only UI guards |
| Centre scope | Users can carry an optional `centreId` | ⚠️ PARTIAL | Enforce centre scope server-side on every centre-owned resource |
| Employee management | Super Admin can create centre-assigned accounts | ✅ DONE | Add edit, disable, unlock, reset, and invitation lifecycle |
| User directory | Employee list exists | ❌ NOT DONE | Add all-user directory covering commuters, staff, admins, and disabled users |
| Password reset | Authenticated change-password and Admin reset endpoints are available | ⚠️ PARTIAL | Add public request flow, expiring email token, reset form, session revocation, and audit event |
| Profile | Profile UI supports local editing/photo preview and identity fields; display-name update API is available | ⚠️ PARTIAL | Wire the UI mutation and persist photo, NIC, location, and gender through secured APIs |
| Identity verification | NIC fields are presented but not verified | ❌ NOT DONE | Add validation, verification state, reviewer workflow, and privacy controls |
| Account status | `IsActive` is persisted, included in user listings, enforced at login, and manageable by Admin | ✅ DONE | Expand to a full Pending/Suspended/Disabled/Locked state machine |
| Booking authorization | Selecting a departure always asks the user to sign in | ❌ NOT DONE | Recognize the current session and continue directly to seat selection |
| Navigation | Some actions still use hard redirects or legacy `/book` links | ⚠️ PARTIAL | Use React Router links/actions and make `/` the canonical booking route |
| Auditability | Operational audit views exist; IAM events are incomplete | ❌ NOT DONE | Record login, logout, reset, role, status, scope, and verification events |

## Target identity lifecycle

| Stage | User experience | Backend requirement |
| --- | --- | --- |
| Register | Commuter creates an account with name, email, password, NIC, location, and optional gender | Validate input, hash password, normalize email, throttle attempts, and create `PendingVerification` user |
| Verify | User confirms email and, where required, NIC identity | One-time expiring tokens; store verification timestamps and verification outcome |
| Sign in | One sign-in entry point serves commuters and staff; destination depends on role | JWT plus refresh/session record, lockout policy, and role/centre claims |
| Onboard | User completes profile and preferences before booking | Persist profile data; never trust client-provided role or centre scope |
| Book | Signed-in user selects departure, seats, and payment without another sign-in prompt | Use current authenticated user; enforce ownership and seat-conflict rules |
| Maintain | User can update profile, photo, location, NIC details, and password | Authenticated PATCH endpoints, validation, re-authentication for sensitive changes |
| Recover | User requests a password reset and receives a time-limited link | Hashed reset token, expiry, single use, session revocation, and audit event |
| Administer | Super Admin searches every account, changes status/role/scope, and resets access | Policy-protected user-management API with reason capture and audit trail |
| Suspend/disable | Admin bans or disables an account; existing sessions stop working | Revocation/version check on tokens and clear status transition rules |
| Restore/delete | Admin restores eligible accounts; personal data is retained or anonymized according to policy | Soft disable by default, retention policy, and irreversible-action confirmation |

## Required authorization model

Use roles for broad responsibilities and permissions for individual capabilities. The API must be authoritative; UI checks are only a usability layer.

| Role | Typical scope | Example permissions |
| --- | --- | --- |
| SuperAdmin | Organization-wide | Manage users, roles, centres, policies, audit, and platform settings |
| CentreManager | Assigned centre | Manage centre staff, bays, routes, and centre operations |
| Dispatcher | Assigned centre | Create/assign trips, update lifecycle, and resolve dispatch incidents |
| FleetOfficer | Assigned centre | Manage vehicles, drivers, maintenance, and compliance |
| Driver | Assigned centre | View assigned trips and report operational incidents |
| Commuter | Own account | Search, reserve, pay, view tickets, and manage own profile |

Every protected API request must verify: authenticated user, account status, required permission, centre scope (when applicable), and resource ownership (for commuter data).

## Delivery order

1. Fix routing/session UX: one `/login`, canonical `/`, session-aware booking continuation, and consistent unauthorized/not-found handling.
2. Add account status and session revocation primitives.
3. Implement password reset and change-password APIs/UI.
4. Build the Super Admin all-user directory with search, filters, details, disable/restore, unlock, and reset actions.
5. Persist commuter profile, photo metadata, location, NIC verification state, and gender; add privacy-safe validation.
6. Replace role-only UI checks with named permission policies and centre-scope handlers across every API controller.
7. Add audit events and security controls: rate limiting, lockout, token expiry/rotation, and sensitive-action confirmation.
8. Add API and browser tests for each lifecycle transition, including disabled users, expired reset links, cross-centre access, and booking continuation.

## Non-goals for the first IAM iteration

- No external IdP or social login integration yet.
- No automatic email delivery requirement; local development may expose reset links through a development channel.
- No hard deletion of users until retention and audit requirements are agreed.

## Implementation notes

- ✅ Authenticated password changes are now available through `POST /api/v1/auth/change-password`.
- ✅ Authenticated display-name updates are available through `PATCH /api/v1/auth/me`.
- ✅ Profile API now accepts home location, NIC number, gender, and profile photo metadata through the same endpoint.
- ✅ Profile UI derives a suggested gender from valid Sri Lankan NIC date digits while editing.
- ⚠️ Apply an EF migration for the new profile columns before using these fields against an existing database.
- ✅ Login and registration now issue HttpOnly access and refresh cookies; the API authenticates from the access cookie and logout clears both cookies.
- ✅ Added `POST /api/v1/auth/refresh` with refresh-cookie validation and access/refresh cookie rotation; the frontend retries one failed API request after refresh.
- ✅ Admin user status management is available through `PATCH /api/v1/auth/users/{id}/status`.
- ✅ Admin password reset is available through `POST /api/v1/auth/users/{id}/reset-password` with a new password string.
- ⚠️ The new `User.IsActive` field requires an EF Core migration before status changes can be used against an existing database.
- ✅ Commuter registration now creates a linked passenger profile and wallet; profile name and email changes stay synchronized.
- ✅ Authenticated commuters can submit bookings through `POST /api/v1/bookings/me` using only the trip and seat, with the passenger resolved from the session.
- ⚠️ Apply an EF migration for the new `Passenger.UserId` relationship before registering new commuters against an existing database.
