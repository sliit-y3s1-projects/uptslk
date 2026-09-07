import { useState } from "react";
import { Plus, UserRound } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { PageHeading } from "@/components/shared/PageHeading";
import { StatusBadge } from "@/components/shared/StatusBadge";
import { centres } from "@/mock/centres";
import { useAuth } from "@/hooks/useAuth";

type TeamMember = { id: string; name: string; email: string; role: string; centre: string; };

const initialMembers: TeamMember[] = [
  { id: "USR-001", name: "Nimal Perera", email: "nimal@upts.lk", role: "Centre manager", centre: "Makumbura MMC" },
  { id: "USR-002", name: "Shanthi Silva", email: "shanthi@upts.lk", role: "Dispatch officer", centre: "Kadawatha MMC" },
  { id: "USR-003", name: "Ayesha Fernando", email: "ayesha@upts.lk", role: "Super admin", centre: "National overview" },
];

export function TeamPage() {
  const { user } = useAuth();
  const assignedCentre = centres.find((item) => item.id === user?.centreId) ?? centres[0];
  const [members, setMembers] = useState(initialMembers);
  const [showForm, setShowForm] = useState(false);
  const localMembers = members.filter((member) => member.centre === assignedCentre.name);
  return <main className="flex flex-1 flex-col gap-4 bg-muted/20 p-4"><PageHeading title={`${assignedCentre.name} team`} description="Manage operational employees assigned to this centre. Organization roles remain controlled by the Super Admin." action={<Button onClick={() => setShowForm((value) => !value)}><Plus /> Add centre user</Button>} />{showForm && <UserForm centre={assignedCentre.name} onAdd={(member) => { setMembers((current) => [{ ...member, id: `USR-${String(current.length + 1).padStart(3, "0")}` }, ...current]); setShowForm(false); }} onCancel={() => setShowForm(false)} />}<section className="overflow-hidden rounded-lg border bg-card"><div className="grid grid-cols-[minmax(0,1.3fr)_minmax(0,1fr)_150px] gap-3 border-b px-4 py-3 text-xs font-medium uppercase text-muted-foreground"><span>User</span><span>Centre scope</span><span>Role</span></div>{localMembers.map((member) => <div key={member.id} className="grid grid-cols-[minmax(0,1.3fr)_minmax(0,1fr)_150px] gap-3 border-b px-4 py-4 text-sm"><div className="flex min-w-0 items-center gap-3"><div className="flex size-8 shrink-0 items-center justify-center rounded-full bg-primary/10 text-primary"><UserRound className="size-4" /></div><div className="min-w-0"><p className="font-medium">{member.name}</p><p className="truncate text-muted-foreground">{member.email}</p></div></div><p className="self-center text-muted-foreground">{member.centre}</p><div className="self-center"><StatusBadge label={member.role} tone="good" /></div></div>)}</section></main>;
}

function UserForm({ centre, onAdd, onCancel }: { centre: string; onAdd: (member: Omit<TeamMember, "id">) => void; onCancel: () => void; }) {
  const [role, setRole] = useState("Dispatch officer");
  return <form className="rounded-lg border bg-card p-5" onSubmit={(event) => { event.preventDefault(); const form = new FormData(event.currentTarget); onAdd({ name: String(form.get("name") || "New user"), email: String(form.get("email") || "user@upts.lk"), role, centre }); }}><h2 className="font-semibold">Create centre user</h2><div className="mt-4 grid gap-4 md:grid-cols-2 xl:grid-cols-4"><label className="grid gap-1.5 text-sm font-medium">Full name<Input name="name" placeholder="Full name" required /></label><label className="grid gap-1.5 text-sm font-medium">Email<Input name="email" type="email" placeholder="name@upts.lk" required /></label><label className="grid gap-1.5 text-sm font-medium">Role<Select value={role} onValueChange={(value) => { if (value) setRole(value); }}><SelectTrigger className="w-full bg-muted/60"><SelectValue /></SelectTrigger><SelectContent><SelectItem value="Dispatch officer">Dispatch officer</SelectItem><SelectItem value="Bay coordinator">Bay coordinator</SelectItem><SelectItem value="Fleet officer">Fleet officer</SelectItem><SelectItem value="Safety officer">Safety officer</SelectItem></SelectContent></Select></label><label className="grid gap-1.5 text-sm font-medium">Centre<Input value={centre} disabled /></label></div><div className="mt-5 flex justify-end gap-2"><Button type="button" variant="outline" onClick={onCancel}>Cancel</Button><Button type="submit">Create mock user</Button></div></form>;
}
