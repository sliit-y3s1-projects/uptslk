// Request options and query encoding only; all HTTP requests use the shared apiClient.
export function jsonBody(method: string, body: unknown): RequestInit {
  return {
    method,
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  };
}
export function queryString(filters: object): string {
  const params = new URLSearchParams();
  for (const [key, value] of Object.entries(filters)) {
    if (value !== undefined && value !== "") params.set(key, String(value));
  }
  return params.size ? `?${params}` : "";
}
export function errorMessage(error: unknown): string {
  if (!(error instanceof Error)) return "The request could not be completed.";
  try {
    const body = JSON.parse(error.message);
    if (typeof body.error === "string") return body.error;
    if (body.errors) return Object.values(body.errors).flat().join(" ");
    if (typeof body.title === "string") return body.title;
  } catch {
    /* The shared client also returns plain-text and network errors. */
  }
  return error.message === "Failed to fetch"
    ? "Cannot reach the API. Check your connection and try again."
    : error.message;
}
