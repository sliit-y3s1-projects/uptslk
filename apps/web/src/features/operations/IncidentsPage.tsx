import { Plus, ShieldAlert } from "lucide-react";
import { Link } from "react-router";
import { Button } from "@/components/ui/button";
import { PageHeading } from "@/components/shared/PageHeading";
import { StatusBadge } from "@/components/shared/StatusBadge";
import { useAuth } from "@/hooks/useAuth";
import { useDispatchMock } from "@/context/DispatchMockContext";

export function IncidentsPage() {
  const { user } = useAuth();
  const { incidents, updateIncident } = useDispatchMock();
  const scoped = incidents.filter((item) => item.centreId === user?.centreId);
  return <main className="flex flex-1 flex-col gap-4 bg-muted/20 p-4"><PageHeading title="Incidents" description="Triage operational and safety issues connected to centre trips." action={<Button render={<Link to="/operations/incidents/new" />}><Plus /> Report incident</Button>} /><section className="grid gap-3 lg:grid-cols-3">{["Open", "Investigating", "Resolved"].map((status) => <article key={status} className="rounded-lg border bg-card"><header className="flex items-center justify-between border-b px-4 py-3"><h2 className="font-semibold">{status}</h2><span className="text-xs text-muted-foreground">{scoped.filter((item) => item.status === status).length}</span></header><div className="space-y-2 p-3">{scoped.filter((item) => item.status === status).map((incident) => <div key={incident.id} className="rounded-md border p-3"><div className="flex items-start justify-between gap-2"><div className="flex gap-2"><ShieldAlert className="mt-0.5 size-4 text-primary" /><div><p className="text-sm font-medium">{incident.title}</p><p className="mt-1 text-xs text-muted-foreground">{incident.id} · {incident.tripId ?? "Centre-wide"}</p></div></div><StatusBadge label={incident.severity} tone={incident.severity === "High" ? "danger" : incident.severity === "Medium" ? "warning" : "neutral"} /></div><p className="mt-3 text-xs text-muted-foreground">{incident.owner} · {incident.reportedAt}</p>{incident.status !== "Resolved" && <Button className="mt-3 w-full" size="sm" variant="outline" onClick={() => updateIncident(incident.id, incident.status === "Open" ? "Investigating" : "Resolved")}>{incident.status === "Open" ? "Begin investigation" : "Resolve incident"}</Button>}</div>)}</div></article>)}</section></main>;
}
