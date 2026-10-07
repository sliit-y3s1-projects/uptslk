# Web test foundation

Tools: **Vitest** (runner), **React Testing Library** (components), **user-event** (typing/clicking),
**MSW** (mock API), **jsdom** (browser environment), **jest-dom** (matchers such as `toBeInTheDocument`).

```bash
pnpm test             # run once
pnpm test:watch       # re-run on change
pnpm test:coverage    # coverage report in ./coverage (open coverage/index.html)
```

## Where tests go

One folder per team member's component, the same names as the backend test folders in `apps/api.Tests`:

| Folder | Component | Owner |
| --- | --- | --- |
| `src/test/centres/` | A: Centres and Network | kishan-ahamed45 |
| `src/test/fleet/` | B: Fleet and Maintenance | RashmiK0119 |
| `src/test/dispatch/` | C: Scheduling and Dispatch | chamals3n4 |
| `src/test/bookings/` | D: Passengers and Fares | Nadeesha-D-Shalom |

Name files after what they test, e.g. `src/test/fleet/VehicleForm.test.tsx`. Only `*.test.ts` / `*.test.tsx` files under
`src/` are run. (`src/features/fares/tests/contracts.test.mjs` uses Node's own test runner and is not part of Vitest.)
The `.gitkeep` files only keep the empty folders in git; delete them once a folder has a test.

## Writing a test

```tsx
import { renderWithProviders, screen } from "@/test/render";
import { buildStaff } from "@/test/factories";
import { server } from "@/test/server";
import { apiUrl, http, HttpResponse } from "@/test/handlers";

it("shows vehicles from the API", async () => {
  server.use(http.get(apiUrl("/api/v1/vehicles"), () => HttpResponse.json([{ id: "1", plateNumber: "NB-1000" }])));

  const { user } = renderWithProviders(<FleetPage />, { route: "/fleet", user: buildStaff("FleetOfficer") });

  expect(await screen.findByText("NB-1000")).toBeInTheDocument();
});
```

- `renderWithProviders(ui, { route, user, auth, queryClient })` wraps the component in React Query, a router and the auth
  context. `user: null` (default) is an anonymous visitor. Auth actions (`login`, `logout`, ...) are `vi.fn()` spies.
- Every API call must be mocked. A request with no handler **fails the test** instead of reaching a real server.
  `src/test/handlers.ts` holds the defaults (anonymous visitor); add per-test responses with `server.use(...)`, they are
  reset after each test.
- `buildUser()` / `buildStaff(role)` in `src/test/factories.ts` create valid users. Roles match the API: Admin,
  CentreManager, Dispatcher, FleetOfficer, Driver, Commuter.
- Query by role and label (`getByRole`, `getByLabelText`) as a user would, not by CSS class.
- `src/test/foundation.test.tsx` only checks that this setup works. Do not add feature tests there.
