import { useState, useEffect } from "react";
import { Link, useNavigate, useParams } from "react-router";
import { Plus, RefreshCw, AlertCircle } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { PageHeading } from "@/components/shared/PageHeading";
import { StatusBadge } from "@/components/shared/StatusBadge";
import {
  Table,
  TableHeader,
  TableBody,
  TableRow,
  TableHead,
  TableCell,
} from "@/components/ui/table";
import type { StatusTone } from "@/mock/mock-data";

const API_BASE = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5250";

export interface VehicleItem {
  vehicleId: string;
  registrationNumber: string;
  vehicleType: "Normal" | "SemiLuxury" | "AcExpress" | string;
  capacity: number;
  status:
    | "Available"
    | "Assigned"
    | "InTrip"
    | "Maintenance"
    | "OutOfService"
    | "Inactive"
    | string;
  centreId: string;
  centreName?: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CentreItem {
  id: string;
  name: string;
  code?: string;
  city?: string;
}

function getStatusTone(status: string): StatusTone {
  switch (status) {
    case "Available":
    case "Active":
      return "good";
    case "Assigned":
    case "InTrip":
      return "neutral";
    case "Maintenance":
      return "warning";
    case "OutOfService":
    case "Inactive":
      return "danger";
    default:
      return "neutral";
  }
}

export function VehiclesPage({
  centreId,
  basePath = "/fleet/vehicles",
  readOnly = false,
}: {
  centreId?: string;
  basePath?: string;
  readOnly?: boolean;
} = {}) {
  const [vehicles, setVehicles] = useState<VehicleItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [actionLoadingId, setActionLoadingId] = useState<string | null>(null);
  useEffect(() => {
    let ignore = false;
    async function fetchVehicles() {
      try {
        const url = centreId
          ? `${API_BASE}/api/vehicles?centreId=${encodeURIComponent(centreId)}`
          : `${API_BASE}/api/vehicles`;
        const res = await fetch(url);
        if (!res.ok) {
          throw new Error(`Failed to load vehicles (HTTP ${res.status})`);
        }
        const data = await res.json();
        if (!ignore) {
          setVehicles(data);
          setError(null);
        }
      } catch (err: unknown) {
        if (!ignore) {
          const message = err instanceof Error ? err.message : "Error connecting to backend API";
          setError(message);
        }
      } finally {
        if (!ignore) {
          setLoading(false);
        }
      }
    }

    void fetchVehicles();

    return () => {
      ignore = true;
    };
  }, [centreId]);

  async function handleRefresh() {
    setLoading(true);
    setError(null);
    try {
      const url = centreId
        ? `${API_BASE}/api/vehicles?centreId=${encodeURIComponent(centreId)}`
        : `${API_BASE}/api/vehicles`;
      const res = await fetch(url);
      if (!res.ok) {
        throw new Error(`Failed to load vehicles (HTTP ${res.status})`);
      }
      const data = await res.json();
      setVehicles(data);
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : "Error connecting to backend API";
      setError(message);
    } finally {
      setLoading(false);
    }
  }

  async function handleSoftDelete(vehicleId: string) {
    if (!confirm("Are you sure you want to deactivate this vehicle?")) return;
    setActionLoadingId(vehicleId);
    try {
      const res = await fetch(`${API_BASE}/api/vehicles/${vehicleId}`, {
        method: "DELETE",
      });
      if (!res.ok) {
        const data = await res.json().catch(() => null);
        throw new Error(data?.error ?? `Delete failed (HTTP ${res.status})`);
      }
      // Refresh vehicles after deactivation
      await handleRefresh();
    } catch (err: unknown) {
      alert(err instanceof Error ? err.message : "Failed to deactivate vehicle");
    } finally {
      setActionLoadingId(null);
    }
  }

  return (
    <main className="flex flex-1 flex-col gap-4 bg-muted/20 p-6">
      <PageHeading
        title="Vehicles"
        description="Fleet management: vehicle registration, type, capacity, and operational status."
        action={
          readOnly ? (
            <Button variant="outline" size="sm" onClick={handleRefresh} disabled={loading}>
              <RefreshCw className={`size-4 ${loading ? "animate-spin" : ""}`} /> Refresh
            </Button>
          ) : (
            <div className="flex gap-2">
              <Button variant="outline" size="sm" onClick={handleRefresh} disabled={loading}>
                <RefreshCw className={`size-4 ${loading ? "animate-spin" : ""}`} /> Refresh
              </Button>
              <Button size="sm" render={<Link to={`${basePath}/new`} />}>
                <Plus className="size-4" /> Add Vehicle
              </Button>
            </div>
          )
        }
      />

      {error && (
        <div className="flex items-center gap-2 rounded-lg border border-destructive/50 bg-destructive/10 p-4 text-sm text-destructive">
          <AlertCircle className="size-4 shrink-0" />
          <span>{error}</span>
          <Button variant="outline" size="sm" className="ml-auto" onClick={handleRefresh}>
            Retry
          </Button>
        </div>
      )}

      {loading ? (
        <div className="rounded-lg border bg-card p-12 text-center text-sm text-muted-foreground">
          Loading vehicles from API...
        </div>
      ) : vehicles.length === 0 ? (
        <div className="rounded-lg border border-dashed bg-card p-12 text-center">
          <p className="text-sm font-medium text-foreground">No vehicles registered yet.</p>
          <p className="mt-1 text-xs text-muted-foreground">
            Click "Add Vehicle" to register the first vehicle into the fleet.
          </p>
          <div className="mt-4">
            <Button size="sm" render={<Link to={`${basePath}/new`} />}>
              <Plus className="size-4" /> Add Vehicle
            </Button>
          </div>
        </div>
      ) : (
        <div className="overflow-hidden rounded-lg border bg-card">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead className="font-semibold">Registration Number</TableHead>
                <TableHead className="font-semibold">Type</TableHead>
                <TableHead className="font-semibold">Capacity</TableHead>
                <TableHead className="font-semibold">Status</TableHead>
                <TableHead className="font-semibold">Centre</TableHead>
                <TableHead className="font-semibold">Active</TableHead>
                <TableHead className="text-right font-semibold">Action</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {vehicles.map((v) => (
                <TableRow key={v.vehicleId}>
                  <TableCell className="font-semibold">
                    <Link to={`${basePath}/${v.vehicleId}`} className="text-primary hover:underline">
                      {v.registrationNumber}
                    </Link>
                  </TableCell>
                  <TableCell>{v.vehicleType}</TableCell>
                  <TableCell>{v.capacity} seats</TableCell>
                  <TableCell>
                    <StatusBadge label={v.status} tone={getStatusTone(v.status)} />
                  </TableCell>
                  <TableCell className="text-muted-foreground">
                    {v.centreName ?? v.centreId}
                  </TableCell>
                  <TableCell>
                    <span
                      className={`inline-flex rounded-full px-2 py-0.5 text-xs font-medium ${
                        v.isActive
                          ? "bg-emerald-100 text-emerald-800 dark:bg-emerald-950 dark:text-emerald-300"
                          : "bg-zinc-200 text-zinc-700 dark:bg-zinc-800 dark:text-zinc-400"
                      }`}
                    >
                      {v.isActive ? "Active" : "Inactive"}
                    </span>
                  </TableCell>
                  <TableCell className="text-right">
                    <div className="flex justify-end gap-2">
                      <Button
                        variant="outline"
                        size="sm"
                        render={<Link to={`${basePath}/${v.vehicleId}/edit`} />}
                      >
                        Edit
                      </Button>
                      {v.isActive ? (
                        <Button
                          variant="destructive"
                          size="sm"
                          disabled={actionLoadingId === v.vehicleId}
                          onClick={() => handleSoftDelete(v.vehicleId)}
                        >
                          {actionLoadingId === v.vehicleId ? "Deactivating..." : "Deactivate"}
                        </Button>
                      ) : (
                        <span className="flex items-center text-xs text-muted-foreground">Deactivated</span>
                      )}
                    </div>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}
    </main>
  );
}

const DEFAULT_CENTRES: CentreItem[] = [
  { id: "11111111-1111-1111-1111-111111111111", name: "Makumbura Multimodal Centre (MMC)" },
  { id: "22222222-2222-2222-2222-222222222222", name: "Kadawatha Multimodal Centre (KMC)" },
  { id: "33333333-3333-3333-3333-333333333333", name: "Kandy Central Transit Hub" },
  { id: "44444444-4444-4444-4444-444444444444", name: "Bastian Mawatha Transit Centre" },
];

export function VehicleFormPage() {
  const navigate = useNavigate();
  const { vehicleId } = useParams();
  const isEditing = Boolean(vehicleId);

  const [registrationNumber, setRegistrationNumber] = useState("");
  const [vehicleType, setVehicleType] = useState<string>("Normal");
  const [capacity, setCapacity] = useState<number | string>(52);
  const [centreId, setCentreId] = useState<string>(DEFAULT_CENTRES[0].id);
  const [status, setStatus] = useState<string>("Available");

  const [centres] = useState<CentreItem[]>(DEFAULT_CENTRES);
  const [loading, setLoading] = useState(isEditing);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!vehicleId) return;
    let ignore = false;
    async function loadVehicle() {
      try {
        const res = await fetch(`${API_BASE}/api/vehicles/${vehicleId}`);
        if (!res.ok) {
          throw new Error(`Failed to load vehicle (HTTP ${res.status})`);
        }
        const data = await res.json();
        if (!ignore) {
          setRegistrationNumber(data.registrationNumber ?? "");
          setVehicleType(data.vehicleType ?? "Normal");
          setCapacity(data.capacity ?? 52);
          setCentreId(data.centreId ?? DEFAULT_CENTRES[0].id);
          setStatus(data.status ?? "Available");
        }
      } catch (err: unknown) {
        if (!ignore) {
          setError(err instanceof Error ? err.message : "Error loading vehicle details");
        }
      } finally {
        if (!ignore) {
          setLoading(false);
        }
      }
    }
    void loadVehicle();
    return () => {
      ignore = true;
    };
  }, [vehicleId]);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);

    const trimmedReg = registrationNumber.trim().toUpperCase();
    if (!trimmedReg) {
      setError("Registration number is required.");
      return;
    }

    const numCapacity = Number(capacity);
    if (!numCapacity || numCapacity <= 0) {
      setError("Capacity must be greater than 0.");
      return;
    }

    if (!centreId) {
      setError("Please select a centre.");
      return;
    }

    const selectedCentre = centres.find((c) => c.id === centreId);

    setSubmitting(true);
    try {
      const url = isEditing ? `${API_BASE}/api/vehicles/${vehicleId}` : `${API_BASE}/api/vehicles`;
      const method = isEditing ? "PUT" : "POST";
      const payload: Record<string, unknown> = {
        registrationNumber: trimmedReg,
        vehicleType,
        capacity: numCapacity,
        centreId,
        centreName: selectedCentre?.name ?? "Makumbura Multimodal Centre (MMC)",
        status,
      };

      const res = await fetch(url, {
        method,
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify(payload),
      });

      if (!res.ok) {
        const data = await res.json().catch(() => null);
        throw new Error(data?.error ?? `Failed to ${isEditing ? "update" : "register"} vehicle (HTTP ${res.status})`);
      }

      // Success -> navigate to vehicles list
      navigate("/fleet/vehicles");
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : `An error occurred while ${isEditing ? "updating" : "creating"} vehicle`);
    } finally {
      setSubmitting(false);
    }
  }

  if (loading) {
    return (
      <main className="flex flex-1 flex-col gap-4 bg-muted/20 p-6">
        <div className="rounded-lg border bg-card p-12 text-center text-sm text-muted-foreground">
          Loading vehicle details...
        </div>
      </main>
    );
  }

  return (
    <main className="flex flex-1 flex-col gap-4 bg-muted/20 p-6">
      <PageHeading
        title={isEditing ? "Edit Vehicle" : "Add Vehicle"}
        description={
          isEditing
            ? "Update vehicle registration, type, capacity, and operational status."
            : "Register a new vehicle into the transport fleet."
        }
      />

      {error && (
        <div className="flex max-w-2xl items-center gap-2 rounded-lg border border-destructive/50 bg-destructive/10 p-4 text-sm text-destructive">
          <AlertCircle className="size-4 shrink-0" />
          <span>{error}</span>
        </div>
      )}

      <form
        onSubmit={handleSubmit}
        className="max-w-2xl space-y-5 rounded-lg border bg-card p-6 shadow-xs"
      >
        <div className="grid gap-4 sm:grid-cols-2">
          <div className="grid gap-1.5 text-sm font-medium">
            <label htmlFor="registrationNumber">Registration Number *</label>
            <Input
              id="registrationNumber"
              value={registrationNumber}
              onChange={(e) => setRegistrationNumber(e.target.value)}
              placeholder="WP CAB-4821"
              required
            />
          </div>

          <div className="grid gap-1.5 text-sm font-medium">
            <label htmlFor="vehicleType">Vehicle Type *</label>
            <select
              id="vehicleType"
              value={vehicleType}
              onChange={(e) => setVehicleType(e.target.value)}
              className="flex h-9 w-full rounded-md border border-input bg-background px-3 py-1 text-sm shadow-xs transition-colors focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
            >
              <option value="Normal">Normal</option>
              <option value="SemiLuxury">SemiLuxury</option>
              <option value="AcExpress">AcExpress</option>
            </select>
          </div>

          <div className="grid gap-1.5 text-sm font-medium">
            <label htmlFor="capacity">Capacity (Seats) *</label>
            <Input
              id="capacity"
              type="number"
              min="1"
              value={capacity}
              onChange={(e) => setCapacity(e.target.value)}
              placeholder="52"
              required
            />
          </div>

          <div className="grid gap-1.5 text-sm font-medium">
            <label htmlFor="status">Status *</label>
            <select
              id="status"
              value={status}
              onChange={(e) => setStatus(e.target.value)}
              className="flex h-9 w-full rounded-md border border-input bg-background px-3 py-1 text-sm shadow-xs transition-colors focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
            >
              <option value="Available">Available</option>
              <option value="Assigned">Assigned</option>
              <option value="InTrip">InTrip</option>
              <option value="Maintenance">Maintenance</option>
              <option value="OutOfService">OutOfService</option>
              <option value="Inactive">Inactive</option>
            </select>
          </div>

          <div className="grid gap-1.5 text-sm font-medium sm:col-span-2">
            <label htmlFor="centreId">Operating Centre *</label>
            <select
              id="centreId"
              value={centreId}
              onChange={(e) => setCentreId(e.target.value)}
              className="flex h-9 w-full rounded-md border border-input bg-background px-3 py-1 text-sm shadow-xs transition-colors focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
              required
            >
              {centres.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.name}
                </option>
              ))}
            </select>
          </div>
        </div>

        <div className="flex justify-end gap-2 border-t pt-4">
          <Button
            type="button"
            variant="outline"
            onClick={() => navigate("/fleet/vehicles")}
            disabled={submitting}
          >
            Cancel
          </Button>
          <Button type="submit" disabled={submitting}>
            {submitting ? "Saving..." : isEditing ? "Save Changes" : "Add Vehicle"}
          </Button>
        </div>
      </form>
    </main>
  );
}

