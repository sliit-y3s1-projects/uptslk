import { CheckCircle2, ImagePlus, LogOut, Pencil } from "lucide-react";
import { useNavigate } from "react-router";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Spinner } from "@/components/ui/spinner";
import { useAuth } from "@/hooks/useAuth";
import { useProfileEditor } from "./useProfileEditor";

export function ProfilePage() {
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
  const accountLabel = "Commuter account";

  return (
    <main className="min-h-screen bg-slate-50">
      <div className="mx-auto max-w-7xl px-5 py-8 sm:px-8">
        <header className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
          <h1 className="text-3xl font-semibold tracking-tight text-slate-950">
            My Profile
          </h1>
          {editing ? (
            <div className="flex items-center gap-3">
              <Button
                variant="outline"
                onClick={handleCancel}
                disabled={saving}
              >
                Cancel
              </Button>
              <Button onClick={() => void handleSave()} disabled={saving}>
                {saving ? (
                  <>
                    <Spinner /> Saving
                  </>
                ) : (
                  "Save changes"
                )}
              </Button>
            </div>
          ) : (
            <Button variant="outline" onClick={handleEdit}>
              <Pencil className="size-4" />
              Edit profile
            </Button>
          )}
        </header>

        <section className="mt-6 overflow-hidden rounded-2xl border-2 border-slate-300 bg-white lg:grid lg:grid-cols-[280px_minmax(0,1fr)]">
          <aside className="border-b border-slate-200 bg-white p-6 lg:border-r lg:border-b-0">
            <div className="flex items-center gap-4 lg:block">
              <label
                className={`group relative flex size-16 shrink-0 items-center justify-center overflow-hidden rounded-full bg-primary text-lg font-semibold text-primary-foreground lg:size-20 lg:text-xl ${
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
                  <span className="absolute inset-0 flex items-center justify-center bg-slate-950/60 text-white">
                    <ImagePlus className="size-5" />
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
              <div className="min-w-0 lg:mt-5">
                <h2 className="truncate text-lg font-semibold text-slate-950">
                  {name}
                </h2>
                <p className="mt-1 break-words text-sm text-slate-500">
                  {user.email}
                </p>
              </div>
            </div>
            <div className="mt-6 border-t border-slate-200 pt-5">
              <span className="inline-flex items-center gap-1.5 rounded-full bg-emerald-50 px-2.5 py-1 text-xs font-medium text-emerald-700">
                <CheckCircle2 className="size-3.5" /> Active
              </span>
              <p className="mt-4 text-xs text-slate-500">Account type</p>
              <p className="mt-1 text-sm font-medium text-slate-900">
                {accountLabel}
              </p>
            </div>
            <div className="mt-6 border-t border-slate-200 pt-5">
              <p className="text-sm font-medium text-slate-900">Password</p>
              <p className="mt-1 text-xs leading-5 text-slate-500">
                Update your account password.
              </p>
              <Button
                variant="outline"
                className="mt-3 w-full"
                onClick={() => navigate("/profile/password")}
              >
                Reset password
              </Button>
            </div>
          </aside>

          <div className="min-w-0">
            <section className="p-6 sm:p-8">
              <h2 className="text-lg font-semibold text-slate-950">
                Personal information
              </h2>
              {editing ? (
                <div className="mt-5 grid gap-5 sm:grid-cols-2">
                  <div className="space-y-2">
                    <Label htmlFor="profile-name">Full name</Label>
                    <Input
                      id="profile-name"
                      value={name}
                      onChange={(event) => setName(event.target.value)}
                    />
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="profile-location">Home location</Label>
                    <Input
                      id="profile-location"
                      placeholder="City or district"
                      value={location}
                      onChange={(event) => setLocation(event.target.value)}
                    />
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="profile-nic">NIC number</Label>
                    <Input
                      id="profile-nic"
                      placeholder="Enter NIC for verification"
                      value={nic}
                      onChange={(event) => setNic(event.target.value)}
                    />
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="profile-gender">Gender</Label>
                    <Input
                      id="profile-gender"
                      placeholder="Select after NIC verification"
                      value={gender}
                      disabled
                    />
                  </div>
                  {saveError && (
                    <p className="rounded-xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700 sm:col-span-2">
                      {saveError}
                    </p>
                  )}
                </div>
              ) : (
                <dl className="mt-5 divide-y divide-slate-200 border-y border-slate-200">
                  <ProfileRow label="Full name" value={name} />
                  <ProfileRow
                    label="Home location"
                    value={location || "Not set"}
                  />
                  <ProfileRow
                    label="NIC number"
                    value={nic || "Not provided"}
                  />
                  <ProfileRow label="Gender" value={gender || "Not set"} />
                  <ProfileRow
                    label="Identity verification"
                    value={nic ? "Pending verification" : "Not completed"}
                  />
                </dl>
              )}
            </section>

            <section className="flex flex-col gap-4 border-t border-slate-200 p-6 sm:flex-row sm:items-center sm:justify-between sm:p-8">
              <div>
                <h2 className="font-semibold text-slate-950">Sign out</h2>
                <p className="mt-1 text-sm text-slate-500">
                  End your current session on this device.
                </p>
              </div>
              <Button variant="destructive" onClick={logout}>
                <LogOut /> Sign out
              </Button>
            </section>
          </div>
        </section>
      </div>
    </main>
  );
}

function ProfileRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="grid gap-1 py-4 sm:grid-cols-[180px_minmax(0,1fr)] sm:items-center">
      <dt className="text-sm text-slate-500">{label}</dt>
      <dd className="break-words text-sm font-medium text-slate-900 sm:text-right">
        {value}
      </dd>
    </div>
  );
}
