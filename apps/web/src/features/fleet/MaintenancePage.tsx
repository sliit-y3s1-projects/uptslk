import { Link, useNavigate, useParams } from "react-router";
import { Plus, Search } from "lucide-react";
import { useState, type ReactNode } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { PageHeading } from "@/components/shared/PageHeading";
import { StatusBadge } from "@/components/shared/StatusBadge";

type Maintenance = { id: string; vehicleId: string; vehicle: string; type: string; description: string; status: "Scheduled" | "InProgress" | "Completed" | "Cancelled"; scheduledFor: string; completedAt?: string };

const mockMaintenance: Maintenance[] = [
  { id: "MNT-201", vehicleId: "vehicle-7714", vehicle: "WP ND-7714", type: "Annual inspection", description: "Full annual safety and emissions inspection.", status: "Scheduled", scheduledFor: "2026-09-26T09:00" },
  { id: "MNT-202", vehicleId: "vehicle-3381", vehicle: "WP NC-3381", type: "Oil service", description: "Routine engine oil and filter replacement.", status: "InProgress", scheduledFor: "2026-09-19T10:00" },
  { id: "MNT-203", vehicleId: "vehicle-2428", vehicle: "WP NB-2428", type: "Brake inspection", description: "Inspect and test brake wear and response.", status: "Completed", scheduledFor: "2026-09-02T08:00", completedAt: "2026-09-02T10:15" },
];

const vehicles = mockMaintenance.map((record) => ({ id: record.vehicleId, label: record.vehicle })).filter((vehicle, index, items) => items.findIndex((item) => item.id === vehicle.id) === index);

export function MaintenancePage() {
  const [query, setQuery] = useState("");
  const records = mockMaintenance.filter((record) => `${record.vehicle} ${record.type} ${record.description} ${record.status}`.toLowerCase().includes(query.toLowerCase()));
  return <main className="flex flex-1 flex-col gap-4 bg-muted/20 p-4"><PageHeading title="Maintenance" description="Vehicle service records with scheduling, status, completion, and cancellation history." action={<Button render={<Link to="/fleet/maintenance/new" />}><Plus /> Schedule maintenance</Button>} /><div className="relative max-w-sm"><Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" /><Input className="pl-9" value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Search vehicle or maintenance type" /></div><section className="overflow-hidden rounded-lg border bg-card">{records.map((record) => <Link key={record.id} to={`/fleet/maintenance/${record.id}`} className="grid gap-2 border-b px-4 py-4 hover:bg-muted/40 md:grid-cols-[minmax(0,1fr)_180px_150px] md:items-center"><div><p className="font-medium">{record.type}</p><p className="mt-1 text-sm text-muted-foreground">{record.vehicle} · {record.description}</p></div><p className="text-sm text-muted-foreground">{new Date(record.scheduledFor).toLocaleString()}</p><StatusBadge label={record.status} tone={tone(record.status)} /></Link>)}{records.length === 0 && <p className="p-8 text-center text-sm text-muted-foreground">No maintenance records match this search.</p>}</section></main>;
}

export function MaintenanceDetailPage() {
  const { recordId } = useParams();
  const record = mockMaintenance.find((item) => item.id === recordId) ?? mockMaintenance[0];
  return <main className="flex flex-1 flex-col gap-4 bg-muted/20 p-4"><PageHeading title={record.type} description={`${record.vehicle} maintenance record`} action={<div className="flex gap-2"><Button variant="outline" render={<Link to={`/fleet/maintenance/${record.id}/edit`} />}>Edit record</Button>{record.status !== "Completed" && <Button variant="destructive">Cancel maintenance</Button>}</div>} /><section className="max-w-3xl rounded-lg border bg-card p-5"><div className="flex items-start justify-between"><div><p className="text-sm text-muted-foreground">Record reference</p><p className="mt-1 font-medium">{record.id}</p></div><StatusBadge label={record.status} tone={tone(record.status)} /></div><div className="mt-6 grid gap-5 border-t pt-5 sm:grid-cols-2"><Fact label="Vehicle" value={record.vehicle} /><Fact label="Scheduled for" value={new Date(record.scheduledFor).toLocaleString()} /><Fact label="Completed at" value={record.completedAt ? new Date(record.completedAt).toLocaleString() : "Not completed"} /><Fact label="Status" value={record.status} /></div><div className="mt-5"><p className="text-xs text-muted-foreground">Description</p><p className="mt-1 text-sm">{record.description}</p></div></section></main>;
}

