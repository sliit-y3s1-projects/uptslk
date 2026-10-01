import { LogOut, Pencil, Upload } from "lucide-react";
import { useNavigate } from "react-router";
import { PageHeading } from "@/components/shared/PageHeading";
import { StatusBadge } from "@/components/shared/StatusBadge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Spinner } from "@/components/ui/spinner";
import { useAuth } from "@/hooks/useAuth";
import { useProfileEditor } from "./useProfileEditor";

const roleLabels: Record<string, string> = {
  SuperAdmin: "Super administrator",
  Admin: "Administrator",
  Manager: "Centre manager",
};

export function StaffProfilePage() {
  const { logout } = useAuth();
  const navigate = useNavigate();
  const {
    user,
    editing,
    saving,
    name,
    setName,
    location,
    setLocation,
    nic,
    setNic,
    photo,
    gender,
    initials,
    saveError,
    handleSave,
    handleEdit,
    handleCancel,
    handlePhoto,
  } = useProfileEditor();
  if (!user) return null;

  return (
    <main className="flex flex-1 flex-col gap-4 bg-muted/20 p-4">
      <PageHeading
        title="My profile"
        description="Your account details and sign-in settings."
        action={
          editing ? (
            <div className="flex gap-2">
              <Button
                variant="outline"
                onClick={handleCancel}
                disabled={saving}
              >
                Cancel
              </Button>
              <Button onClick={() => void handleSave()} disabled={saving}>
                {saving && <Spinner />}
                {saving ? "Saving" : "Save changes"}
              </Button>
            </div>
          ) : (
            <Button variant="outline" onClick={handleEdit}>
              <Pencil /> Edit profile
            </Button>
          )
        }
      />

      <section className="grid gap-4 lg:grid-cols-[320px_minmax(0,1fr)]">
        <article className="rounded-lg border bg-card p-5">
          <div className="flex items-center gap-4">
            <label
              className={`relative flex size-16 shrink-0 items-center justify-center overflow-hidden rounded-lg bg-primary text-lg font-semibold text-primary-foreground ${
                editing ? "cursor-pointer" : "cursor-default"
              }`}
            >
              {photo ? (
                <img
                  src={photo}
                  alt="Profile"
                  className="size-full object-cover"
                />
              ) : (
                <span>{initials}</span>
              )}
              {editing && (
                <span className="absolute inset-0 flex items-center justify-center bg-foreground/60 text-background">
                  <Upload className="size-5" />
                </span>
              )}
              <input
                type="file"
                accept="image/jpeg,image/png,image/webp"
                className="sr-only"
                onChange={handlePhoto}
                disabled={!editing}
              />
            </label>
            <div className="min-w-0">
              <h2 className="truncate font-semibold">{name}</h2>
              <p className="truncate text-sm text-muted-foreground">
                {user.email}
              </p>
            </div>
          </div>
          <dl className="mt-5 space-y-3 border-t pt-4 text-sm">
            <div className="flex items-center justify-between gap-3">
              <dt className="text-muted-foreground">Role</dt>
              <dd className="font-medium">
                {roleLabels[user.role] ?? user.role}
              </dd>
            </div>
            <div className="flex items-center justify-between gap-3">
              <dt className="text-muted-foreground">Status</dt>
              <dd>
                <StatusBadge label="Active" tone="good" />
              </dd>
            </div>
          </dl>
        </article>

        <article className="rounded-lg border bg-card p-5">
          <h2 className="font-semibold">Personal information</h2>
          {editing ? (
            <div className="mt-4 grid gap-4 sm:grid-cols-2">
              <div className="space-y-1.5">
                <Label htmlFor="staff-name">Full name</Label>
                <Input
                  id="staff-name"
                  value={name}
                  onChange={(event) => setName(event.target.value)}
                />
              </div>
              <div className="space-y-1.5">
                <Label htmlFor="staff-location">Home location</Label>
                <Input
                  id="staff-location"
                  placeholder="City or district"
                  value={location}
                  onChange={(event) => setLocation(event.target.value)}
                />
              </div>
              <div className="space-y-1.5">
                <Label htmlFor="staff-nic">NIC number</Label>
                <Input
                  id="staff-nic"
                  placeholder="Enter NIC for verification"
                  value={nic}
                  onChange={(event) => setNic(event.target.value)}
                />
              </div>
              <div className="space-y-1.5">
                <Label htmlFor="staff-gender">Gender</Label>
                <Input
                  id="staff-gender"
                  placeholder="Derived from NIC"
                  value={gender}
                  disabled
                />
              </div>
              {saveError && (
                <p className="rounded-md border border-destructive/20 bg-destructive/10 p-3 text-sm text-destructive sm:col-span-2">
                  {saveError}
                </p>
              )}
            </div>
          ) : (
            <dl className="mt-4 grid gap-4 sm:grid-cols-2">
              <Info label="Full name" value={name} />
              <Info label="Email" value={user.email} />
              <Info label="Home location" value={location || "Not set"} />
              <Info label="NIC number" value={nic || "Not provided"} />
              <Info label="Gender" value={gender || "Not set"} />
              <Info
                label="Identity verification"
                value={nic ? "Pending verification" : "Not completed"}
              />
            </dl>
          )}
        </article>
      </section>

      <section className="flex flex-col gap-3 rounded-lg border bg-card p-5 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h2 className="font-semibold">Security</h2>
          <p className="mt-1 text-sm text-muted-foreground">
            Update your password or end this session.
          </p>
        </div>
        <div className="flex gap-2">
          <Button
            variant="outline"
            onClick={() => navigate("/profile/password")}
          >
            Change password
          </Button>
          <Button
            variant="outline"
            className="text-destructive"
            onClick={logout}
          >
            <LogOut /> Sign out
          </Button>
        </div>
      </section>
    </main>
  );
}

function Info({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <dt className="text-xs text-muted-foreground">{label}</dt>
      <dd className="mt-1 break-words text-sm font-medium">{value}</dd>
    </div>
  );
}
