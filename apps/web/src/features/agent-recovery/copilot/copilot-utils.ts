export const errorMessage = (error: unknown) =>
  error instanceof Error ? error.message : "";

export const time = (value: string) =>
  new Date(value).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" });
