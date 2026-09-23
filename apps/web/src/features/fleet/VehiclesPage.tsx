import { useState } from "react";
import { Plus, Search, AlertCircle, Loader2 } from "lucide-react";
import { Link, useNavigate, useParams } from "react-router";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Checkbox } from "@/components/ui/checkbox";
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
  useVehicles,
  useVehicle,
  useCreateVehicle,
  useUpdateVehicle,
  useDeactivateVehicle,
} from "./hooks";
import type {
  VehicleType,
  VehicleStatus,
  VehicleDetail,
  CentreOption,
} from "./types";
import { extractErrorMessage } from "./services/error.utils";

export function VehiclesPage({
  centreId,
  basePath = "/fleet/vehicles",
  readOnly = false,
}: {
  centreId?: string;
  basePath?: string;
  readOnly?: boolean;
} = {}) {
  const { user } = useAuth();
  const [query, setQuery] = useState("");
  const {
    effectiveCentreId,
    centres,
    isLoading: centresLoading,
  } = useEffectiveCentreGuid(centreId ?? user?.centreId);

  const {
    data: vehicles = [],
    isLoading,
    isError,
    error,
    refetch,
  } = useVehicles({
    centreId: effectiveCentreId,
    search: query.trim() || undefined,
  });

  const centre = centres.find((item) => item.id === effectiveCentreId);

  return (
    <main className="flex flex-1 flex-col gap-4 bg-muted/20 p-4">
      <PageHeading
        title="Vehicles"
        description={
          readOnly
            ? "Read-only fleet assignments, compliance, and service history."
            : "A fleet workspace for availability, assignments, compliance, and service history."
        }
        action={
          readOnly ? (
            <Button
              variant="outline"
              render={
                <Link
                  to={
                    effectiveCentreId
                      ? `/admin/centres/${effectiveCentreId}`
                      : "/admin/centres"
                  }
                />
              }
            >
              Back to centre
            </Button>
          ) : (
            <Button render={<Link to="/fleet/vehicles/new" />}>
              <Plus /> Register vehicle
            </Button>
          )
        }
      />

      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <p className="text-sm text-muted-foreground">
          {vehicles.length} vehicle{vehicles.length === 1 ? "" : "s"} assigned
          to{" "}
          {centre?.name ??
            (effectiveCentreId ? "selected centre" : "all centres")}
        </p>
        <div className="relative w-full sm:max-w-xs">
          <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            value={query}
            onChange={(event) => setQuery(event.target.value)}
            className="h-9 bg-card pl-9"
            placeholder="Search registration or model"
          />
        </div>
      </div>

      {isError && (
        <div className="flex items-center justify-between rounded-lg border border-destructive/20 bg-destructive/10 p-4 text-sm text-destructive">
          <div className="flex items-center gap-2">
            <AlertCircle className="size-4" />
            <span>Failed to load vehicles: {extractErrorMessage(error)}</span>
          </div>
          <Button variant="outline" size="sm" onClick={() => refetch()}>
            Retry
          </Button>
        </div>
      )}

      {isLoading || centresLoading ? (
        <div className="flex items-center justify-center p-12 text-sm text-muted-foreground">
          <Loader2 className="mr-2 size-5 animate-spin" /> Loading vehicles...
        </div>
      ) : (
        <section className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">
          {vehicles.map((vehicle) => (
            <Link
              key={vehicle.id}
              to={`${basePath}/${vehicle.id}`}
              className="group flex rounded-lg border bg-card transition hover:border-primary/40 hover:shadow-sm"
            >
              <div className="flex w-24 shrink-0 items-center justify-center p-2">
                <img
                  src="/vehicle-placeholder.svg"
                  alt="UPTS bus placeholder"
                  className="size-20 rounded-md bg-muted object-contain"
                />
              </div>
              <div className="flex min-w-0 flex-1 flex-col py-2 pr-3">
                <div className="flex items-start justify-between gap-2">
                  <p className="font-semibold">{vehicle.plateNumber}</p>
                  <StatusBadge
                    label={vehicle.status}
                    tone={
                      vehicle.status === "Active"
                        ? "good"
                        : vehicle.status === "Maintenance"
                          ? "warning"
                          : "neutral"
                    }
                  />
                </div>
                <h2 className="mt-1 text-sm font-medium">{vehicle.model}</h2>
                <p className="text-xs text-muted-foreground">
                  {vehicle.type} · {vehicle.capacity} seats ·{" "}
                  {vehicle.isAccessible ? "Accessible" : "Standard"}
                </p>
                <div className="mt-2 border-t pt-2 flex items-center justify-between text-xs text-muted-foreground">
                  <span>{vehicle.centre}</span>
                  <span>
                    {vehicle.maintenanceCount > 0
                      ? `${vehicle.maintenanceCount} in maintenance`
                      : "Ready for service"}
                  </span>
                </div>
              </div>
            </Link>
          ))}
          {vehicles.length === 0 && (
            <div className="col-span-full rounded-lg border border-dashed bg-card p-8 text-center text-sm text-muted-foreground">
              {query.trim()
                ? "No vehicles match this search."
                : "No vehicles registered for this centre yet."}
            </div>
          )}
        </section>
      )}
    </main>
  );
}

