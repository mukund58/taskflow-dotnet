import { apiClient } from "@/services/api/client";
import type { ApiResponse } from "@/types/api";
import type {
  AddProjectMemberInput,
  CreateProjectInput,
  CreateProjectInvitationInput,
  Project,
  ProjectInvitation,
  ProjectMember,
} from "@/types/project";

export async function getProjects() {
  const response = await apiClient.get<ApiResponse<Project[]>>("/api/projects", {
    auth: true,
  });

  return response.data;
}

export async function createProject(payload: CreateProjectInput) {
  const response = await apiClient.post<ApiResponse<Project>>("/api/projects", payload, {
    auth: true,
  });

  return response.data;
}

export async function getProjectById(projectId: string) {
  const response = await apiClient.get<ApiResponse<Project>>(`/api/projects/${projectId}`, {
    auth: true,
  });

  return response.data;
}

export async function getProjectMembers(projectId: string) {
  const response = await apiClient.get<ApiResponse<ProjectMember[]>>(`/api/projects/${projectId}/members`, {
    auth: true,
  });

  return response.data;
}

export async function addProjectMember(projectId: string, payload: AddProjectMemberInput) {
  const response = await apiClient.post<ApiResponse<ProjectMember>>(`/api/projects/${projectId}/members`, payload, {
    auth: true,
  });

  return response.data;
}

export async function getProjectInvitations(projectId: string) {
  const response = await apiClient.get<ApiResponse<ProjectInvitation[]>>(`/api/projects/${projectId}/invitations`, {
    auth: true,
  });

  return response.data;
}

export async function createProjectInvitation(projectId: string, payload: CreateProjectInvitationInput) {
  const response = await apiClient.post<ApiResponse<ProjectInvitation>>(
    `/api/projects/${projectId}/invitations`,
    payload,
    {
      auth: true,
    },
  );

  return response.data;
}
