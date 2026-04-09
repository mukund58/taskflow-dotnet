import { apiClient } from "@/services/api/client";
import type { ApiResponse } from "@/types/api";
import type { User } from "@/types/user";

export async function getUsers() {
  const response = await apiClient.get<ApiResponse<User[]>>("/api/users", {
    auth: true,
  });

  return response.data;
}
