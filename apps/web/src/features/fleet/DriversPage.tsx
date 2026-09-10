import { Link, useNavigate, useParams } from "react-router";
import { Plus, Search } from "lucide-react";
import { useState, type ReactNode } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { PageHeading } from "@/components/shared/PageHeading";
import { StatusBadge } from "@/components/shared/StatusBadge";
import { centres } from "@/mock/centres";
import { useAuth } from "@/hooks/useAuth";

type Driver = { id: string; centreId: string; fullName: string; phoneNumber: string; licenseNumber: string; status: "Active" | "Inactive" };

const mockDrivers: Driver[] = [
  { id: "DRV-020", centreId: "makumbura", fullName: "A. Perera", phoneNumber: "077 123 4567", licenseNumber: "B-457829", status: "Active" },
  { id: "DRV-036", centreId: "makumbura", fullName: "S. Fernando", phoneNumber: "077 234 5678", licenseNumber: "B-334204", status: "Active" },
  { id: "DRV-051", centreId: "kadawatha", fullName: "N. Silva", phoneNumber: "077 345 6789", licenseNumber: "B-751228", status: "Inactive" },
];

export function DriversPage() {
  const { user } = useAuth();
  const [query, setQuery] = useState("");
  const rows = mockDrivers.filter((driver) => (!user?.centreId || driver.centreId === user.centreId) && `${driver.fullName} ${driver.licenseNumber} ${driver.phoneNumber}`.toLowerCase().includes(query.toLowerCase()));
  return <main className="flex flex-1 flex-col gap-4 bg-muted/20 p-4"><PageHeading title="Drivers" description="Centre-scoped driver identity, licence, duty eligibility, and trip assignment records." action={<Button render={<Link to="/fleet/drivers/new" />}><Plus /> Add driver</Button>} /><div className="relative max-w-sm"><Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" /><Input className="pl-9" value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Search name or licence" /></div><section className="overflow-hidden rounded-lg border bg-card">{rows.map((driver) => <Link key={driver.id} to={`/fleet/drivers/${driver.id}`} className="grid gap-2 border-b px-4 py-4 hover:bg-muted/40 md:grid-cols-[minmax(0,1fr)_180px_150px] md:items-center"><div><p className="font-medium">{driver.fullName}</p><p className="mt-1 text-sm text-muted-foreground">{driver.licenseNumber} · {driver.phoneNumber}</p></div><p className="text-sm text-muted-foreground">{centres.find((centre) => centre.id === driver.centreId)?.name}</p><StatusBadge label={driver.status} tone={driver.status === "Active" ? "good" : "neutral"} /></Link>)}{rows.length === 0 && <p className="p-8 text-center text-sm text-muted-foreground">No drivers match this search.</p>}</section></main>;
}

export function DriverDetailPage() {
  const { driverId } = useParams();
  const driver = mockDrivers.find((item) => item.id === driverId) ?? mockDrivers[0];
  return <main className="flex flex-1 flex-col gap-4 bg-muted/20 p-4"><PageHeading title={driver.fullName} description="Driver profile and API-ready operational identity." action={<div className="flex gap-2"><Button variant="outline" render={<Link to={`/fleet/drivers/${driver.id}/edit`} />}>Edit driver</Button><Button variant="destructive">Deactivate</Button></div>} /><section className="max-w-3xl rounded-lg border bg-card p-5"><div className="flex items-start justify-between"><div><p className="text-sm text-muted-foreground">Driver reference</p><p className="mt-1 font-medium">{driver.id}</p></div><StatusBadge label={driver.status} tone={driver.status === "Active" ? "good" : "neutral"} /></div><div className="mt-6 grid gap-5 border-t pt-5 sm:grid-cols-2"><Fact label="Full name" value={driver.fullName} /><Fact label="Phone number" value={driver.phoneNumber} /><Fact label="Licence number" value={driver.licenseNumber} /><Fact label="Assigned centre" value={centres.find((centre) => centre.id === driver.centreId)?.name ?? "—"} /></div><p className="mt-6 rounded-md bg-muted/50 p-3 text-sm text-muted-foreground">Trip duty and assignment are shown through Dispatch after API integration. This profile owns only the Driver API fields.</p></section></main>;
}

export function DriverFormPage() {
  const { driverId } = useParams();
  const navigate = useNavigate();
  const { user } = useAuth();
  const selectedDriver = mockDrivers.find((item) => item.id === driverId);
  const editing = Boolean(selectedDriver);
  const driver = selectedDriver ?? mockDrivers[0];
  const defaultCentre = selectedDriver?.centreId ?? user?.centreId ?? centres[0].id;
  return <main className="flex flex-1 flex-col gap-4 bg-muted/20 p-4"><PageHeading title={editing ? `Edit ${driver.fullName}` : "Add driver"} description={editing ? "Update fields map directly to UpdateDriverRequest." : "Fields map directly to CreateDriverRequest."} /><form className="max-w-3xl rounded-lg border bg-card p-5" onSubmit={(event) => { event.preventDefault(); navigate(editing ? `/fleet/drivers/${driver.id}` : "/fleet/drivers"); }}><div className="grid gap-4 sm:grid-cols-2"><Field name="fullName" label="Full name" value={selectedDriver?.fullName} placeholder="Driver full name" /><Field name="phoneNumber" label="Phone number" value={selectedDriver?.phoneNumber} placeholder="077 123 4567" /><SelectField name="centreId" label="Assigned centre" defaultValue={defaultCentre}>{centres.map((centre) => <SelectItem key={centre.id} value={centre.id}>{centre.name}</SelectItem>)}</SelectField>{!editing && <Field name="licenseNumber" label="Licence number" placeholder="B-457829" />}{editing && <Fact label="Licence number" value={driver.licenseNumber} />}<SelectField name="status" label="Duty eligibility" defaultValue={selectedDriver?.status ?? "Active"}><SelectItem value="Active">Active</SelectItem><SelectItem value="Inactive">Inactive</SelectItem></SelectField></div>{editing && <p className="mt-4 text-xs text-muted-foreground">Licence number is immutable in the current UpdateDriverRequest. Create a corrected driver record if it was entered incorrectly.</p>}<div className="mt-5 flex justify-end gap-2"><Button type="button" variant="outline" onClick={() => navigate(-1)}>Cancel</Button><Button type="submit">{editing ? "Save changes" : "Create driver"}</Button></div></form></main>;
}

function Field({ name, label, value, placeholder }: { name: string; label: string; value?: string; placeholder: string }) { return <label className="grid gap-1.5 text-sm font-medium">{label}<Input name={name} defaultValue={value} placeholder={placeholder} required={name !== "phoneNumber"} /></label>; }
function SelectField({ name, label, defaultValue, children }: { name: string; label: string; defaultValue: string; children: ReactNode }) { return <label className="grid gap-1.5 text-sm font-medium">{label}<Select name={name} defaultValue={defaultValue}><SelectTrigger className="w-full bg-muted/60"><SelectValue /></SelectTrigger><SelectContent>{children}</SelectContent></Select></label>; }
function Fact({ label, value }: { label: string; value: string }) { return <div><p className="text-xs text-muted-foreground">{label}</p><p className="mt-1 text-sm font-medium">{value}</p></div>; }
