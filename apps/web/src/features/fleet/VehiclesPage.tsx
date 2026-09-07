import { useState } from "react";
import { Plus, Search } from "lucide-react";
import { Link, useNavigate, useParams } from "react-router";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { PageHeading } from "@/components/shared/PageHeading";
import { StatusBadge } from "@/components/shared/StatusBadge";
import { collections } from "@/mock/mock-data";
import { useMockData } from "@/context/MockDataContext";
import { useAuth } from "@/hooks/useAuth";
import { mockRowCentreId } from "@/mock/centre-scope";
import { centres, departuresByCentre } from "@/mock/centres";

export function VehiclesPage({ centreId, basePath = "/fleet/vehicles", readOnly = false }: { centreId?: string; basePath?: string; readOnly?: boolean } = {}) {
  const { data } = useMockData();
  const { user } = useAuth();
  const [query, setQuery] = useState("");
  const effectiveCentreId = centreId ?? user?.centreId;
  const centre = centres.find((item) => item.id === effectiveCentreId);
  const vehicles = data.vehicles ?? collections.vehicles;
  const visibleVehicles = vehicles.filter((vehicle, index) => !effectiveCentreId || mockRowCentreId(vehicle, index) === effectiveCentreId);
  const matchingVehicles = visibleVehicles.filter((vehicle) => `${vehicle.id} ${vehicle.title} ${vehicle.subtitle} ${vehicle.status}`.toLowerCase().includes(query.trim().toLowerCase()));
  return <main className="flex flex-1 flex-col gap-4 bg-muted/20 p-4"><PageHeading title="Vehicles" description={`${readOnly ? "Read-only fleet assignments, compliance, and service history" : "A fleet workspace for availability, assignments, compliance, and service history"}.`} action={readOnly ? <Button variant="outline" render={<Link to={`/admin/centres/${effectiveCentreId}`} />}>Back to centre</Button> : <Button render={<Link to="/fleet/vehicles/new" />}><Plus /> Register vehicle</Button>} /><div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between"><p className="text-sm text-muted-foreground">{matchingVehicles.length} vehicles assigned to {centre?.name ?? "this centre"}</p><div className="relative w-full sm:max-w-xs"><Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" /><Input value={query} onChange={(event) => setQuery(event.target.value)} className="h-9 bg-card pl-9" placeholder="Search registration or model" /></div></div><section className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">{matchingVehicles.map((vehicle) => <Link key={vehicle.id} to={`${basePath}/${vehicle.id}`} className="group flex rounded-lg border bg-card"><div className="flex w-24 shrink-0 items-center justify-center p-2"><img src="/vehicle-placeholder.svg" alt="UPTS bus placeholder" className="size-20 rounded-md bg-muted object-contain" /></div><div className="flex min-w-0 flex-1 flex-col py-2 pr-3"><div className="flex items-start justify-between gap-2"><p className="font-semibold">{vehicle.id}</p><StatusBadge label={vehicle.status} tone={vehicle.tone} /></div><h2 className="mt-1 text-sm font-medium">{vehicle.title}</h2><p className="mt-2 border-t pt-2 text-xs text-muted-foreground">{vehicle.updated}</p></div></Link>)}{matchingVehicles.length === 0 && <div className="col-span-full rounded-lg border border-dashed bg-card p-8 text-center text-sm text-muted-foreground">No vehicles match this search.</div>}</section></main>;
}

