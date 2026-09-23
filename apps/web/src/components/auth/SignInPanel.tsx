import { useState } from "react";
import { Eye, EyeOff, LockKeyhole } from "lucide-react";
import { useAuth } from "@/hooks/useAuth";
import { Link, useNavigate, useSearchParams } from "react-router";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";

export function SignInPanel() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [showPassword, setShowPassword] = useState(false);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setSubmitting(true);
    try {
      const role = await login(email, password);
      const returnTo = params.get("returnTo");
      const safeReturnTo = returnTo?.startsWith("/booking/checkout")
        ? returnTo
        : null;
      navigate(
        role === "Commuter" && safeReturnTo
          ? safeReturnTo
          : role === "Admin" || role === "SuperAdmin"
            ? "/admin"
            : role === "Commuter"
              ? "/"
              : "/operations",
        { replace: true },
      );
    } catch (cause) {
      setError(
        cause instanceof Error ? cause.message : "Invalid email or password.",
      );
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <main className="flex min-h-screen items-center justify-center bg-slate-50 px-4 py-10 sm:px-6">
      <section className="w-full max-w-md rounded-2xl border border-slate-200 bg-white p-7 sm:p-9">
        <div className="mb-8 space-y-3">
          <div className="flex size-11 items-center justify-center rounded-xl bg-primary text-primary-foreground">
            <LockKeyhole className="size-5" />
          </div>
          <h1 className="text-2xl font-semibold tracking-tight text-slate-950">
            Welcome back
          </h1>
          <p className="text-sm leading-6 text-slate-500">
            Sign in to continue to your UPTSLK dashboard.
          </p>
        </div>

        <form onSubmit={handleSubmit} className="w-full space-y-4 text-left">
          <div className="space-y-1.5">
            <Label htmlFor="email">Email</Label>
            <Input
              id="email"
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="you@example.com"
              autoComplete="email"
              required
            />
          </div>
          <div className="space-y-1.5">
            <Label htmlFor="password">Password</Label>
            <div className="relative">
              <Input
                id="password"
                type={showPassword ? "text" : "password"}
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder="Enter your password"
                autoComplete="current-password"
                className="pr-11"
                required
              />
              <button
                type="button"
                aria-label={showPassword ? "Hide password" : "Show password"}
                onClick={() => setShowPassword((visible) => !visible)}
                className="absolute right-2 top-1/2 -translate-y-1/2 rounded-md p-2 text-muted-foreground hover:bg-muted"
              >
                {showPassword ? (
                  <EyeOff className="size-4" />
                ) : (
                  <Eye className="size-4" />
                )}
              </button>
            </div>
          </div>

          {error && <p className="text-sm text-destructive">{error}</p>}

          <Button
            type="submit"
            className="h-11 w-full rounded-lg"
            disabled={submitting}
          >
            {submitting ? "Signing in..." : "Sign In"}
          </Button>
        </form>
        <p className="mt-6 text-center text-sm text-slate-500">
          New to UPTSLK?{" "}
          <Link to="/signup" className="font-medium text-primary">
            Create an account
          </Link>
        </p>
        <p className="mt-7 text-center text-xs text-slate-400">
          Secure access for UPTSLK journeys and operations.
        </p>
      </section>
    </main>
  );
}
