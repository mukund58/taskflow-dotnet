import { apiClient } from "@/services/api/client";
import type { ApiResponse } from "@/types/api";
import type {
  ChangePasswordInput,
  DeleteAccountInput,
  UpdateUserProfileInput,
  UserProfile,
} from "@/types/profile";

export async function getMyProfile() {
  const response = await apiClient.get<ApiResponse<UserProfile>>("/api/profile", {
    auth: true,
  });

  return response.data;
}

export async function updateMyProfile(payload: UpdateUserProfileInput) {
  const response = await apiClient.put<ApiResponse<UserProfile>>("/api/profile", payload, {
    auth: true,
  });

  return response.data;
}

export async function changeMyPassword(payload: ChangePasswordInput) {
  await apiClient.put<ApiResponse<unknown>>("/api/profile/change-password", payload, {
    auth: true,
  });
}

export async function deleteMyAccount(payload: DeleteAccountInput) {
  await apiClient.delete<ApiResponse<unknown>>("/api/profile", {
    auth: true,
    body: payload,
  });
}
