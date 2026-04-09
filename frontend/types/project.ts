export type Project = {
  id: string;
  name: string;
  description: string;
  dueDate?: string | null;
  ownerUserId?: string | null;
  createdAt?: string;
};

export type CreateProjectInput = {
  name: string;
  description: string;
  dueDate?: string | null;
};

export type ProjectMember = {
  userId: string;
  name: string;
  email: string;
  role: "Admin" | "Member";
  isProjectOwner: boolean;
  addedAt: string;
};

export type AddProjectMemberInput = {
  userId?: string;
  email?: string;
  role: "Admin" | "Member";
};

export type ProjectInvitation = {
  id: string;
  email: string;
  role: "Admin" | "Member";
  status: "Pending" | "Accepted" | "Revoked" | "Expired";
  invitedByUserId: string;
  invitedByUserName: string;
  createdAt: string;
  expiresAt?: string | null;
};

export type CreateProjectInvitationInput = {
  email: string;
  role: "Admin" | "Member";
  expiresInDays?: number;
};
