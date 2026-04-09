"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { FormEvent, useState } from "react";
import { Button } from "@/components/ui/Button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/Card";
import { Input } from "@/components/ui/Input";
import { Label } from "@/components/ui/Label";
import { register } from "@/services/auth";

export default function RegisterPage() {
  const router = useRouter();
  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(false);

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    try {
      setError(null);
      setIsLoading(true);
      await register({ name, email, password });
      router.push("/dashboard");
    } catch (caughtError) {
      const message = caughtError instanceof Error ? caughtError.message : "Unable to register";
      setError(message);
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

            {error ? <p className="text-sm text-destructive">{error}</p> : null}

            <Button type="submit" className="w-full" isLoading={isLoading}>
              Create account
            </Button>
          </form>

          <p className="text-sm text-muted-foreground">
            Already have an account?{" "}
            <Link className="font-semibold text-primary hover:underline" href="/auth/login">
              Sign in
            </Link>
          </p>
        </CardContent>
      </Card>
    </div>
  );
}