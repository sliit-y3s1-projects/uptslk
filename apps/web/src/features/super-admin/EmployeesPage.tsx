import { useMemo, useState, type FormEvent } from "react";
import { Building2, Plus, Search, UserRound } from "lucide-react";
import { useSearchParams } from "react-router";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { PageHeading } from "@/components/shared/PageHeading";
import { StatusBadge } from "@/components/shared/StatusBadge";
import { useOrganizationMock } from "@/context/OrganizationMockContext";
import { platformRoles } from "@/mock/organization";

export function EmployeesPage() {
  const { centres, employees, saveEmployee, toggleEmployee } = useOrganizationMock();
  const [params, setParams] = useSearchParams();
  const [query, setQuery] = useState("");
  const [centre, setCentre] = useState(params.get("centre") ?? "all");
  const [role, setRole] = useState("all");
  const showForm = params.get("create") === "true";
  const filtered = useMemo(() => employees.filter((employee) => {
    const matchesQuery = `${employee.name} ${employee.email} ${employee.id}`.toLowerCase().includes(query.toLowerCase());
    return matchesQuery && (centre === "all" || employee.centreId === centre) && (role === "all" || employee.role === role);
  }), [centre, employees, query, role]);

  function closeForm() { const next = new URLSearchParams(params); next.delete("create"); setParams(next); }
  function addEmployee(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    const selectedRole = String(form.get("role"));
    saveEmployee({ id: `EMP-${String(employees.length + 40).padStart(3, "0")}`, name: String(form.get("name")), email: String(form.get("email")), role: selectedRole, centreId: selectedRole === "Super Admin" ? undefined : String(form.get("centreId")), status: "Invited", lastActive: "Invitation sent" });
    closeForm();
  }

  return <main className="flex flex-1 flex-col gap-4 bg-muted/20 p-5">
    <PageHeading title="UPTSLK employees" description="Manage organization identities, centre assignments, roles, and account status." action={<Button onClick={() => setParams((current) => { const next = new URLSearchParams(current); next.set("create", "true"); return next; })}><Plus /> Add employee</Button>} />
    {showForm && <form onSubmit={addEmployee} className="rounded-lg border bg-card p-5"><div><h2 className="font-semibold">Invite employee</h2><p className="text-sm text-muted-foreground">The employee receives mock invited status until backend onboarding is connected.</p></div><div className="mt-4 grid gap-4 md:grid-cols-2 xl:grid-cols-4"><Field name="name" label="Full name" placeholder="Employee name" /><Field name="email" label="Work email" placeholder="name@upts.lk" type="email" /><label className="grid gap-1.5 text-sm font-medium">Role<Select name="role" defaultValue="Centre Manager"><SelectTrigger className="w-full bg-muted/60"><SelectValue /></SelectTrigger><SelectContent>{platformRoles.map((item) => <SelectItem key={item.id} value={item.name}>{item.name}</SelectItem>)}</SelectContent></Select></label><label className="grid gap-1.5 text-sm font-medium">Assigned centre<Select name="centreId" defaultValue={centres[0]?.id}><SelectTrigger className="w-full bg-muted/60"><SelectValue /></SelectTrigger><SelectContent>{centres.map((item) => <SelectItem key={item.id} value={item.id}>{item.name}</SelectItem>)}</SelectContent></Select></label></div><div className="mt-4 flex justify-end gap-2"><Button type="button" variant="outline" onClick={closeForm}>Cancel</Button><Button type="submit">Send invitation</Button></div></form>}
    <section className="rounded-lg border bg-card p-3"><div className="grid gap-3 md:grid-cols-[minmax(240px,1fr)_220px_220px]"><div className="relative"><Search className="absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" /><Input className="pl-9" placeholder="Search name, email, or ID" value={query} onChange={(event) => setQuery(event.target.value)} /></div><Select value={centre} onValueChange={(value) => setCentre(value ?? "all")}><SelectTrigger className="w-full bg-muted/60"><Building2 /><SelectValue /></SelectTrigger><SelectContent><SelectItem value="all">All centres</SelectItem>{centres.map((item) => <SelectItem key={item.id} value={item.id}>{item.name}</SelectItem>)}</SelectContent></Select><Select value={role} onValueChange={(value) => setRole(value ?? "all")}><SelectTrigger className="w-full bg-muted/60"><SelectValue /></SelectTrigger><SelectContent><SelectItem value="all">All roles</SelectItem>{platformRoles.map((item) => <SelectItem key={item.id} value={item.name}>{item.name}</SelectItem>)}</SelectContent></Select></div></section>
    <section className="overflow-hidden rounded-lg border bg-card"><div className="hidden grid-cols-[minmax(240px,1.2fr)_180px_180px_120px_110px] gap-3 border-b px-4 py-3 text-xs font-medium uppercase text-muted-foreground md:grid"><span>Employee</span><span>Role</span><span>Centre</span><span>Status</span><span>Action</span></div>{filtered.map((employee) => { const assignedCentre = centres.find((item) => item.id === employee.centreId); return <div key={employee.id} className="grid gap-3 border-b px-4 py-4 md:grid-cols-[minmax(240px,1.2fr)_180px_180px_120px_110px] md:items-center"><div className="flex min-w-0 items-center gap-3"><div className="flex size-9 shrink-0 items-center justify-center rounded-full bg-primary/10 text-primary"><UserRound className="size-4" /></div><div className="min-w-0"><p className="font-medium">{employee.name}</p><p className="truncate text-sm text-muted-foreground">{employee.email} · {employee.id}</p></div></div><p className="text-sm">{employee.role}</p><p className="text-sm text-muted-foreground">{assignedCentre?.name ?? "Organization-wide"}</p><StatusBadge label={employee.status} tone={employee.status === "Active" ? "good" : employee.status === "Suspended" ? "danger" : "warning"} /><Button size="sm" variant="outline" onClick={() => toggleEmployee(employee.id)}>{employee.status === "Suspended" ? "Reactivate" : "Suspend"}</Button></div>; })}{filtered.length === 0 && <p className="p-8 text-center text-sm text-muted-foreground">No employees match these filters.</p>}</section>
  </main>;
}

function Field({ name, label, placeholder, type = "text" }: { name: string; label: string; placeholder: string; type?: string }) {
  return <label className="grid gap-1.5 text-sm font-medium">{label}<Input name={name} type={type} placeholder={placeholder} required /></label>;
}
