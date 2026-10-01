export const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5250";

/** Error thrown for failed API calls, carrying the HTTP status and any machine-readable code. */
export class ApiError extends Error {
  status: number;
  code?: string;

  constructor(message: string, status: number, code?: string) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.code = code;
  }
}

export async function apiClient<T>(
  path: string,
  options: RequestInit = {},
): Promise<T> {
  const headers = new Headers(options.headers);

  let response = await fetch(`${API_BASE_URL}${path}`, {
    ...options,
    headers,
    credentials: "include",
  });

  if (
    response.status === 401 &&
    path !== "/api/v1/auth/refresh" &&
    path !== "/api/v1/auth/login"
  ) {
    const refreshed = await fetch(`${API_BASE_URL}/api/v1/auth/refresh`, {
      method: "POST",
      credentials: "include",
    });
    if (refreshed.ok)
      response = await fetch(`${API_BASE_URL}${path}`, {
        ...options,
        headers,
        credentials: "include",
      });
  }

  if (response.status === 204) return undefined as T;
  if (!response.ok) {
    const raw = await response.text();
    try {
      const payload = JSON.parse(raw) as {
        error?: string | string[];
        code?: string;
        title?: string;
        detail?: string;
      };
      const error = payload.error;
      throw new ApiError(
        Array.isArray(error)
          ? error.join(" ")
          : (error ?? payload.detail ?? payload.title ?? "Request failed"),
        response.status,
        payload.code,
      );
    } catch (cause) {
      if (
        cause instanceof Error &&
        cause.message !== "Unexpected end of JSON input"
      )
        throw cause;
      throw new Error(raw || "Request failed", { cause });
    }
  }
  return response.json() as Promise<T>;
}
