import type { SettingsTheme } from "@/types/settings";

export type UserProfile = {
  id: string;
  name: string;
  email: string;
  role: string;
  theme: SettingsTheme;
  language: string;
  timezone: string;
  emailNotificationsEnabled: boolean;
  pushNotificationsEnabled: boolean;
};

export type UpdateUserProfileInput = {
  name: string;
  email: string;
  theme: SettingsTheme;
  language: string;
  timezone: string;
  emailNotificationsEnabled: boolean;
  pushNotificationsEnabled: boolean;
};

export type ChangePasswordInput = {
  currentPassword: string;
  newPassword: string;
  confirmNewPassword: string;
};

export type DeleteAccountInput = {
  currentPassword: string;
};