export function VehicleProfilePage(props: {
  centreId?: string;
  basePath?: string;
  readOnly?: boolean;
} = {}) {
  const { vehicleId } = useParams();
  const navigate = useNavigate();
  const basePath = props.basePath ?? "/fleet/vehicles";
  const [vehicle, setVehicle] = useState<VehicleItem | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    async function load() {
      if (!vehicleId) return;
      try {
        const res = await fetch(`${API_BASE}/api/vehicles/${vehicleId}`);
        if (res.ok) {
          setVehicle(await res.json());
        }
      } catch {
        // ignore
      } finally {
        setLoading(false);
      }
    }
    void load();
  }, [vehicleId]);

  if (loading) return <main className="p-6">Loading vehicle details...</main>;
  if (!vehicle) {
    return (
      <main className="p-6">
        <p>Vehicle not found.</p>
        <Button variant="outline" className="mt-4" onClick={() => navigate(basePath)}>
          Back to vehicles
        </Button>
      </main>
    );
  }

  return (
    <main className="flex flex-1 flex-col gap-4 bg-muted/20 p-6">
      <PageHeading
        title={vehicle.registrationNumber}
        description="Vehicle details and operating state"
        action={
          <div className="flex gap-2">
            <Button variant="outline" onClick={() => navigate(basePath)}>
              Back to vehicles
            </Button>
            <Button render={<Link to={`${basePath}/${vehicle.vehicleId}/edit`} />}>
              Edit vehicle
            </Button>
          </div>
        }
      />
      <div className="max-w-xl rounded-lg border bg-card p-6 space-y-3">
        <p><b>Registration:</b> {vehicle.registrationNumber}</p>
        <p><b>Type:</b> {vehicle.vehicleType}</p>
        <p><b>Capacity:</b> {vehicle.capacity} seats</p>
        <p><b>Status:</b> {vehicle.status}</p>
        <p><b>Centre:</b> {vehicle.centreName ?? vehicle.centreId}</p>
        <p><b>Active:</b> {vehicle.isActive ? "Yes" : "No"}</p>
      </div>
    </main>
  );
}
