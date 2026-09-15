import { apiClient } from "@/lib/api/api-client";
import type { CentreOption } from "../types";

export async function getCentres(): Promise<CentreOption[]> {
  return apiClient<CentreOption[]>("/api/v1/centres");
}

