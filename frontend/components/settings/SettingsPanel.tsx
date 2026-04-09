"use client";

import { type FormEvent, useState } from "react";
import { useRouter } from "next/navigation";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import toast from "react-hot-toast";
import { Badge } from "@/components/ui/Badge";
import { Button } from "@/components/ui/Button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/Card";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/Dialog";
import { Input } from "@/components/ui/Input";
import { Label } from "@/components/ui/Label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/Select";
import { ApiError } from "@/services/api";
import { clearAuthToken } from "@/services/auth/token-store";
import { changeMyPassword, deleteMyAccount, getMyProfile, updateMyProfile } from "@/services/profile";
import { getMySettings, updateMySettings } from "@/services/settings";
import { useAuthStore } from "@/store/authStore";
import type { ChangePasswordInput, UserProfile } from "@/types/profile";
import type { SettingsTheme, UserSettings } from "@/types/settings";

function getErrorMessage(error: unknown, fallback = "Something went wrong") {
  if (error instanceof ApiError) {
    return error.message;
  }

  if (error instanceof Error) {
    return error.message;
  }

  return fallback;
}

const fallbackSettings: UserSettings = {
  theme: "system",
  language: "en",
  timezone: "UTC",
  emailNotificationsEnabled: true,
  pushNotificationsEnabled: true,
};

function profileToSettings(profile: UserProfile): UserSettings {
  return {
    theme: profile.theme,
    language: profile.language,
    timezone: profile.timezone,
    emailNotificationsEnabled: profile.emailNotificationsEnabled,
    pushNotificationsEnabled: profile.pushNotificationsEnabled,
  };
}

