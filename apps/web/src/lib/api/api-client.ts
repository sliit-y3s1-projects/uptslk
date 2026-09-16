export const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5250";

export async function apiClient<T>(path: string, options: RequestInit = {}): Promise<T> {
  const headers = new Headers(options.headers);

  let response = await fetch(`${API_BASE_URL}${path}`, {
    ...options,
    headers,
    credentials: "include",
  });

  if (response.status === 401 && path !== "/api/v1/auth/refresh" && path !== "/api/v1/auth/login") {
    const refreshed = await fetch(`${API_BASE_URL}/api/v1/auth/refresh`, { method: "POST", credentials: "include" });
    if (refreshed.ok) response = await fetch(`${API_BASE_URL}${path}`, { ...options, headers, credentials: "include" });
  }

  if (response.status === 204) return undefined as T;
  if (!response.ok) throw new Error(await response.text() || "Request failed");
  return response.json() as Promise<T>;
}
