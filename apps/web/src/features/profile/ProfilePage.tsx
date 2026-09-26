import { useEffect, useState } from "react";
import {
  CheckCircle2,
  ImagePlus,
  LogOut,
  Pencil,
} from "lucide-react";
import { useNavigate } from "react-router";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Spinner } from "@/components/ui/spinner";
import { useAuth } from "@/hooks/useAuth";
import { apiClient } from "@/lib/api/api-client";

function genderFromNic(value: string) {
  const normalized = value.trim().toUpperCase();
  const digits =
    normalized.length === 10
      ? normalized.slice(4, 7)
      : normalized.length >= 9
        ? normalized.slice(2, 5)
        : "";
  const day = Number(digits);
  return day >= 1 && day <= 866 ? (day > 500 ? "Female" : "Male") : "";
}

export function ProfilePage() {
  const { user, logout, setProfilePhotoUrl } = useAuth();
  const navigate = useNavigate();
  const [editing, setEditing] = useState(false);
  const [saving, setSaving] = useState(false);
  const [name, setName] = useState(user?.name ?? "");
  const [location, setLocation] = useState(user?.homeLocation ?? "");
  const [nic, setNic] = useState(user?.nicNumber ?? "");
  const [photo, setPhoto] = useState<string | null>(
    user?.profilePhotoUrl ?? null,
  );
  const [photoFile, setPhotoFile] = useState<File | null>(null);
  const [saveError, setSaveError] = useState<string | null>(null);
  const [editSnapshot, setEditSnapshot] = useState({
    name: user?.name ?? "",
    location: user?.homeLocation ?? "",
    nic: user?.nicNumber ?? "",
    photo: user?.profilePhotoUrl ?? null,
  });
  const gender = genderFromNic(nic) || user?.gender || "";
  async function handleSave() {
    setSaveError(null);
    setSaving(true);
    try {
      await apiClient(`/api/v1/auth/me`, {
        method: "PATCH",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          name,
          homeLocation: location,
          nicNumber: nic,
          gender,
        }),
      });
      let savedPhoto = photo;
      if (photoFile) {
        const form = new FormData();
        form.append("file", photoFile);
        const uploaded = await apiClient<{ profilePhotoUrl: string }>(
          "/api/v1/auth/me/profile-photo",
          { method: "POST", body: form },
        );
        savedPhoto = uploaded.profilePhotoUrl;
        setPhoto(uploaded.profilePhotoUrl);
        setProfilePhotoUrl(uploaded.profilePhotoUrl);
        setPhotoFile(null);
      }
      setEditSnapshot({ name, location, nic, photo: savedPhoto });
      setEditing(false);
    } catch (cause) {
      setSaveError(
        cause instanceof Error
          ? cause.message
          : "Could not update your profile.",
      );
    } finally {
      setSaving(false);
    }
  }

  function handleEdit() {
    setSaveError(null);
    setEditSnapshot({ name, location, nic, photo });
    setEditing(true);
  }

  function handleCancel() {
    setName(editSnapshot.name);
    setLocation(editSnapshot.location);
    setNic(editSnapshot.nic);
    setPhoto(editSnapshot.photo);
    setPhotoFile(null);
    setSaveError(null);
    setEditing(false);
  }

  useEffect(
    () => () => {
      if (photo?.startsWith("blob:")) URL.revokeObjectURL(photo);
    },
    [photo],
  );

  useEffect(() => {
    const value = nic.trim().toUpperCase();
    if (!editing || !/^(\d{9}[VX]|\d{12})$/.test(value)) return;
    void apiClient("/api/v1/auth/me/verify-nic", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ nicNumber: value }),
    });
  }, [editing, nic]);
  if (!user) return null;
  const initials = name
    .split(" ")
    .map((part) => part[0])
    .join("")
    .slice(0, 2)
    .toUpperCase();
  const accountLabel =
    user.role === "Commuter" ? "Commuter account" : user.role;
  function handlePhoto(event: React.ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0];
    if (!file) return;
    if (!["image/jpeg", "image/png", "image/webp"].includes(file.type)) {
      setSaveError("Choose a JPG, PNG, or WebP image.");
      event.target.value = "";
      return;
    }
    if (file.size > 5 * 1024 * 1024) {
      setSaveError("Profile images cannot exceed 5 MB.");
      event.target.value = "";
      return;
    }
    setSaveError(null);
    setPhotoFile(file);
    setPhoto(URL.createObjectURL(file));
  }

  return (
    <main className="min-h-screen bg-slate-50">
      <div className="mx-auto max-w-7xl px-5 py-8 sm:px-8">
        <header className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
          <h1 className="text-3xl font-semibold tracking-tight text-slate-950">
            My Profile
          </h1>
          {editing ? (
            <div className="flex items-center gap-3">
              <Button variant="outline" onClick={handleCancel} disabled={saving}>
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
                  <ProfileRow label="NIC number" value={nic || "Not provided"} />
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
