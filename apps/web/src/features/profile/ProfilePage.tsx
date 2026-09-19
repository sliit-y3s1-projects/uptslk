import { useEffect, useState } from "react";
import {
  ArrowLeft,
  CheckCircle2,
  ImagePlus,
  LogOut,
  Mail,
  MapPin,
  Pencil,
  ShieldCheck,
} from "lucide-react";
import { Link, useNavigate } from "react-router";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useAuth } from "@/hooks/useAuth";
import { apiClient } from "@/lib/api/api-client";

export function ProfilePage() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const [editing, setEditing] = useState(false);
  const [name, setName] = useState(user?.name ?? "");
  const [location, setLocation] = useState(user?.homeLocation ?? "");
  const [nic, setNic] = useState(user?.nicNumber ?? "");
  const [gender, _setGender] = useState(user?.gender ?? "");
  const [photo, setPhoto] = useState<string | null>(null);
  useEffect(() => {
    if (editing || name === user?.name || !user) return;
    void apiClient(`/api/v1/auth/me`, {
      method: "PATCH",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        name,
        homeLocation: location,
        nicNumber: nic,
        gender,
      }),
    });
  }, [editing, name, location, nic, gender, user]);
  useEffect(() => {
    const value = nic.trim().toUpperCase();
    const digits =
      value.length === 10
        ? value.slice(4, 7)
        : value.length >= 9
          ? value.slice(2, 5)
          : "";
    const day = Number(digits);
    if (day >= 1 && day <= 866) _setGender(day > 500 ? "Female" : "Male");
  }, [nic]);
  useEffect(() => {
    const value = nic.trim().toUpperCase();
    if (!editing || !/^(\d{9}[VX]|\d{12})$/.test(value)) return;
    void apiClient("/api/v1/auth/me/verify-nic", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ nicNumber: value }),
    });
  }, [editing, nic]);
  useEffect(() => {
    const handler = (event: MouseEvent) => {
      const button = (event.target as HTMLElement).closest("button");
      if (button?.textContent?.trim() === "Reset password")
        navigate("/profile/password");
    };
    document.addEventListener("click", handler);
    return () => document.removeEventListener("click", handler);
  }, [navigate]);
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
    if (file) setPhoto(URL.createObjectURL(file));
  }

  return (
    <main className="min-h-screen bg-slate-50">
      <header className="border-b border-slate-200 bg-white">
        <div className="mx-auto flex max-w-7xl items-center justify-between px-5 py-4 sm:px-8">
          <Link
            to="/book"
            className="text-xl font-semibold tracking-tight text-slate-900"
          >
            UPTSLK{" "}
            <span className="font-normal text-slate-500">Seat Reservation</span>
          </Link>
          <Button
            variant="outline"
            className="rounded-full hover:border-red-200 hover:bg-red-50 hover:text-red-600"
            onClick={logout}
          >
            Sign out
          </Button>
        </div>
      </header>
      <div className="mx-auto max-w-7xl px-5 py-8 sm:px-8">
        <Link
          to="/book"
          className="inline-flex items-center gap-2 text-sm font-medium text-primary"
        >
          <ArrowLeft className="size-4" /> Back to booking
        </Link>
        <div className="mt-7 grid gap-5 lg:grid-cols-[minmax(0,1.1fr)_minmax(0,0.9fr)]">
          <section className="min-w-0 rounded-2xl border border-slate-200 bg-white">
            <div className="flex items-start justify-between border-b border-slate-200 p-6 sm:p-8">
              <div>
                <p className="text-sm font-medium text-primary">
                  Account settings
                </p>
                <h1 className="mt-2 text-3xl font-semibold tracking-tight text-slate-950">
                  My Profile
                </h1>
                <p className="mt-2 text-sm text-slate-500">
                  Update your personal details and identity information.
                </p>
              </div>
              <Button
                variant="outline"
                className="rounded-full"
                onClick={() => setEditing((value) => !value)}
              >
                <Pencil className="size-4" />{" "}
                {editing ? "Done" : "Edit profile"}
              </Button>
            </div>
            <div className="flex flex-wrap items-center gap-4 border-b border-slate-200 p-6 sm:p-8">
              <label className="group relative flex size-16 shrink-0 cursor-pointer items-center justify-center overflow-hidden rounded-full bg-primary text-lg font-semibold text-primary-foreground">
                <span>
                  {photo ? (
                    <img
                      src={photo}
                      alt="Profile"
                      className="size-full object-cover"
                    />
                  ) : (
                    initials
                  )}
                </span>
                {editing && (
                  <span className="absolute inset-0 flex items-center justify-center bg-slate-950/60 text-white">
                    <ImagePlus className="size-5" />
                  </span>
                )}
                <input
                  type="file"
                  accept="image/*"
                  className="sr-only"
                  onChange={handlePhoto}
                  disabled={!editing}
                />
              </label>
              <div className="min-w-0">
                <h2 className="truncate text-xl font-semibold text-slate-950">
                  {name}
                </h2>
                <p className="mt-1 text-sm text-slate-500">{accountLabel}</p>
              </div>
              <span className="ml-auto inline-flex items-center gap-1.5 rounded-full bg-emerald-50 px-3 py-1.5 text-xs font-medium text-emerald-700">
                <CheckCircle2 className="size-3.5" /> Active
              </span>
            </div>
            {editing ? (
              <div className="grid gap-5 p-6 sm:grid-cols-2 sm:p-8">
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
                    disabled
                  />
                </div>
                <p className="text-xs text-slate-500 sm:col-span-2">
                  Identity details are used to personalize bookings. NIC
                  verification and profile updates will be submitted securely
                  when the account service is connected.
                </p>
              </div>
            ) : (
              <dl className="grid gap-6 p-6 sm:grid-cols-2 sm:p-8">
                <div>
                  <dt className="flex items-center gap-2 text-xs font-medium uppercase tracking-wide text-slate-500">
                    <Mail className="size-4" /> Email address
                  </dt>
                  <dd className="mt-2 break-words text-sm font-medium text-slate-900">
                    {user.email}
                  </dd>
                </div>
                <div>
                  <dt className="flex items-center gap-2 text-xs font-medium uppercase tracking-wide text-slate-500">
                    <ShieldCheck className="size-4" /> Account type
                  </dt>
                  <dd className="mt-2 text-sm font-medium text-slate-900">
                    {accountLabel}
                  </dd>
                </div>
                <div>
                  <dt className="flex items-center gap-2 text-xs font-medium uppercase tracking-wide text-slate-500">
                    <MapPin className="size-4" /> Home location
                  </dt>
                  <dd className="mt-2 text-sm text-slate-500">
                    {location || "Not set"}
                  </dd>
                </div>
                <div>
                  <dt className="text-xs font-medium uppercase tracking-wide text-slate-500">
                    Identity verification
                  </dt>
                  <dd className="mt-2 text-sm text-slate-500">
                    {nic ? "Pending verification" : "Not completed"}
                  </dd>
                </div>
              </dl>
            )}
          </section>
          <aside className="min-w-0 space-y-5">
            <section className="rounded-2xl border border-slate-200 bg-white p-6">
              <div className="flex size-10 items-center justify-center rounded-xl bg-primary/10 text-primary">
                <ShieldCheck className="size-5" />
              </div>
              <h2 className="mt-4 font-semibold text-slate-950">
                Password &amp; security
              </h2>
              <p className="mt-2 text-sm leading-6 text-slate-500">
                Keep your account secure by resetting your password regularly.
              </p>
              <Button variant="outline" className="mt-5 w-full rounded-full">
                Reset password
              </Button>
            </section>
            <section className="rounded-2xl border border-slate-200 bg-white p-6">
              <h2 className="font-semibold text-slate-950">
                Need to switch accounts?
              </h2>
              <p className="mt-2 text-sm text-slate-500">
                Sign out here and continue with a different UPTSLK account.
              </p>
              <Button
                variant="outline"
                className="mt-5 w-full rounded-full hover:border-red-200 hover:bg-red-50 hover:text-red-600"
                onClick={logout}
              >
                <LogOut /> Sign out
              </Button>
            </section>
          </aside>
        </div>
      </div>
    </main>
  );
}