export function SettingsPanel() {
  const router = useRouter();
  const queryClient = useQueryClient();
  const clearSession = useAuthStore((state) => state.clearSession);
  const [settingsOverrides, setSettingsOverrides] = useState<Partial<UserSettings>>({});
  const [profileOverrides, setProfileOverrides] = useState<Partial<Pick<UserProfile, "name" | "email">>>({});
  const [passwordForm, setPasswordForm] = useState<ChangePasswordInput>({
    currentPassword: "",
    newPassword: "",
    confirmNewPassword: "",
  });
  const [isDeleteDialogOpen, setIsDeleteDialogOpen] = useState(false);
  const [deletePassword, setDeletePassword] = useState("");

  const profileQuery = useQuery({
    queryKey: ["profile", "me"],
    queryFn: () => getMyProfile(),
  });

  const settingsQuery = useQuery({
    queryKey: ["settings", "me"],
    queryFn: () => getMySettings(),
  });

  const saveProfileMutation = useMutation({
    mutationFn: updateMyProfile,
    onSuccess: async (nextProfile) => {
      toast.success("Profile saved");
      setProfileOverrides({});
      queryClient.setQueryData(["profile", "me"], nextProfile);
      queryClient.setQueryData(["settings", "me"], profileToSettings(nextProfile));

      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ["profile", "me"] }),
        queryClient.invalidateQueries({ queryKey: ["settings", "me"] }),
      ]);
    },
    onError: (error) => {
      toast.error(getErrorMessage(error, "Could not save profile"));
    },
  });

  const saveSettingsMutation = useMutation({
    mutationFn: updateMySettings,
    onSuccess: async (nextSettings) => {
      toast.success("Settings saved");
      setSettingsOverrides({});
      queryClient.setQueryData(["settings", "me"], nextSettings);

      const currentProfile = queryClient.getQueryData<UserProfile>(["profile", "me"]);
      if (currentProfile) {
        queryClient.setQueryData(["profile", "me"], {
          ...currentProfile,
          ...nextSettings,
        });
      }

      await queryClient.invalidateQueries({ queryKey: ["settings", "me"] });
    },
    onError: (error) => {
      toast.error(getErrorMessage(error, "Could not save settings"));
    },
  });

  const changePasswordMutation = useMutation({
    mutationFn: changeMyPassword,
    onSuccess: () => {
      toast.success("Password changed");
      setPasswordForm({
        currentPassword: "",
        newPassword: "",
        confirmNewPassword: "",
      });
    },
    onError: (error) => {
      toast.error(getErrorMessage(error, "Could not change password"));
    },
  });

  const deleteAccountMutation = useMutation({
    mutationFn: deleteMyAccount,
    onSuccess: () => {
      toast.success("Account deleted");
      setDeletePassword("");
      setIsDeleteDialogOpen(false);
      clearAuthToken();
      clearSession();
      queryClient.clear();
      router.push("/auth/login");
    },
    onError: (error) => {
      toast.error(getErrorMessage(error, "Could not delete account"));
    },
  });

  const baseProfile = profileQuery.data;
  const profileDraft = {
    name: profileOverrides.name ?? baseProfile?.name ?? "",
    email: profileOverrides.email ?? baseProfile?.email ?? "",
  };

  const setProfileDraftValue = (key: "name" | "email", value: string) => {
    setProfileOverrides((current) => {
      const next = { ...current, [key]: value };

      if (baseProfile && value === baseProfile[key]) {
        const rest = { ...next };
        delete rest[key];
        return rest;
      }

      return next;
    });
  };

  const hasUnsavedProfileChanges = Object.keys(profileOverrides).length > 0;

  const baseSettings = settingsQuery.data ?? (baseProfile ? profileToSettings(baseProfile) : fallbackSettings);
  const settingsDraft: UserSettings = { ...baseSettings, ...settingsOverrides };
  const hasUnsavedPreferenceChanges = Object.keys(settingsOverrides).length > 0;

  const setDraftValue = <TKey extends keyof UserSettings>(key: TKey, value: UserSettings[TKey]) => {
    setSettingsOverrides((current) => {
      const next = { ...current, [key]: value };

      if (value === baseSettings[key]) {
        const rest = { ...next };
        delete rest[key];
        return rest;
      }

      return next;
    });
  };

  const handleProfileSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    if (!baseProfile) {
      toast.error("Profile is still loading");
      return;
    }

    const name = profileDraft.name.trim();
    const email = profileDraft.email.trim();
    if (!name || !email) {
      toast.error("Name and email are required");
      return;
    }

    const preferences = settingsQuery.data ?? profileToSettings(baseProfile);
    saveProfileMutation.mutate({
      name,
      email,
      theme: preferences.theme,
      language: preferences.language,
      timezone: preferences.timezone,
      emailNotificationsEnabled: preferences.emailNotificationsEnabled,
      pushNotificationsEnabled: preferences.pushNotificationsEnabled,
    });
  };

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    saveSettingsMutation.mutate({
      theme: settingsDraft.theme,
      language: settingsDraft.language.trim(),
      timezone: settingsDraft.timezone.trim(),
      emailNotificationsEnabled: settingsDraft.emailNotificationsEnabled,
      pushNotificationsEnabled: settingsDraft.pushNotificationsEnabled,
    });
  };

  const handlePasswordSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    changePasswordMutation.mutate(passwordForm);
  };

  const handleDeleteAccount = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    const currentPassword = deletePassword.trim();
    if (!currentPassword) {
      toast.error("Current password is required");
      return;
    }

    deleteAccountMutation.mutate({ currentPassword });
  };

  return (
    <div className="space-y-6">
      <section className="surface-card p-6 md:p-8">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div>
            <Badge variant="secondary">Account</Badge>
            <h2 className="mt-2 text-3xl font-semibold tracking-tight">Settings</h2>
            <p className="mt-2 text-sm text-muted-foreground">
              Manage your personal experience, language, timezone, and notifications.
            </p>
          </div>
        </div>
      </section>

      {profileQuery.isLoading ? (
        <Card className="animate-pulse">
          <CardHeader className="space-y-3">
            <div className="h-7 w-44 rounded bg-muted" />
            <div className="h-4 w-3/5 rounded bg-muted" />
          </CardHeader>
          <CardContent className="grid gap-4 pb-6 lg:grid-cols-2">
            <div className="h-10 rounded bg-muted" />
            <div className="h-10 rounded bg-muted" />
            <div className="h-12 rounded bg-muted lg:col-span-2" />
          </CardContent>
        </Card>
      ) : null}

      {profileQuery.isError ? (
        <Card>
          <CardHeader>
            <CardTitle>Unable to load profile</CardTitle>
            <CardDescription>{getErrorMessage(profileQuery.error)}</CardDescription>
          </CardHeader>
          <CardContent>
            <Button variant="outline" onClick={() => void profileQuery.refetch()}>
              Retry
            </Button>
          </CardContent>
        </Card>
      ) : null}

      {!profileQuery.isLoading && !profileQuery.isError && profileQuery.data ? (
        <Card>
          <CardHeader>
            <CardTitle>Profile</CardTitle>
            <CardDescription>Update your name and email.</CardDescription>
          </CardHeader>
          <CardContent className="pb-6">
            <form className="grid gap-4 lg:grid-cols-2" onSubmit={handleProfileSubmit}>
              <div className="space-y-1.5">
                <Label htmlFor="profile-name">Name</Label>
                <Input
                  id="profile-name"
                  value={profileDraft.name}
                  onChange={(event) => setProfileDraftValue("name", event.target.value)}
                  maxLength={100}
                  required
                />
              </div>

              <div className="space-y-1.5">
                <Label htmlFor="profile-email">Email</Label>
                <Input
                  id="profile-email"
                  type="email"
                  value={profileDraft.email}
                  onChange={(event) => setProfileDraftValue("email", event.target.value)}
                  required
                />
              </div>

              <div className="flex flex-wrap items-center gap-2 lg:col-span-2">
                <Badge variant="neutral">Role: {profileQuery.data.role}</Badge>
                <Button type="submit" isLoading={saveProfileMutation.isPending}>
                  Save profile
                </Button>
                {hasUnsavedProfileChanges ? (
                  <Badge variant="warning">Unsaved changes</Badge>
                ) : (
                  <Badge variant="success">Saved</Badge>
                )}
              </div>
            </form>
          </CardContent>
        </Card>
      ) : null}

      {settingsQuery.isLoading ? (
        <Card className="animate-pulse">
          <CardHeader className="space-y-3">
            <div className="h-7 w-44 rounded bg-muted" />
            <div className="h-4 w-3/5 rounded bg-muted" />
          </CardHeader>
          <CardContent className="grid gap-4 pb-6 lg:grid-cols-2">
            <div className="h-10 rounded bg-muted" />
            <div className="h-10 rounded bg-muted" />
            <div className="h-10 rounded bg-muted lg:col-span-2" />
            <div className="h-16 rounded bg-muted lg:col-span-2" />
          </CardContent>
        </Card>
      ) : null}

      {settingsQuery.isError ? (
        <Card>
          <CardHeader>
            <CardTitle>Unable to load settings</CardTitle>
            <CardDescription>{getErrorMessage(settingsQuery.error)}</CardDescription>
          </CardHeader>
          <CardContent>
            <Button variant="outline" onClick={() => void settingsQuery.refetch()}>
              Retry
            </Button>
          </CardContent>
        </Card>
      ) : null}

      {!settingsQuery.isLoading && !settingsQuery.isError ? (
        <Card>
          <CardHeader>
            <CardTitle>Preferences</CardTitle>
            <CardDescription>Your changes are saved to your account on the backend.</CardDescription>
          </CardHeader>
          <CardContent className="pb-6">
            <form className="grid gap-4 lg:grid-cols-2" onSubmit={handleSubmit}>
              <div className="space-y-1.5">
                <Label htmlFor="settings-theme">Theme</Label>
                <Select value={settingsDraft.theme} onValueChange={(value) => setDraftValue("theme", value as SettingsTheme)}>
                  <SelectTrigger id="settings-theme">
                    <SelectValue placeholder="Choose theme" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="system">System</SelectItem>
                    <SelectItem value="light">Light</SelectItem>
                    <SelectItem value="dark">Dark</SelectItem>
                  </SelectContent>
                </Select>
              </div>

              <div className="space-y-1.5">
                <Label htmlFor="settings-language">Language</Label>
                <Input
                  id="settings-language"
                  value={settingsDraft.language}
                  onChange={(event) => setDraftValue("language", event.target.value)}
                  placeholder="en"
                  maxLength={10}
                  required
                />
              </div>

              <div className="space-y-1.5 lg:col-span-2">
                <Label htmlFor="settings-timezone">Timezone</Label>
                <Input
                  id="settings-timezone"
                  value={settingsDraft.timezone}
                  onChange={(event) => setDraftValue("timezone", event.target.value)}
                  placeholder="UTC"
                  maxLength={64}
                  required
                />
              </div>

              <div className="space-y-3 lg:col-span-2">
                <Label>Notifications</Label>

                <label className="flex items-center justify-between rounded-lg border border-border p-3">
                  <div>
                    <p className="text-sm font-medium">Email notifications</p>
                    <p className="text-xs text-muted-foreground">Receive updates by email.</p>
                  </div>
                  <input
                    type="checkbox"
                    checked={settingsDraft.emailNotificationsEnabled}
                    onChange={(event) => setDraftValue("emailNotificationsEnabled", event.target.checked)}
                  />
                </label>

                <label className="flex items-center justify-between rounded-lg border border-border p-3">
                  <div>
                    <p className="text-sm font-medium">Push notifications</p>
                    <p className="text-xs text-muted-foreground">Receive in-app push notifications.</p>
                  </div>
                  <input
                    type="checkbox"
                    checked={settingsDraft.pushNotificationsEnabled}
                    onChange={(event) => setDraftValue("pushNotificationsEnabled", event.target.checked)}
                  />
                </label>
              </div>

              <div className="flex flex-wrap items-center gap-2 lg:col-span-2">
                <Button type="submit" isLoading={saveSettingsMutation.isPending}>
                  Save settings
                </Button>
                {hasUnsavedPreferenceChanges ? <Badge variant="warning">Unsaved changes</Badge> : <Badge variant="success">Saved</Badge>}
              </div>
            </form>
          </CardContent>
        </Card>
      ) : null}

      {!profileQuery.isLoading && !profileQuery.isError && profileQuery.data ? (
        <Card>
          <CardHeader>
            <CardTitle>Security</CardTitle>
            <CardDescription>Change your password. You will keep your current session.</CardDescription>
          </CardHeader>
          <CardContent className="pb-6">
            <form className="grid gap-4 lg:grid-cols-2" onSubmit={handlePasswordSubmit}>
              <div className="space-y-1.5 lg:col-span-2">
                <Label htmlFor="password-current">Current password</Label>
                <Input
                  id="password-current"
                  type="password"
                  autoComplete="current-password"
                  value={passwordForm.currentPassword}
                  onChange={(event) =>
                    setPasswordForm((current) => ({
                      ...current,
                      currentPassword: event.target.value,
                    }))
                  }
                  required
                />
              </div>

              <div className="space-y-1.5">
                <Label htmlFor="password-new">New password</Label>
                <Input
                  id="password-new"
                  type="password"
                  autoComplete="new-password"
                  value={passwordForm.newPassword}
                  onChange={(event) =>
                    setPasswordForm((current) => ({
                      ...current,
                      newPassword: event.target.value,
                    }))
                  }
                  required
                />
              </div>

              <div className="space-y-1.5">
                <Label htmlFor="password-confirm">Confirm new password</Label>
                <Input
                  id="password-confirm"
                  type="password"
                  autoComplete="new-password"
                  value={passwordForm.confirmNewPassword}
                  onChange={(event) =>
                    setPasswordForm((current) => ({
                      ...current,
                      confirmNewPassword: event.target.value,
                    }))
                  }
                  required
                />
              </div>

              <div className="lg:col-span-2">
                <Button type="submit" isLoading={changePasswordMutation.isPending}>
                  Update password
                </Button>
              </div>
            </form>
          </CardContent>
        </Card>
      ) : null}

      {!profileQuery.isLoading && !profileQuery.isError && profileQuery.data ? (
        <Card className="border-destructive/40">
          <CardHeader>
            <CardTitle>Danger Zone</CardTitle>
            <CardDescription>
              Deleting your account is permanent. You will lose access to protected routes immediately.
            </CardDescription>
          </CardHeader>
          <CardContent className="flex flex-wrap items-center justify-between gap-3 pb-6">
            <Badge variant="danger">Permanent action</Badge>
            <Button variant="danger" onClick={() => setIsDeleteDialogOpen(true)}>
              Delete account
            </Button>
          </CardContent>
        </Card>
      ) : null}

      <Dialog
        open={isDeleteDialogOpen}
        onOpenChange={(nextOpen) => {
          if (!deleteAccountMutation.isPending) {
            setIsDeleteDialogOpen(nextOpen);
            if (!nextOpen) {
              setDeletePassword("");
            }
          }
        }}
      >
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Delete Account</DialogTitle>
            <DialogDescription>
              Confirm your current password to permanently delete your account.
            </DialogDescription>
          </DialogHeader>

          <form className="space-y-4" onSubmit={handleDeleteAccount}>
            <div className="space-y-1.5">
              <Label htmlFor="delete-account-password">Current password</Label>
              <Input
                id="delete-account-password"
                type="password"
                autoComplete="current-password"
                value={deletePassword}
                onChange={(event) => setDeletePassword(event.target.value)}
                required
              />
            </div>

            <DialogFooter>
              <Button
                type="button"
                variant="outline"
                onClick={() => {
                  setDeletePassword("");
                  setIsDeleteDialogOpen(false);
                }}
                disabled={deleteAccountMutation.isPending}
              >
                Cancel
              </Button>
              <Button type="submit" variant="danger" isLoading={deleteAccountMutation.isPending}>
                Delete account permanently
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </div>
  );
}
