export type SettingsTheme = "light" | "dark" | "system";

export type UserSettings = {
  theme: SettingsTheme;
  language: string;
  timezone: string;
  emailNotificationsEnabled: boolean;
  pushNotificationsEnabled: boolean;
};

export type UpdateUserSettingsInput = UserSettings;
