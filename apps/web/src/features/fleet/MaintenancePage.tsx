import { useState } from "react";
import { Link, useNavigate, useParams } from "react-router";
import { Plus, Search, AlertCircle, Loader2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { PageHeading } from "@/components/shared/PageHeading";
import { StatusBadge } from "@/components/shared/StatusBadge";
import { useAuth } from "@/hooks/useAuth";
import {
  useEffectiveCentreGuid,
  useVehicles,
  useMaintenanceRecords,
  useMaintenanceRecord,
  useCreateMaintenanceRecord,
  useUpdateMaintenanceRecord,
  useCancelMaintenanceRecord,
} from "./hooks";
import type { MaintenanceStatus, MaintenanceDetail, VehicleListItem } from "./types";
import { extractErrorMessage } from "./services/error.utils";

export function MaintenancePage() {
  const { user } = useAuth();
  const [query, setQuery] = useState("");
  const { effectiveCentreId } = useEffectiveCentreGuid(user?.centreId);

  const {
    data: records = [],
    isLoading,
    isError,
    error,
    refetch,
  } = useMaintenanceRecords({
    centreId: effectiveCentreId,
  });

  const filteredRecords = records.filter((record) => {
    if (!query.trim()) return true;
    const term = query.trim().toLowerCase();
    return (
      record.vehicle.toLowerCase().includes(term) ||
      record.type.toLowerCase().includes(term) ||
      record.description.toLowerCase().includes(term) ||
      record.status.toLowerCase().includes(term)
    );
  });

  return (
    <main className="flex flex-1 flex-col gap-4 bg-muted/20 p-4">
      <PageHeading
        title="Maintenance"
        description="Vehicle service records with scheduling, status, completion, and cancellation history."
        action={
          <Button render={<Link to="/fleet/maintenance/new" />}>
            <Plus /> Schedule maintenance
          </Button>
        }
      />

      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <p className="text-sm text-muted-foreground">
          {filteredRecords.length} maintenance record{filteredRecords.length === 1 ? "" : "s"}
        </p>
        <div className="relative w-full sm:max-w-xs">
          <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            className="h-9 bg-card pl-9"
            value={query}
            onChange={(event) => setQuery(event.target.value)}
            placeholder="Search vehicle or maintenance type"
          />
        </div>
      </div>

      {isError && (
        <div className="flex items-center justify-between rounded-lg border border-destructive/20 bg-destructive/10 p-4 text-sm text-destructive">
          <div className="flex items-center gap-2">
            <AlertCircle className="size-4" />
            <span>Failed to load maintenance records: {extractErrorMessage(error)}</span>
          </div>
          <Button variant="outline" size="sm" onClick={() => refetch()}>
            Retry
          </Button>
        </div>
      )}

      {isLoading ? (
        <div className="flex items-center justify-center p-12 text-sm text-muted-foreground">
          <Loader2 className="mr-2 size-5 animate-spin" /> Loading maintenance records...
        </div>
      ) : (
        <section className="overflow-hidden rounded-lg border bg-card">
          {filteredRecords.map((record) => (
            <Link
              key={record.id}
              to={`/fleet/maintenance/${record.id}`}
              className="grid gap-2 border-b px-4 py-4 transition hover:bg-muted/40 md:grid-cols-[minmax(0,1fr)_180px_150px] md:items-center"
            >
              <div>
                <p className="font-medium">{record.type}</p>
                <p className="mt-1 text-sm text-muted-foreground">
                  <span className="font-semibold text-foreground">{record.vehicle}</span> · {record.description}
                </p>
              </div>
              <p className="text-sm text-muted-foreground">
                {new Date(record.scheduledFor).toLocaleDateString()}{" "}
                {new Date(record.scheduledFor).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" })}
              </p>
              <StatusBadge label={record.status} tone={tone(record.status)} />
            </Link>
          ))}
          {filteredRecords.length === 0 && (
            <p className="p-8 text-center text-sm text-muted-foreground">
              {query.trim()
                ? "No maintenance records match this search."
                : "No maintenance records scheduled yet."}
            </p>
          )}
        </section>
      )}
    </main>
  );
}

export function MaintenanceDetailPage() {
  const { recordId } = useParams();
  const navigate = useNavigate();
  const { data: record, isLoading, isError, error, refetch } = useMaintenanceRecord(recordId);
  const cancelMutation = useCancelMaintenanceRecord();
  const [actionError, setActionError] = useState<string | null>(null);

  if (isLoading) {
    return (
      <main className="flex flex-1 items-center justify-center p-12 text-sm text-muted-foreground">
        <Loader2 className="mr-2 size-5 animate-spin" /> Loading maintenance record...
      </main>
    );
  }

  if (isError || !record) {
    return (
      <main className="flex flex-1 flex-col items-center justify-center gap-4 p-8 text-center">
        <AlertCircle className="size-8 text-destructive" />
        <p className="text-sm text-destructive">
          {error ? extractErrorMessage(error) : "Maintenance record not found."}
        </p>
        <div className="flex gap-2">
          <Button variant="outline" onClick={() => refetch()}>
            Retry
          </Button>
          <Button variant="outline" render={<Link to="/fleet/maintenance" />}>
            Back to records
          </Button>
        </div>
      </main>
    );
  }

  async function handleCancel() {
    if (!recordId) return;
    if (!confirm(`Are you sure you want to cancel this ${record?.type} maintenance record?`)) {
      return;
    }
    setActionError(null);
    try {
      await cancelMutation.mutateAsync(recordId);
      navigate("/fleet/maintenance");
    } catch (err) {
      setActionError(extractErrorMessage(err, "Failed to cancel maintenance record."));
    }
  }

  const canCancel = record.status !== "Completed" && record.status !== "Cancelled";

  return (
    <main className="flex flex-1 flex-col gap-4 bg-muted/20 p-4">
      <PageHeading
        title={record.type}
        description={`${record.vehicle?.plateNumber ?? "Vehicle"} maintenance record`}
        action={
          <div className="flex gap-2">
            <Button variant="outline" render={<Link to={`/fleet/maintenance/${record.id}/edit`} />}>
              Edit record
            </Button>
            {canCancel && (
              <Button
                variant="destructive"
                disabled={cancelMutation.isPending}
                onClick={handleCancel}
              >
                {cancelMutation.isPending ? "Cancelling..." : "Cancel maintenance"}
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
            <p className="text-sm text-muted-foreground">Record reference</p>
            <p className="mt-1 font-mono text-sm">{record.id}</p>
          </div>
          <StatusBadge label={record.status} tone={tone(record.status)} />
        </div>

        <div className="mt-6 grid gap-5 border-t pt-5 sm:grid-cols-2">
          <Fact
            label="Vehicle"
            value={`${record.vehicle?.plateNumber ?? "—"} (${record.vehicle?.model ?? ""})`}
          />
          <Fact
            label="Scheduled for"
            value={`${new Date(record.scheduledFor).toLocaleDateString()} ${new Date(record.scheduledFor).toLocaleTimeString()}`}
          />
          <Fact
            label="Completed at"
            value={
              record.completedAt
                ? `${new Date(record.completedAt).toLocaleDateString()} ${new Date(record.completedAt).toLocaleTimeString()}`
                : "Not completed"
            }
          />
          <Fact label="Status" value={record.status} />
        </div>

        <div className="mt-5 border-t pt-4">
          <p className="text-xs text-muted-foreground">Description</p>
          <p className="mt-1 text-sm">{record.description}</p>
        </div>
      </section>
    </main>
  );
}

export function MaintenanceFormPage() {
  const { recordId } = useParams();
  const editing = Boolean(recordId);
  const { user } = useAuth();

  const { data: existing, isLoading: existingLoading } = useMaintenanceRecord(recordId);
  const { effectiveCentreId: userCentreGuid } = useEffectiveCentreGuid(user?.centreId);

  const { data: vehicles = [], isLoading: vehiclesLoading } = useVehicles({
    centreId: userCentreGuid,
  });

  if ((editing && existingLoading) || vehiclesLoading) {
    return (
      <main className="flex flex-1 items-center justify-center p-12 text-sm text-muted-foreground">
        <Loader2 className="mr-2 size-5 animate-spin" /> Loading maintenance details...
      </main>
    );
  }

  return (
    <MaintenanceFormInner
      key={existing?.id ?? "new"}
      recordId={recordId}
      existing={existing}
      vehicles={vehicles}
    />
  );
}

function MaintenanceFormInner({
  recordId,
  existing,
  vehicles,
}: {
  recordId?: string;
  existing?: MaintenanceDetail;
  vehicles: VehicleListItem[];
}) {
  const navigate = useNavigate();
  const editing = Boolean(recordId);

  const createMutation = useCreateMaintenanceRecord();
  const updateMutation = useUpdateMaintenanceRecord();

  const getDefaultScheduledFor = () => {
    if (existing?.scheduledFor) {
      return toDateTimeLocalString(existing.scheduledFor);
    }
    const tomorrow = new Date();
    tomorrow.setDate(tomorrow.getDate() + 1);
    tomorrow.setHours(9, 0, 0, 0);
    return toDateTimeLocalString(tomorrow.toISOString());
  };

  const [vehicleId, setVehicleId] = useState(existing?.vehicleId ?? vehicles[0]?.id ?? "");
  const [type, setType] = useState(existing?.type ?? "");
  const [scheduledFor, setScheduledFor] = useState(getDefaultScheduledFor());
  const [description, setDescription] = useState(existing?.description ?? "");
  const [status, setStatus] = useState<MaintenanceStatus>(existing?.status ?? "Scheduled");
  const [completedAt, setCompletedAt] = useState(toDateTimeLocalString(existing?.completedAt));
  const [formError, setFormError] = useState<string | null>(null);

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    setFormError(null);

    if (!type.trim()) {
      setFormError("Maintenance type is required.");
      return;
    }
    if (!scheduledFor) {
      setFormError("Scheduled date and time is required.");
      return;
    }
    if (!description.trim()) {
      setFormError("Description is required.");
      return;
    }

    if (editing) {
      if (status === "Completed" && !completedAt) {
        setFormError("Completed maintenance requires a completion time.");
        return;
      }

      try {
        await updateMutation.mutateAsync({
          id: recordId!,
          data: {
            type: type.trim(),
            description: description.trim(),
            status,
            scheduledFor: new Date(scheduledFor).toISOString(),
            completedAt: completedAt ? new Date(completedAt).toISOString() : null,
          },
        });
        navigate(`/fleet/maintenance/${recordId}`);
      } catch (err) {
        setFormError(extractErrorMessage(err, "Failed to update maintenance record."));
      }
    } else {
      if (!vehicleId) {
        setFormError("Please select a vehicle.");
        return;
      }

      try {
        const created = await createMutation.mutateAsync({
          vehicleId,
          type: type.trim(),
          description: description.trim(),
          scheduledFor: new Date(scheduledFor).toISOString(),
        });
        navigate(`/fleet/maintenance/${created.id}`);
      } catch (err) {
        setFormError(extractErrorMessage(err, "Failed to schedule maintenance."));
      }
    }
  }

  const isSubmitting = createMutation.isPending || updateMutation.isPending;

  return (
    <main className="flex flex-1 flex-col gap-4 bg-muted/20 p-4">
      <PageHeading
        title={editing ? `Edit ${existing?.type ?? "Maintenance"}` : "Schedule maintenance"}
        description={
          editing
            ? "Fields map directly to UpdateMaintenanceRecordRequest."
            : "Fields map directly to CreateMaintenanceRecordRequest."
        }
      />

      <form className="max-w-3xl rounded-lg border bg-card p-5" onSubmit={handleSubmit}>
        {formError && (
          <div className="mb-4 flex items-center gap-2 rounded-md border border-destructive/20 bg-destructive/10 p-3 text-sm text-destructive">
            <AlertCircle className="size-4 shrink-0" />
            <span>{formError}</span>
          </div>
        )}

        <div className="grid gap-4 sm:grid-cols-2">
          {!editing ? (
            <label className="grid gap-1.5 text-sm font-medium">
              Vehicle
              <Select value={vehicleId} onValueChange={(val) => val && setVehicleId(val)}>
                <SelectTrigger className="w-full bg-muted/60">
                  <SelectValue placeholder="Select vehicle" />
                </SelectTrigger>
                <SelectContent>
                  {vehicles.map((v) => (
                    <SelectItem key={v.id} value={v.id}>
                      {v.plateNumber} ({v.model})
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </label>
          ) : (
            <Fact
              label="Vehicle"
              value={`${existing?.vehicle?.plateNumber ?? "—"} (${existing?.vehicle?.model ?? ""})`}
            />
          )}

          <label className="grid gap-1.5 text-sm font-medium">
            Maintenance type
            <Input
              value={type}
              onChange={(e) => setType(e.target.value)}
              placeholder="Annual inspection, Oil service, Brake check..."
              required
            />
          </label>

          <label className="grid gap-1.5 text-sm font-medium">
            Scheduled for
            <Input
              type="datetime-local"
              value={scheduledFor}
              onChange={(e) => setScheduledFor(e.target.value)}
              required
            />
          </label>

          {editing && (
            <>
              <label className="grid gap-1.5 text-sm font-medium">
                Maintenance status
                <Select
                  value={status}
                  onValueChange={(val) => val && setStatus(val as MaintenanceStatus)}
                >
                  <SelectTrigger className="w-full bg-muted/60">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="Scheduled">Scheduled</SelectItem>
                    <SelectItem value="InProgress">In progress</SelectItem>
                    <SelectItem value="Completed">Completed</SelectItem>
                    <SelectItem value="Cancelled">Cancelled</SelectItem>
                  </SelectContent>
                </Select>
              </label>

              <label className="grid gap-1.5 text-sm font-medium">
                Completed at {status === "Completed" && <span className="text-destructive">*</span>}
                <Input
                  type="datetime-local"
                  value={completedAt}
                  onChange={(e) => setCompletedAt(e.target.value)}
                  required={status === "Completed"}
                />
              </label>
            </>
          )}

          <label className="grid gap-1.5 text-sm font-medium sm:col-span-2">
            Description
            <Input
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder="Describe the service work required"
              required
            />
          </label>
        </div>

        {editing && (
          <p className="mt-4 text-xs text-muted-foreground">
            The API requires a completion time whenever status is set to Completed.
          </p>
        )}

        <div className="mt-5 flex justify-end gap-2">
          <Button type="button" variant="outline" onClick={() => navigate(-1)} disabled={isSubmitting}>
            Cancel
          </Button>
          <Button type="submit" disabled={isSubmitting}>
            {isSubmitting
              ? editing
                ? "Saving changes..."
                : "Scheduling..."
              : editing
                ? "Save changes"
                : "Schedule maintenance"}
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

function tone(status: MaintenanceStatus) {
  return status === "Completed"
    ? "good"
    : status === "Cancelled"
      ? "danger"
      : status === "InProgress"
        ? "warning"
        : "neutral";
}

function toDateTimeLocalString(dateString?: string | null): string {
  if (!dateString) return "";
  const d = new Date(dateString);
  if (isNaN(d.getTime())) return "";
  const pad = (n: number) => n.toString().padStart(2, "0");
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
}
