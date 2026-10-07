import type { ReactElement } from "react";
import { render } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter } from "react-router";
import { vi } from "vitest";
import { AuthContext } from "@/context/auth-context";
import type { AuthContextType, User } from "@/types/auth";

export { screen, within, waitFor, waitForElementToBeRemoved, fireEvent, act } from "@testing-library/react";
export { userEvent };

/** A query client for tests: no retries (a failed request fails the test at once) and no caching between tests. */
export function createTestQueryClient() {
  return new QueryClient({
    defaultOptions: { queries: { retry: false, gcTime: 0 }, mutations: { retry: false } },
  });
}

export interface RenderOptions {
  /** Initial URL for the router, e.g. "/admin/fleet". Default "/". */
  route?: string;
  /** The logged-in user. Leave out (null) to render as an anonymous visitor. */
  user?: User | null;
  /** Replace parts of the auth context, e.g. `{ login: vi.fn().mockRejectedValue(new Error("Invalid credentials")) }`. */
  auth?: Partial<AuthContextType>;
  queryClient?: QueryClient;
}

/**
 * Renders a component inside the same providers the app uses: React Query, a router and the auth context.
 * The auth context is supplied directly (no network call). Returns the usual Testing Library helpers plus
 * `user` (a userEvent instance), `queryClient` and `auth` (whose functions are vi.fn() spies).
 */
export function renderWithProviders(ui: ReactElement, options: RenderOptions = {}) {
  const { route = "/", user = null, auth, queryClient = createTestQueryClient() } = options;
  const authValue: AuthContextType = {
    user,
    token: user ? "cookie-session" : null,
    loading: false,
    login: vi.fn(async () => user?.role ?? ""),
    register: vi.fn(async () => null),
    setProfilePhotoUrl: vi.fn(),
    setProfile: vi.fn(),
    logout: vi.fn(),
    ...auth,
  };

  const result = render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[route]}>
        <AuthContext.Provider value={authValue}>{ui}</AuthContext.Provider>
      </MemoryRouter>
    </QueryClientProvider>,
  );

  return { ...result, user: userEvent.setup(), queryClient, auth: authValue };
}