export function VehicleProfilePage({ centreId, basePath = "/fleet/vehicles", readOnly = false }: { centreId?: string; basePath?: string; readOnly?: boolean } = {}) {
  const { data, deactivateRecord } = useMockData();
  const { user } = useAuth();
  const { vehicleId } = useParams();
  const vehicles = data.vehicles ?? collections.vehicles;
  const effectiveCentreId = centreId ?? user?.centreId;
  const scopedVehicles = vehicles.filter((item, index) => !effectiveCentreId || mockRowCentreId(item, index) === effectiveCentreId);
  const vehicle = scopedVehicles.find((item) => item.id === vehicleId) ?? scopedVehicles[0];
  if (!vehicle) return <main className="p-5">Vehicle not found for this centre.</main>;
  const assignment = effectiveCentreId ? departuresByCentre[effectiveCentreId]?.find((item) => item.vehicle === vehicle.id) : undefined;
  return <main className="flex flex-1 flex-col gap-4 bg-muted/20 p-4"><PageHeading title={vehicle.id} description="Vehicle profile, current operating state, and maintenance readiness." action={readOnly ? <Button variant="outline" render={<Link to={basePath} />}>Back to vehicles</Button> : <div className="flex gap-2"><Button variant="outline" render={<Link to={`/fleet/vehicles/${vehicle.id}/edit`} />}>Edit vehicle</Button><Button variant="destructive" onClick={() => deactivateRecord("vehicles", vehicle.id)}>Deactivate</Button></div>} /><section className="grid gap-4 xl:grid-cols-[minmax(0,1.1fr)_minmax(330px,0.9fr)]"><article className="overflow-hidden rounded-lg border bg-card"><img src="/vehicle-placeholder.svg" alt={`Placeholder for ${vehicle.id}`} className="h-64 w-full object-cover" /><div className="grid gap-4 p-5 sm:grid-cols-3"><Info label="Registration" value={vehicle.id} /><Info label="Vehicle type" value={vehicle.title} /><Info label="Operating status" value={vehicle.status} /></div></article><article className="rounded-lg border bg-card p-5"><h2 className="font-semibold">Current assignment</h2><div className="mt-4 rounded-md bg-muted/60 p-4"><p className="text-sm font-medium">{vehicle.updated}</p><p className="mt-1 text-sm text-muted-foreground">{assignment ? `Route ${assignment.route} · ${assignment.destination}` : "No active departure assigned"}</p><p className="mt-3 text-xs text-muted-foreground">{assignment ? `Bay ${assignment.bay} · Departure ${assignment.time} · ${assignment.occupancy}% occupied` : "Vehicle remains available for centre dispatch"}</p></div><h2 className="mt-6 font-semibold">Operational readiness</h2><div className="mt-3 space-y-3"><Meter label="Fuel / charge" value="78%" width="78%" /><Meter label="Inspection compliance" value="Valid" width="100%" /><Meter label="Seat availability" value="9 seats" width="18%" /></div></article></section><section className="grid gap-4 lg:grid-cols-2"><article className="rounded-lg border bg-card p-5"><h2 className="font-semibold">Maintenance timeline</h2><div className="mt-4 space-y-4 border-l pl-4 text-sm"><p><b>Sep 21</b> · Next oil service scheduled</p><p><b>Aug 19</b> · Routine inspection passed</p><p><b>Jul 04</b> · Brake service completed</p></div></article><article className="rounded-lg border bg-card p-5"><h2 className="font-semibold">Service performance</h2><div className="mt-4 grid grid-cols-3 gap-3"><Info label="Trips / 30d" value="184" /><Info label="On-time" value="91.8%" /><Info label="Incidents" value="2" /></div></article></section></main>;
}

export function VehicleFormPage() {
  const { vehicleId } = useParams();
  const navigate = useNavigate();
  const { data, saveRecord } = useMockData();
  const { user } = useAuth();
  const editing = Boolean(vehicleId);
  const existing = data.vehicles?.find((item) => item.id === vehicleId);
  return <main className="flex flex-1 flex-col gap-4 bg-muted/20 p-4"><PageHeading title={editing ? `Edit ${vehicleId}` : "Register vehicle"} description="Capture the operational details required before a vehicle can be dispatched." /><form className="max-w-3xl rounded-lg border bg-card p-5" onSubmit={(event) => { event.preventDefault(); const form = new FormData(event.currentTarget); const id = String(form.get("registration") || vehicleId || "WP-NEW-0001").toUpperCase(); saveRecord("vehicles", { id, title: String(form.get("model") || "New vehicle"), subtitle: `${String(form.get("type") || "Standard bus")} · ${String(form.get("capacity") || "0")} seats · ${String(form.get("depot") || "Unassigned depot")}`, status: existing?.status ?? "Available", tone: existing?.tone ?? "neutral", updated: "Updated just now", meta: "Inspection pending", district: String(form.get("district") || "Colombo"), centreId: user?.centreId }); navigate(`/fleet/vehicles/${id}`); }}><div className="grid gap-4 sm:grid-cols-2"><Field name="registration" label="Registration number" value={vehicleId ?? ""} placeholder="WP CAB-4821" /><Field name="model" label="Vehicle model" value={existing?.title} placeholder="Ashok Leyland Viking" /><Field name="type" label="Vehicle type" placeholder="Standard bus" /><Field name="capacity" label="Seat capacity" placeholder="52" type="number" /><Field name="depot" label="Depot" placeholder="Colombo depot" /><Field name="district" label="District" value={existing?.district} placeholder="Colombo" /></div><div className="mt-5 flex justify-end gap-2"><Button type="button" variant="outline" onClick={() => navigate(-1)}>Cancel</Button><Button type="submit">{editing ? "Save changes" : "Register vehicle"}</Button></div></form></main>;
}

function Field({ label, value, ...props }: { label: string; name: string; value?: string; placeholder?: string; type?: string }) { return <label className="grid gap-1.5 text-sm font-medium">{label}<Input defaultValue={value} {...props} /></label>; }
function Info({ label, value }: { label: string; value: string }) { return <div><p className="text-xs text-muted-foreground">{label}</p><p className="mt-1 text-sm font-medium">{value}</p></div>; }
function Meter({ label, value, width }: { label: string; value: string; width: string }) { return <div><div className="flex justify-between text-sm"><span>{label}</span><span className="font-medium">{value}</span></div><div className="mt-2 h-1.5 rounded bg-muted"><div className="h-full rounded bg-primary" style={{ width }} /></div></div>; }
