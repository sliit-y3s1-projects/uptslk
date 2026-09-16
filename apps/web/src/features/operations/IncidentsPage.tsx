import { Plus, ShieldAlert } from "lucide-react";
import { Link } from "react-router";
import { Button } from "@/components/ui/button";
import { PageHeading } from "@/components/shared/PageHeading";
import { StatusBadge } from "@/components/shared/StatusBadge";
import { useAuth } from "@/hooks/useAuth";
import { useIncidents, useUpdateIncident } from "./hooks/useIncidents";

export function IncidentsPage() {
  const { user } = useAuth();
  const { data: incidents = [], isLoading, error } = useIncidents(user?.centreId);
  const updateMutation = useUpdateIncident();
  if (isLoading) return <main className="p-5">Loading incidents...</main>;
  if (error) return <main className="p-5 text-red-600">Failed to load incidents.</main>;
  return <main className="flex flex-1 flex-col gap-4 bg-muted/20 p-4"><PageHeading title="Incidents" description="Triage operational and safety issues connected to centre trips." action={<Button render={<Link to="/operations/incidents/new" />}><Plus /> Report incident</Button>} /><section className="grid gap-3 lg:grid-cols-3">{["Open", "InProgress", "Resolved"].map((status) => <article key={status} className="rounded-lg border bg-card"><header className="flex items-center justify-between border-b px-4 py-3"><h2 className="font-semibold">{status === "InProgress" ? "Investigating" : status}</h2><span className="text-xs text-muted-foreground">{incidents.filter((item) => item.status === status).length}</span></header><div className="space-y-2 p-3">{incidents.filter((item) => item.status === status).map((incident) => <div key={incident.id} className="rounded-md border p-3"><div className="flex items-start justify-between gap-2"><div className="flex gap-2"><ShieldAlert className="mt-0.5 size-4 text-primary" /><div><p className="text-sm font-medium">{incident.title}</p><p className="mt-1 text-xs text-muted-foreground">{incident.id} · {incident.tripId ?? "Centre-wide"}</p></div></div><StatusBadge label={incident.severity} tone={incident.severity === "High" ? "danger" : incident.severity === "Medium" ? "warning" : "neutral"} /></div><p className="mt-3 text-xs text-muted-foreground">{incident.assignedTo ?? incident.reportedByName} · {new Date(incident.createdAt).toLocaleString()}</p>{incident.status !== "Resolved" && <Button className="mt-3 w-full" size="sm" variant="outline" disabled={updateMutation.isPending} onClick={() => updateMutation.mutate({ id: incident.id, data: { severity: incident.severity, title: incident.title, description: incident.title, assignedTo: incident.assignedTo, status: incident.status === "Open" ? "InProgress" : "Resolved", slaDueAt: incident.slaDueAt } })}>{incident.status === "Open" ? "Begin investigation" : "Resolve incident"}</Button>}</div>)}</div></article>)}</section></main>;
}
