export function extractErrorMessage(error: unknown, fallbackMessage = "Request failed"): string {
  if (error instanceof Error) {
    try {
      const parsed = JSON.parse(error.message);
      if (typeof parsed?.error === "string") return parsed.error;
      if (Array.isArray(parsed?.error)) return parsed.error.join(", ");
      if (parsed?.errors && typeof parsed.errors === "object") {
        const messages = Object.values(parsed.errors).flat().filter(Boolean);
        if (messages.length > 0) return messages.join(". ");
      }
      if (typeof parsed?.title === "string") return parsed.title;
      if (typeof parsed?.message === "string") return parsed.message;
    } catch {
      // not JSON string
    }
    if (error.message && error.message.trim()) return error.message;
  }
  return fallbackMessage;
}

