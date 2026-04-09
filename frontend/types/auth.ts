export type AuthUser = {
  id: string;
  name: string;
  email: string;
};

export type LoginInput = {
  email: string;
  password: string;
};

export type RegisterInput = {
  name: string;
  email: string;
  password: string;
};

export type AuthPayload = {
  success: boolean;
  message: string;
  token: string | null;
  user: AuthUser | null;
};