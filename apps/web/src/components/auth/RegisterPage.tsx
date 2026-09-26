import { useEffect, useState } from "react";
import { Eye, EyeOff, ImagePlus } from "lucide-react";
import { Link, useNavigate } from "react-router";
import { useAuth } from "@/hooks/useAuth";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Spinner } from "@/components/ui/spinner";

export function RegisterPage() {
  const { register } = useAuth();
  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [profilePhoto, setProfilePhoto] = useState<File | null>(null);
  const [photoPreview, setPhotoPreview] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [showPassword, setShowPassword] = useState(false);
  const navigate = useNavigate();

  useEffect(
    () => () => {
      if (photoPreview) URL.revokeObjectURL(photoPreview);
    },
    [photoPreview],
  );

  function handlePhotoChange(event: React.ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0];
    if (!file) return;
    if (!["image/jpeg", "image/png", "image/webp"].includes(file.type)) {
      setError("Choose a JPG, PNG, or WebP image.");
      event.target.value = "";
      return;
    }
    if (file.size > 5 * 1024 * 1024) {
      setError("Profile images cannot exceed 5 MB.");
      event.target.value = "";
      return;
    }
    setError(null);
    setProfilePhoto(file);
    setPhotoPreview(URL.createObjectURL(file));
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setSubmitting(true);
    try {
      // register() always creates a Commuter account, role is not
      // exposed to the client, see AuthContext
      const profilePhotoWarning = await register(
        name,
        email,
        password,
        profilePhoto ?? undefined,
      );
      navigate("/onboarding", {
        state: profilePhotoWarning ? { profilePhotoWarning } : undefined,
      });
    } catch (cause) {
      let message = "Could not create account, please try again";
      if (cause instanceof Error) {
        try {
          const body = JSON.parse(cause.message) as {
            error?: string[] | string;
          };
          message = Array.isArray(body.error)
            ? body.error.join(" ")
            : (body.error ?? message);
        } catch {
          message = cause.message || message;
        }
      }
      setError(message);
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <main className="flex min-h-screen items-center justify-center bg-slate-50 px-4 py-10">
      <section className="w-full max-w-md rounded-2xl border border-slate-200 bg-white p-7 sm:p-9">
        <div className="mb-7">
          <h1 className="text-2xl font-semibold tracking-tight text-slate-950">
            Create your UPTSLK account
          </h1>
          <p className="mt-2 text-sm text-slate-500">
            Enter your details to get started.
          </p>
        </div>

        <form onSubmit={handleSubmit} className="w-full space-y-4 text-left">
          <div className="space-y-1.5">
            <Label htmlFor="profile-photo">Profile photo (optional)</Label>
            <label
              htmlFor="profile-photo"
              className="flex cursor-pointer items-center gap-4 rounded-xl border border-slate-300 p-3 transition-colors hover:border-primary"
            >
              <span className="flex size-14 shrink-0 items-center justify-center overflow-hidden rounded-full bg-primary/10 text-primary">
                {photoPreview ? (
                  <img
                    src={photoPreview}
                    alt="Selected profile"
                    className="size-full object-cover"
                  />
                ) : (
                  <ImagePlus className="size-5" />
                )}
              </span>
              <span className="min-w-0">
                <span className="block truncate text-sm font-medium text-slate-900">
                  {profilePhoto?.name ?? "Choose a profile photo"}
                </span>
                <span className="mt-1 block text-xs text-slate-500">
                  JPG, PNG, or WebP · Up to 5 MB
                </span>
              </span>
              <input
                id="profile-photo"
                type="file"
                accept="image/jpeg,image/png,image/webp"
                className="sr-only"
                onChange={handlePhotoChange}
                disabled={submitting}
              />
            </label>
          </div>
          <div className="space-y-1.5">
            <Label htmlFor="name">Full Name</Label>
            <Input
              id="name"
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="Your full name"
              autoComplete="name"
              required
            />
          </div>
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
          <div className="relative space-y-1.5">
            <Label htmlFor="password">Password</Label>
            <Input
              id="password"
              type={showPassword ? "text" : "password"}
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="At least 8 characters"
              autoComplete="new-password"
              required
              minLength={8}
              className="pr-11"
            />
            <button
              type="button"
              aria-label={showPassword ? "Hide password" : "Show password"}
              onClick={() => setShowPassword((visible) => !visible)}
              className="absolute right-2 top-1/2 -translate-y-1/2 p-2 text-muted-foreground"
            >
              {showPassword ? (
                <EyeOff className="size-4" />
              ) : (
                <Eye className="size-4" />
              )}
            </button>
          </div>

          {error && <p className="text-sm text-destructive">{error}</p>}

          <Button
            type="submit"
            className="h-11 w-full"
            disabled={submitting}
          >
            {submitting ? (
              <>
                <Spinner className="size-4" />
                Creating account
              </>
            ) : (
              "Sign Up"
            )}
          </Button>
        </form>
        <p className="mt-6 text-center text-sm text-slate-500">
          Already have an account?{" "}
          <Link to="/login" className="font-medium text-primary">
            Sign in
          </Link>
        </p>
      </section>
    </main>
  );
}
