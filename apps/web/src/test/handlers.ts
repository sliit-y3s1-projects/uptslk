import { http, HttpResponse } from "msw";
import type { User } from "@/types/auth";

/** Base address of the mocked API (set in vitest.config.ts). */
export const API_BASE_URL = import.meta.env.VITE_API_BASE_URL as string;

export const apiUrl = (path: string) => `${API_BASE_URL}${path}`;

/**
 * Handlers that are active in every test. The default is an anonymous visitor: nobody is logged in and the session
 * cannot be refreshed. Anything else a test calls must be mocked in that test, otherwise the request fails loudly
 * (`onUnhandledRequest: "error"` in setup.ts).
 */
export const defaultHandlers = [
  http.get(apiUrl("/api/v1/auth/me"), () => HttpResponse.json({ error: "Unauthorized" }, { status: 401 })),
  http.post(apiUrl("/api/v1/auth/refresh"), () => new HttpResponse(null, { status: 401 })),
];

/** Handlers that make `/auth/me` return the given user, i.e. a logged-in session. Use with `server.use(...)`. */
export const loggedInHandlers = (user: User) => [
  http.get(apiUrl("/api/v1/auth/me"), () => HttpResponse.json(user)),
];

export { http, HttpResponse };