export function VehicleProfilePage({
  basePath = "/fleet/vehicles",
  readOnly = false,
}: {
  centreId?: string;
  basePath?: string;
  readOnly?: boolean;
} = {}) {
  const { vehicleId } = useParams();
  const navigate = useNavigate();
  const {
    data: vehicle,
    isLoading,
    isError,
    error,
    refetch,
  } = useVehicle(vehicleId);
  const deactivateMutation = useDeactivateVehicle();
  const [actionError, setActionError] = useState<string | null>(null);

  if (isLoading) {
    return (
      <main className="flex flex-1 items-center justify-center p-12 text-sm text-muted-foreground">
        <Loader2 className="mr-2 size-5 animate-spin" /> Loading vehicle
        profile...
      </main>
    );
  }

  if (isError || !vehicle) {
    return (
      <main className="flex flex-1 flex-col items-center justify-center gap-4 p-8 text-center">
        <AlertCircle className="size-8 text-destructive" />
        <p className="text-sm text-destructive">
          {error ? extractErrorMessage(error) : "Vehicle not found."}
        </p>
        <div className="flex gap-2">
          <Button variant="outline" onClick={() => refetch()}>
            Retry
          </Button>
          <Button variant="outline" render={<Link to={basePath} />}>
            Back to vehicles
          </Button>
        </div>
      </main>
    );
  }

  async function handleDeactivate() {
    if (!vehicleId) return;
    if (
      !confirm(
        `Are you sure you want to deactivate vehicle ${vehicle?.plateNumber}?`,
      )
    ) {
      return;
    }
    setActionError(null);
    try {
      await deactivateMutation.mutateAsync(vehicleId);
      navigate(basePath);
    } catch (err) {
      setActionError(extractErrorMessage(err, "Failed to deactivate vehicle."));
    }
  }

  return (
    <main className="flex flex-1 flex-col gap-4 bg-muted/20 p-4">
      <PageHeading
        title={vehicle.plateNumber}
        description="Vehicle profile, current operating state, and maintenance readiness."
        action={
          readOnly ? (
            <Button variant="outline" render={<Link to={basePath} />}>
              Back to vehicles
            </Button>
          ) : (
            <div className="flex gap-2">
              <Button
                variant="outline"
                render={<Link to={`/fleet/vehicles/${vehicle.id}/edit`} />}
              >
                Edit vehicle
              </Button>
              {vehicle.status !== "Inactive" && (
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
          )
        }
      />

      {actionError && (
        <div className="rounded-lg border border-destructive/20 bg-destructive/10 p-3 text-sm text-destructive">
          {actionError}
        </div>
      )}

      <section className="grid gap-4 xl:grid-cols-[minmax(0,1.1fr)_minmax(330px,0.9fr)]">
        <article className="overflow-hidden rounded-lg border bg-card">
          <img
            src="/vehicle-placeholder.svg"
            alt={`Placeholder for ${vehicle.plateNumber}`}
            className="h-64 w-full object-cover"
          />
          <div className="grid gap-4 p-5 sm:grid-cols-3">
            <Info label="Registration" value={vehicle.plateNumber} />
            <Info label="Model" value={vehicle.model} />
            <Info label="Vehicle type" value={vehicle.type} />
            <Info
              label="Passenger capacity"
              value={`${vehicle.capacity} passengers`}
            />
            <Info
              label="Accessibility"
              value={
                vehicle.isAccessible
                  ? "Wheelchair accessible"
                  : "Standard access"
              }
            />
            <Info
              label="Assigned centre"
              value={vehicle.centre?.name ?? "Not assigned"}
            />
            <Info label="Operating status" value={vehicle.status} />
          </div>
        </article>

        <article className="rounded-lg border bg-card p-5">
          <h2 className="font-semibold">Current centre assignment</h2>
          <div className="mt-4 rounded-md bg-muted/60 p-4">
            <p className="text-sm font-medium">
              {vehicle.centre?.name ?? "Unassigned"}
            </p>
            <p className="mt-1 text-sm text-muted-foreground">
              Centre Code: {vehicle.centre?.code ?? "—"}
            </p>
            <p className="mt-3 text-xs text-muted-foreground">
              Vehicle status: {vehicle.status} · Available for centre dispatch
            </p>
          </div>

          <h2 className="mt-6 font-semibold">Operational readiness</h2>
          <div className="mt-3 space-y-3">
            <Meter
              label="Accessibility standard"
              value={vehicle.isAccessible ? "Compliant" : "Standard"}
              width={vehicle.isAccessible ? "100%" : "60%"}
            />
            <Meter
              label="Status readiness"
              value={vehicle.status === "Active" ? "Active" : vehicle.status}
              width={vehicle.status === "Active" ? "100%" : "40%"}
            />
          </div>
        </article>
      </section>

      <section className="grid gap-4 lg:grid-cols-2">
        <article className="rounded-lg border bg-card p-5">
          <div className="flex items-center justify-between">
            <h2 className="font-semibold">Maintenance history</h2>
            <Button
              variant="outline"
              size="sm"
              render={<Link to={`/fleet/maintenance/new`} />}
            >
              Schedule service
            </Button>
          </div>
          {vehicle.maintenance && vehicle.maintenance.length > 0 ? (
            <div className="mt-4 space-y-4 border-l pl-4 text-sm">
              {vehicle.maintenance.map((m) => (
                <div key={m.id} className="space-y-1">
                  <div className="flex items-center justify-between">
                    <p className="font-medium">
                      {new Date(m.scheduledFor).toLocaleDateString()} · {m.type}
                    </p>
                    <StatusBadge
                      label={m.status}
                      tone={
                        m.status === "Completed"
                          ? "good"
                          : m.status === "Cancelled"
                            ? "danger"
                            : m.status === "InProgress"
                              ? "warning"
                              : "neutral"
                      }
                    />
                  </div>
                  <p className="text-xs text-muted-foreground">
                    {m.description}
                  </p>
                  {m.completedAt && (
                    <p className="text-xs text-emerald-600">
                      Completed: {new Date(m.completedAt).toLocaleString()}
                    </p>
                  )}
                </div>
              ))}
            </div>
          ) : (
            <p className="mt-4 text-sm text-muted-foreground">
              No maintenance records for this vehicle.
            </p>
          )}
        </article>

        <article className="rounded-lg border bg-card p-5">
          <h2 className="font-semibold">Service profile</h2>
          <div className="mt-4 grid grid-cols-3 gap-3">
            <Info label="Seating" value={String(vehicle.capacity)} />
            <Info
              label="Accessibility"
              value={vehicle.isAccessible ? "Yes" : "No"}
            />
            <Info label="Status" value={vehicle.status} />
          </div>
        </article>
      </section>
    </main>
  );
}

export function VehicleFormPage() {
  const { vehicleId } = useParams();
  const { user } = useAuth();
  const editing = Boolean(vehicleId);

  const { data: existing, isLoading: existingLoading } = useVehicle(vehicleId);
  const { data: centres = [], isLoading: centresLoading } = useCentres();
  const { effectiveCentreId: userCentreGuid } = useEffectiveCentreGuid(
    user?.centreId,
  );

  if ((editing && existingLoading) || centresLoading) {
    return (
      <main className="flex flex-1 items-center justify-center p-12 text-sm text-muted-foreground">
        <Loader2 className="mr-2 size-5 animate-spin" /> Loading vehicle
        details...
      </main>
    );
  }

  const defaultCentreId =
    existing?.centreId ?? userCentreGuid ?? centres[0]?.id ?? "";

  return (
    <VehicleFormInner
      key={existing?.id ?? "new"}
      vehicleId={vehicleId}
      existing={existing}
      centres={centres}
      defaultCentreId={defaultCentreId}
    />
  );
}

function VehicleFormInner({
  vehicleId,
  existing,
  centres,
  defaultCentreId,
}: {
  vehicleId?: string;
  existing?: VehicleDetail;
  centres: CentreOption[];
  defaultCentreId: string;
}) {
  const navigate = useNavigate();
  const editing = Boolean(vehicleId);
  const createMutation = useCreateVehicle();
  const updateMutation = useUpdateVehicle();

  const [plateNumber, setPlateNumber] = useState(existing?.plateNumber ?? "");
  const [model, setModel] = useState(existing?.model ?? "");
  const [centreId, setCentreId] = useState(
    existing?.centreId ?? defaultCentreId,
  );
  const [type, setType] = useState<VehicleType>(existing?.type ?? "Normal");
  const [capacity, setCapacity] = useState(String(existing?.capacity ?? 52));
  const [isAccessible, setIsAccessible] = useState(
    existing?.isAccessible ?? false,
  );
  const [status, setStatus] = useState<VehicleStatus>(
    existing?.status ?? "Active",
  );
  const [formError, setFormError] = useState<string | null>(null);

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    setFormError(null);

    if (!plateNumber.trim()) {
      setFormError("Registration plate number is required.");
      return;
    }
    if (!model.trim()) {
      setFormError("Vehicle model is required.");
      return;
    }
    if (!centreId) {
      setFormError("Please select an assigned centre.");
      return;
    }

    const numericCapacity = parseInt(capacity, 10);
    if (isNaN(numericCapacity) || numericCapacity <= 0) {
      setFormError("Passenger capacity must be a positive number.");
      return;
    }

    const payload = {
      centreId,
      plateNumber: plateNumber.trim().toUpperCase(),
      model: model.trim(),
      type,
      capacity: numericCapacity,
      isAccessible,
      status,
    };

    try {
      if (editing && vehicleId) {
        await updateMutation.mutateAsync({ id: vehicleId, data: payload });
        navigate(`/fleet/vehicles/${vehicleId}`);
      } else {
        const created = await createMutation.mutateAsync(payload);
        navigate(`/fleet/vehicles/${created.id}`);
      }
    } catch (err) {
      setFormError(extractErrorMessage(err, "Failed to save vehicle."));
    }
  }

  const isSubmitting = createMutation.isPending || updateMutation.isPending;

  return (
    <main className="flex flex-1 flex-col gap-4 bg-muted/20 p-4">
      <PageHeading
        title={
          editing
            ? `Edit ${existing?.plateNumber ?? "Vehicle"}`
            : "Register vehicle"
        }
        description="All fields map directly to the backend Vehicle create and update APIs."
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
            Registration number
            <Input
              value={plateNumber}
              onChange={(e) => setPlateNumber(e.target.value)}
              placeholder="WP CAB-4821"
              required
            />
          </label>

          <label className="grid gap-1.5 text-sm font-medium">
            Vehicle model
            <Input
              value={model}
              onChange={(e) => setModel(e.target.value)}
              placeholder="Ashok Leyland Viking"
              required
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

          <label className="grid gap-1.5 text-sm font-medium">
            Vehicle type
            <Select
              value={type}
              onValueChange={(val) => val && setType(val as VehicleType)}
              itemToStringLabel={(value) =>
                ({
                  Normal: "Normal",
                  SemiLuxury: "Semi-luxury",
                  AcExpress: "AC express",
                })[value] ?? value
              }
            >
              <SelectTrigger className="w-full bg-muted/60">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="Normal">Normal</SelectItem>
                <SelectItem value="SemiLuxury">Semi-luxury</SelectItem>
                <SelectItem value="AcExpress">AC express</SelectItem>
              </SelectContent>
            </Select>
          </label>

          <label className="grid gap-1.5 text-sm font-medium">
            Seated passenger capacity
            <Input
              type="number"
              min="1"
              max="200"
              value={capacity}
              onChange={(e) => setCapacity(e.target.value)}
              placeholder="52"
              required
            />
          </label>

          <label className="grid gap-1.5 text-sm font-medium">
            Operating status
            <Select
              value={status}
              onValueChange={(val) => val && setStatus(val as VehicleStatus)}
              itemToStringLabel={(value) =>
                ({
                  Active: "Active",
                  Maintenance: "Maintenance",
                  Inactive: "Inactive",
                })[value] ?? value
              }
            >
              <SelectTrigger className="w-full bg-muted/60">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="Active">Active</SelectItem>
                <SelectItem value="Maintenance">Maintenance</SelectItem>
                <SelectItem value="Inactive">Inactive</SelectItem>
              </SelectContent>
            </Select>
          </label>
        </div>

        <label className="mt-4 flex items-center gap-2 text-sm font-medium">
          <Checkbox
            checked={isAccessible}
            onCheckedChange={(checked) => setIsAccessible(Boolean(checked))}
          />
          Wheelchair accessible
        </label>

        <p className="mt-3 text-xs text-muted-foreground">
          Centre, plate number, model, type, capacity, accessibility, and status
          are sent directly to the Vehicles API.
        </p>

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
                : "Registering..."
              : editing
                ? "Save changes"
                : "Register vehicle"}
          </Button>
        </div>
      </form>
    </main>
  );
}

function Info({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <p className="text-xs text-muted-foreground">{label}</p>
      <p className="mt-1 text-sm font-medium">{value}</p>
    </div>
  );
}

function Meter({
  label,
  value,
  width,
}: {
  label: string;
  value: string;
  width: string;
}) {
  return (
    <div>
      <div className="flex justify-between text-sm">
        <span>{label}</span>
        <span className="font-medium">{value}</span>
      </div>
      <div className="mt-2 h-1.5 rounded bg-muted">
        <div className="h-full rounded bg-primary" style={{ width }} />
      </div>
    </div>
  );
}
