import type { DashboardStats } from "@/types/dashboard";
import { apiClient } from "@/services/api/client";

const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5000";
const API_PATH_PREFIX = normalizeApiPathPrefix(process.env.NEXT_PUBLIC_API_PATH_PREFIX ?? "/api/v1");

function normalizeApiPathPrefix(pathPrefix: string) {
  const trimmed = pathPrefix.trim();
  if (!trimmed) {
    return "/api/v1";
  }

  const withLeadingSlash = trimmed.startsWith("/") ? trimmed : `/${trimmed}`;
  return withLeadingSlash.replace(/\/+$/, "");
}

export async function getDashboardStatsSSR() {
  try {
    const response = await fetch(`${API_BASE_URL}${API_PATH_PREFIX}/dashboard`, {
      method: "GET",
      cache: "no-store",
      headers: {
        Accept: "application/json",
      },
    });

    if (!response.ok) {
      return null;
    }

    return (await response.json()) as DashboardStats;
  } catch {
    return null;
  }
}

export function getDashboardStats() {
  return apiClient.get<DashboardStats>("/api/dashboard", {
    auth: true,
  });
}
