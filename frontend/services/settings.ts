import { apiClient } from "@/services/api/client";
import type { ApiResponse } from "@/types/api";
import type { UpdateUserSettingsInput, UserSettings } from "@/types/settings";

export async function getMySettings() {
  const response = await apiClient.get<ApiResponse<UserSettings>>("/api/settings", {
    auth: true,
  });

  return response.data;
}

export async function updateMySettings(payload: UpdateUserSettingsInput) {
  const response = await apiClient.put<ApiResponse<UserSettings>>("/api/settings", payload, {
    auth: true,
  });

  return response.data;
}
