import { Check, KeyRound, LockKeyhole, Plus, ShieldCheck } from "lucide-react";
import { useState, type FormEvent } from "react";
import { PageHeading } from "@/components/shared/PageHeading";
import { StatusBadge } from "@/components/shared/StatusBadge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog";

const roles = [
  { name: "Admin", scope: "Organization", permissions: "Centres, employees, roles, audit, settings" },
  { name: "CentreManager", scope: "Centre", permissions: "Operations, fleet, routes, employees, reports" },
  { name: "Dispatcher", scope: "Centre", permissions: "Dispatch, bays, trips, incidents" },
  { name: "FleetOfficer", scope: "Centre", permissions: "Vehicles, drivers, maintenance" },
  { name: "Driver", scope: "Centre", permissions: "Assigned trips, vehicle checks, incidents" },
];

export function RolesPage() {
  const [customRoles, setCustomRoles] = useState<typeof roles>([]);
  const [open, setOpen] = useState(false);
  function createRole(event: FormEvent<HTMLFormElement>) { event.preventDefault(); const form = new FormData(event.currentTarget); setCustomRoles((current) => [...current, { name: String(form.get("name")), scope: String(form.get("scope")), permissions: String(form.get("permissions")) }]); setOpen(false); }
  const allRoles = [...roles, ...customRoles];
  return <main className="flex flex-1 flex-col gap-4 bg-muted/20 p-5"><PageHeading title="Roles & permissions" description="Define least-privilege access profiles for UPTS identities." action={<Dialog open={open} onOpenChange={setOpen}><DialogTrigger render={<Button><Plus /> Create role</Button>} /><DialogContent><DialogHeader><DialogTitle>Create access role</DialogTitle><DialogDescription>Define a named role and the permissions it grants.</DialogDescription></DialogHeader><form onSubmit={createRole} className="grid gap-4"><label className="grid gap-1.5 text-sm font-medium">Role name<Input name="name" placeholder="Terminal supervisor" required /></label><label className="grid gap-1.5 text-sm font-medium">Scope<Select name="scope" defaultValue="Centre"><SelectTrigger><SelectValue /></SelectTrigger><SelectContent><SelectItem value="Centre">Centre</SelectItem><SelectItem value="Organization">Organization</SelectItem></SelectContent></Select></label><label className="grid gap-1.5 text-sm font-medium">Permissions<Input name="permissions" placeholder="Dispatch, Bays, Incidents" required /></label><DialogFooter><Button type="button" variant="outline" onClick={() => setOpen(false)}>Cancel</Button><Button type="submit">Create role</Button></DialogFooter></form></DialogContent></Dialog>} /><section className="overflow-hidden rounded-lg border bg-card"><div className="hidden grid-cols-[220px_160px_minmax(0,1fr)_120px] gap-4 border-b bg-muted/30 px-4 py-3 text-xs font-medium uppercase text-muted-foreground md:grid"><span>Role</span><span>Scope</span><span>Permissions</span><span>Status</span></div>{allRoles.map((role) => <div key={role.name} className="grid gap-3 border-b px-4 py-4 md:grid-cols-[220px_160px_minmax(0,1fr)_120px] md:items-center"><div className="flex items-center gap-3"><div className="flex size-9 items-center justify-center rounded-md bg-primary/10 text-primary"><ShieldCheck className="size-4" /></div><p className="font-medium">{role.name}</p></div><StatusBadge label={role.scope} tone={role.scope === "Organization" ? "warning" : "neutral"} /><p className="text-sm text-muted-foreground">{role.permissions}</p><span className="flex items-center gap-1 text-sm text-emerald-700"><Check className="size-4" /> Active</span></div>)}</section><section className="rounded-lg border border-amber-200 bg-amber-50 p-4 text-sm text-amber-950"><div className="flex gap-3"><LockKeyhole className="mt-0.5 size-4 shrink-0" /><p><strong>Least privilege:</strong> centre roles are limited to their assigned centre. Organization-wide access is reserved for Admin.</p></div></section></main>;
}

export function AccessRequestsPage() { return <main className="flex flex-1 flex-col gap-4 bg-muted/20 p-5"><PageHeading title="Access requests" description="Review exceptional access requests when the workflow API is connected." /><section className="rounded-lg border bg-card p-10 text-center text-sm text-muted-foreground"><KeyRound className="mx-auto mb-3 size-5" />No access requests available.</section></main>; }