export function MaintenanceFormPage() {
  const { recordId } = useParams();
  const navigate = useNavigate();
  const selectedRecord = mockMaintenance.find((item) => item.id === recordId);
  const editing = Boolean(selectedRecord);
  const record = selectedRecord ?? mockMaintenance[0];
  return <main className="flex flex-1 flex-col gap-4 bg-muted/20 p-4"><PageHeading title={editing ? `Edit ${record.type}` : "Schedule maintenance"} description={editing ? "Fields map directly to UpdateMaintenanceRecordRequest." : "Fields map directly to CreateMaintenanceRecordRequest."} /><form className="max-w-3xl rounded-lg border bg-card p-5" onSubmit={(event) => { event.preventDefault(); navigate(editing ? `/fleet/maintenance/${record.id}` : "/fleet/maintenance"); }}><div className="grid gap-4 sm:grid-cols-2"><SelectField name="vehicleId" label="Vehicle" defaultValue={selectedRecord?.vehicleId ?? vehicles[0].id}>{vehicles.map((vehicle) => <SelectItem key={vehicle.id} value={vehicle.id}>{vehicle.label}</SelectItem>)}</SelectField><Field name="type" label="Maintenance type" value={selectedRecord?.type} placeholder="Annual inspection" /><label className="grid gap-1.5 text-sm font-medium">Scheduled for<Input name="scheduledFor" type="datetime-local" defaultValue={selectedRecord?.scheduledFor} required /></label><label className="grid gap-1.5 text-sm font-medium sm:col-span-2">Description<Input name="description" defaultValue={selectedRecord?.description} placeholder="Describe the service work required" required /></label>{editing && <><SelectField name="status" label="Maintenance status" defaultValue={record.status}><SelectItem value="Scheduled">Scheduled</SelectItem><SelectItem value="InProgress">In progress</SelectItem><SelectItem value="Completed">Completed</SelectItem><SelectItem value="Cancelled">Cancelled</SelectItem></SelectField><label className="grid gap-1.5 text-sm font-medium">Completed at<Input name="completedAt" type="datetime-local" defaultValue={record.completedAt} /></label></>}</div>{editing && <p className="mt-4 text-xs text-muted-foreground">The API requires a completion time whenever status is set to Completed.</p>}<div className="mt-5 flex justify-end gap-2"><Button type="button" variant="outline" onClick={() => navigate(-1)}>Cancel</Button><Button type="submit">{editing ? "Save changes" : "Create maintenance record"}</Button></div></form></main>;
}

function Field({ name, label, value, placeholder }: { name: string; label: string; value?: string; placeholder: string }) { return <label className="grid gap-1.5 text-sm font-medium">{label}<Input name={name} defaultValue={value} placeholder={placeholder} required /></label>; }
function SelectField({ name, label, defaultValue, children }: { name: string; label: string; defaultValue: string; children: ReactNode }) { return <label className="grid gap-1.5 text-sm font-medium">{label}<Select name={name} defaultValue={defaultValue}><SelectTrigger className="w-full bg-muted/60"><SelectValue /></SelectTrigger><SelectContent>{children}</SelectContent></Select></label>; }
function Fact({ label, value }: { label: string; value: string }) { return <div><p className="text-xs text-muted-foreground">{label}</p><p className="mt-1 text-sm font-medium">{value}</p></div>; }
function tone(status: Maintenance["status"]) { return status === "Completed" ? "good" : status === "Cancelled" ? "danger" : status === "InProgress" ? "warning" : "neutral"; }
