import { useState } from "react";
import { Link, useNavigate, useParams } from "react-router";
import { Plus, Search, AlertCircle, Loader2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { PageHeading } from "@/components/shared/PageHeading";
import { StatusBadge } from "@/components/shared/StatusBadge";
import { useAuth } from "@/hooks/useAuth";
import {
  useCentres,
  useEffectiveCentreGuid,
  useDrivers,
  useDriver,
  useCreateDriver,
  useUpdateDriver,
  useDeactivateDriver,
} from "./hooks";
import type { DriverStatus, DriverDetail, CentreOption } from "./types";
import { extractErrorMessage } from "./services/error.utils";

export function DriversPage() {
  const { user } = useAuth();
  const [query, setQuery] = useState("");
  const {
    effectiveCentreId,
    centres,
    isLoading: centresLoading,
  } = useEffectiveCentreGuid(user?.centreId);

  const {
    data: drivers = [],
    isLoading,
    isError,
    error,
    refetch,
  } = useDrivers({
    centreId: effectiveCentreId,
    search: query.trim() || undefined,
  });

  const centre = centres.find((item) => item.id === effectiveCentreId);

  return (
    <main className="flex flex-1 flex-col gap-4 bg-muted/20 p-4">
      <PageHeading
        title="Drivers"
        description="Centre-scoped driver identity, licence, duty eligibility, and trip assignment records."
        action={
          <Button render={<Link to="/fleet/drivers/new" />}>
            <Plus /> Add driver
          </Button>
        }
      />

      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <p className="text-sm text-muted-foreground">
          {drivers.length} driver{drivers.length === 1 ? "" : "s"} assigned to{" "}
          {centre?.name ??
            (effectiveCentreId ? "selected centre" : "all centres")}
        </p>
        <div className="relative w-full sm:max-w-xs">
          <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            className="h-9 bg-card pl-9"
            value={query}
            onChange={(event) => setQuery(event.target.value)}
            placeholder="Search name or licence"
          />
        </div>
      </div>

      {isError && (
        <div className="flex items-center justify-between rounded-lg border border-destructive/20 bg-destructive/10 p-4 text-sm text-destructive">
          <div className="flex items-center gap-2">
            <AlertCircle className="size-4" />
            <span>Failed to load drivers: {extractErrorMessage(error)}</span>
          </div>
          <Button variant="outline" size="sm" onClick={() => refetch()}>
            Retry
          </Button>
        </div>
      )}

      {isLoading || centresLoading ? (
        <div className="flex items-center justify-center p-12 text-sm text-muted-foreground">
          <Loader2 className="mr-2 size-5 animate-spin" /> Loading drivers...
        </div>
      ) : (
        <section className="overflow-hidden rounded-lg border bg-card">
          {drivers.map((driver) => (
            <Link
              key={driver.id}
              to={`/fleet/drivers/${driver.id}`}
              className="grid gap-2 border-b px-4 py-4 transition hover:bg-muted/40 md:grid-cols-[minmax(0,1fr)_180px_150px] md:items-center"
            >
              <div>
                <p className="font-medium">{driver.fullName}</p>
                <p className="mt-1 text-sm text-muted-foreground">
                  {driver.licenseNumber} ·{" "}
                  {driver.phoneNumber || "No phone recorded"}
                </p>
              </div>
              <p className="text-sm text-muted-foreground">{driver.centre}</p>
              <StatusBadge
                label={driver.status}
                tone={driver.status === "Active" ? "good" : "neutral"}
              />
            </Link>
          ))}
          {drivers.length === 0 && (
            <p className="p-8 text-center text-sm text-muted-foreground">
              {query.trim()
                ? "No drivers match this search."
                : "No drivers registered for this centre yet."}
            </p>
          )}
        </section>
      )}
    </main>
  );
}

export function DriverDetailPage() {
  const { driverId } = useParams();
  const navigate = useNavigate();
  const {
    data: driver,
    isLoading,
    isError,
    error,
    refetch,
  } = useDriver(driverId);
  const deactivateMutation = useDeactivateDriver();
  const [actionError, setActionError] = useState<string | null>(null);

  if (isLoading) {
    return (
      <main className="flex flex-1 items-center justify-center p-12 text-sm text-muted-foreground">
        <Loader2 className="mr-2 size-5 animate-spin" /> Loading driver
        profile...
      </main>
    );
  }

  if (isError || !driver) {
    return (
      <main className="flex flex-1 flex-col items-center justify-center gap-4 p-8 text-center">
        <AlertCircle className="size-8 text-destructive" />
        <p className="text-sm text-destructive">
          {error ? extractErrorMessage(error) : "Driver not found."}
        </p>
        <div className="flex gap-2">
          <Button variant="outline" onClick={() => refetch()}>
            Retry
          </Button>
          <Button variant="outline" render={<Link to="/fleet/drivers" />}>
            Back to drivers
          </Button>
        </div>
      </main>
    );
  }

  async function handleDeactivate() {
    if (!driverId) return;
    if (
      !confirm(
        `Are you sure you want to deactivate driver ${driver?.fullName}?`,
      )
    ) {
      return;
    }
    setActionError(null);
    try {
      await deactivateMutation.mutateAsync(driverId);
      navigate("/fleet/drivers");
    } catch (err) {
      setActionError(extractErrorMessage(err, "Failed to deactivate driver."));
    }
  }

  return (
    <main className="flex flex-1 flex-col gap-4 bg-muted/20 p-4">
      <PageHeading
        title={driver.fullName}
        description="Driver profile and API-ready operational identity."
        action={
          <div className="flex gap-2">
            <Button
              variant="outline"
              render={<Link to={`/fleet/drivers/${driver.id}/edit`} />}
            >
              Edit driver
            </Button>
            {driver.status !== "Inactive" && (
              <Button
                variant="destructive"
                disabled={deactivateMutation.isPending}
                onClick={handleDeactivate}
              >
                {deactivateMutation.isPending
                  ? "Deactivating..."
                  : "Deactivate"}
              </Button>
            )}
          </div>
        }
      />

      {actionError && (
        <div className="rounded-lg border border-destructive/20 bg-destructive/10 p-3 text-sm text-destructive">
          {actionError}
        </div>
      )}

      <section className="max-w-3xl rounded-lg border bg-card p-5">
        <div className="flex items-start justify-between">
          <div>
            <p className="text-sm text-muted-foreground">Driver reference</p>
            <p className="mt-1 font-mono text-sm">{driver.id}</p>
          </div>
          <StatusBadge
            label={driver.status}
            tone={driver.status === "Active" ? "good" : "neutral"}
          />
        </div>

        <div className="mt-6 grid gap-5 border-t pt-5 sm:grid-cols-2">
          <Fact label="Full name" value={driver.fullName} />
          <Fact
            label="Phone number"
            value={driver.phoneNumber || "Not provided"}
          />
          <Fact label="Licence number" value={driver.licenseNumber} />
          <Fact label="Assigned centre" value={driver.centre?.name ?? "—"} />
        </div>

        <p className="mt-6 rounded-md bg-muted/50 p-3 text-sm text-muted-foreground">
          Driver record is synced with the backend PostgreSQL database. Trip
          duty and assignment are managed in Dispatch.
        </p>
      </section>
    </main>
  );
}

export function DriverFormPage() {
  const { driverId } = useParams();
  const editing = Boolean(driverId);
  const { user } = useAuth();

  const { data: existing, isLoading: existingLoading } = useDriver(driverId);
  const { data: centres = [], isLoading: centresLoading } = useCentres();
  const { effectiveCentreId: userCentreGuid } = useEffectiveCentreGuid(
    user?.centreId,
  );

  if ((editing && existingLoading) || centresLoading) {
    return (
      <main className="flex flex-1 items-center justify-center p-12 text-sm text-muted-foreground">
        <Loader2 className="mr-2 size-5 animate-spin" /> Loading driver
        details...
      </main>
    );
  }

  const defaultCentreId =
    existing?.centreId ?? userCentreGuid ?? centres[0]?.id ?? "";

  return (
    <DriverFormInner
      key={existing?.id ?? "new"}
      driverId={driverId}
      existing={existing}
      centres={centres}
      defaultCentreId={defaultCentreId}
    />
  );
}

function DriverFormInner({
  driverId,
  existing,
  centres,
  defaultCentreId,
}: {
  driverId?: string;
  existing?: DriverDetail;
  centres: CentreOption[];
  defaultCentreId: string;
}) {
  const navigate = useNavigate();
  const editing = Boolean(driverId);
  const createMutation = useCreateDriver();
  const updateMutation = useUpdateDriver();

  const [fullName, setFullName] = useState(existing?.fullName ?? "");
  const [phoneNumber, setPhoneNumber] = useState(existing?.phoneNumber ?? "");
  const [centreId, setCentreId] = useState(
    existing?.centreId ?? defaultCentreId,
  );
  const [licenseNumber, setLicenseNumber] = useState(
    existing?.licenseNumber ?? "",
  );
  const [status, setStatus] = useState<DriverStatus>(
    existing?.status ?? "Active",
  );
  const [formError, setFormError] = useState<string | null>(null);

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    setFormError(null);

    if (!fullName.trim()) {
      setFormError("Full name is required.");
      return;
    }
    if (!centreId) {
      setFormError("Please select an assigned centre.");
      return;
    }
    if (!editing && !licenseNumber.trim()) {
      setFormError("Licence number is required.");
      return;
    }

    try {
      if (editing && driverId) {
        // Update body strictly does NOT include licenseNumber
        await updateMutation.mutateAsync({
          id: driverId,
          data: {
            centreId,
            fullName: fullName.trim(),
            phoneNumber: phoneNumber.trim() || null,
            status,
          },
        });
        navigate(`/fleet/drivers/${driverId}`);
      } else {
        const created = await createMutation.mutateAsync({
          centreId,
          fullName: fullName.trim(),
          phoneNumber: phoneNumber.trim() || null,
          licenseNumber: licenseNumber.trim().toUpperCase(),
          status,
        });
        navigate(`/fleet/drivers/${created.id}`);
      }
    } catch (err) {
      setFormError(extractErrorMessage(err, "Failed to save driver record."));
    }
  }

  const isSubmitting = createMutation.isPending || updateMutation.isPending;

  return (
    <main className="flex flex-1 flex-col gap-4 bg-muted/20 p-4">
      <PageHeading
        title={
          editing ? `Edit ${existing?.fullName ?? "Driver"}` : "Add driver"
        }
        description={
          editing
            ? "Update fields map directly to UpdateDriverRequest (licence is immutable)."
            : "Fields map directly to CreateDriverRequest."
        }
      />

      <form
        className="max-w-3xl rounded-lg border bg-card p-5"
        onSubmit={handleSubmit}
      >
        {formError && (
          <div className="mb-4 flex items-center gap-2 rounded-md border border-destructive/20 bg-destructive/10 p-3 text-sm text-destructive">
            <AlertCircle className="size-4 shrink-0" />
            <span>{formError}</span>
          </div>
        )}

        <div className="grid gap-4 sm:grid-cols-2">
          <label className="grid gap-1.5 text-sm font-medium">
            Full name
            <Input
              value={fullName}
              onChange={(e) => setFullName(e.target.value)}
              placeholder="Driver full name"
              required
            />
          </label>

          <label className="grid gap-1.5 text-sm font-medium">
            Phone number
            <Input
              value={phoneNumber}
              onChange={(e) => setPhoneNumber(e.target.value)}
              placeholder="077 123 4567"
            />
          </label>

          <label className="grid gap-1.5 text-sm font-medium">
            Assigned centre
            <Select
              value={centreId}
              onValueChange={(val) => val && setCentreId(val)}
              itemToStringLabel={(value) =>
                centres.find((centre) => centre.id === value)?.name ?? value
              }
            >
              <SelectTrigger className="w-full bg-muted/60">
                <SelectValue placeholder="Select centre" />
              </SelectTrigger>
              <SelectContent>
                {centres.map((c) => (
                  <SelectItem key={c.id} value={c.id}>
                    {c.name}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </label>

          {!editing ? (
            <label className="grid gap-1.5 text-sm font-medium">
              Licence number
              <Input
                value={licenseNumber}
                onChange={(e) => setLicenseNumber(e.target.value)}
                placeholder="B-457829"
                required
              />
            </label>
          ) : (
            <Fact
              label="Licence number (immutable)"
              value={existing?.licenseNumber ?? "—"}
            />
          )}

          <label className="grid gap-1.5 text-sm font-medium">
            Duty eligibility
            <Select
              value={status}
              onValueChange={(val) => val && setStatus(val as DriverStatus)}
              itemToStringLabel={(value) =>
                ({ Active: "Active", Inactive: "Inactive" })[value] ?? value
              }
            >
              <SelectTrigger className="w-full bg-muted/60">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="Active">Active</SelectItem>
                <SelectItem value="Inactive">Inactive</SelectItem>
              </SelectContent>
            </Select>
          </label>
        </div>

        {editing && (
          <p className="mt-4 text-xs text-muted-foreground">
            Licence number is immutable in the UpdateDriverRequest backend
            contract. Create a corrected driver record if it was entered
            incorrectly.
          </p>
        )}

        <div className="mt-5 flex justify-end gap-2">
          <Button
            type="button"
            variant="outline"
            onClick={() => navigate(-1)}
            disabled={isSubmitting}
          >
            Cancel
          </Button>
          <Button type="submit" disabled={isSubmitting}>
            {isSubmitting
              ? editing
                ? "Saving changes..."
                : "Creating driver..."
              : editing
                ? "Save changes"
                : "Create driver"}
          </Button>
        </div>
      </form>
    </main>
  );
}

function Fact({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <p className="text-xs text-muted-foreground">{label}</p>
      <p className="mt-1 text-sm font-medium">{value}</p>
    </div>
  );
}
