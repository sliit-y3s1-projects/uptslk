export const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5250";

export async function apiClient<T>(path: string, options: RequestInit = {}): Promise<T> {
  const token = localStorage.getItem("upts_token");
  const headers = new Headers(options.headers);
  if (token && token !== "demo-session") headers.set("Authorization", `Bearer ${token}`);

  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...options,
    headers,
  });

  if (response.status === 204) return undefined as T;
  if (!response.ok) throw new Error(await response.text() || "Request failed");
  return response.json() as Promise<T>;
}
