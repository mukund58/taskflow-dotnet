"use client";

import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { FormEvent, useMemo, useState } from "react";
import { Button } from "@/components/ui/Button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/Card";
import { Input } from "@/components/ui/Input";
import { Label } from "@/components/ui/Label";
import { register } from "@/services/auth";
import { ApiError } from "@/services/api";

function resolveSafeRedirect(rawRedirect: string | null) {
  if (!rawRedirect) {
    return null;
  }

  if (!rawRedirect.startsWith("/") || rawRedirect.startsWith("//")) {
    return null;
  }

  return rawRedirect;
}

function extractFieldErrors(details: unknown): string[] {
  if (!details || typeof details !== "object") return [];
  const errors = (details as Record<string, unknown>).errors;
  if (!errors || typeof errors !== "object") return [];
  return Object.values(errors as Record<string, string[]>)
    .flat()
    .filter((msg): msg is string => typeof msg === "string");
}

export default function RegisterPage() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [fieldErrors, setFieldErrors] = useState<string[]>([]);
  const [isLoading, setIsLoading] = useState(false);

  const redirectTarget = resolveSafeRedirect(searchParams.get("redirect"));
  const loginHref = useMemo(() => {
    if (!redirectTarget) {
      return "/auth/login";
    }

    const query = new URLSearchParams({ redirect: redirectTarget });
    return `/auth/login?${query.toString()}`;
  }, [redirectTarget]);

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    try {
      setError(null);
      setFieldErrors([]);
      setIsLoading(true);
      await register({ name, email, password });
      router.push(redirectTarget ?? "/dashboard");
    } catch (caughtError) {
      const message = caughtError instanceof Error ? caughtError.message : "Unable to register";
      setError(message);
      if (caughtError instanceof ApiError) {
        setFieldErrors(extractFieldErrors(caughtError.details));
      }
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="mx-auto max-w-md py-6">
      <Card>
        <CardHeader>
          <CardTitle>Create account</CardTitle>
          <CardDescription>Register to start using projects, tasks, and dashboard.</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4 pb-6">
          <form onSubmit={handleSubmit} className="space-y-4">
            <div className="space-y-1.5">
              <Label htmlFor="name">Name</Label>
              <Input id="name" value={name} onChange={(event) => setName(event.target.value)} required />
            </div>

            <div className="space-y-1.5">
              <Label htmlFor="email">Email</Label>
              <Input
                id="email"
                type="email"
                autoComplete="email"
                value={email}
                onChange={(event) => setEmail(event.target.value)}
                required
              />
            </div>

            <div className="space-y-1.5">
              <Label htmlFor="password">Password</Label>
              <Input
                id="password"
                type="password"
                autoComplete="new-password"
                value={password}
                onChange={(event) => setPassword(event.target.value)}
                required
              />
            </div>

            {(error || fieldErrors.length > 0) && (
              <div className="rounded-md border border-destructive/40 bg-destructive/10 px-3 py-2 text-sm text-destructive space-y-1">
                {error && <p className="font-medium">{error}</p>}
                {fieldErrors.length > 0 && (
                  <ul className="list-disc list-inside space-y-0.5">
                    {fieldErrors.map((msg, i) => (
                      <li key={i}>{msg}</li>
                    ))}
                  </ul>
                )}
              </div>
            )}

            <Button type="submit" className="w-full" isLoading={isLoading}>
              Create account
            </Button>
          </form>

          <p className="text-sm text-muted-foreground">
            Already have an account?{" "}
            <Link className="font-semibold text-primary hover:underline" href={loginHref}>
              Sign in
            </Link>
          </p>
        </CardContent>
      </Card>
    </div>
  );
}