import { useState } from "react";
import { Accessibility, ArchiveRestore, Clock3, UsersRound } from "lucide-react";
import { Button } from "@/components/ui/button";
import { PageHeading } from "@/components/shared/PageHeading";
import { StatusBadge } from "@/components/shared/StatusBadge";
import { useAuth } from "@/hooks/useAuth";
import { assistanceCases, passengerZones } from "@/mock/centre-operations";

export function PassengerFlowPage() {
  const { user } = useAuth();
  const zones = passengerZones.filter((item) => item.centreId === user?.centreId);
  const total = zones.reduce((sum, zone) => sum + zone.passengers, 0);
  return <main className="flex flex-1 flex-col gap-4 bg-muted/20 p-4"><PageHeading title="Passenger flow" description="Monitor terminal crowding, queues, and waiting times before they disrupt boarding." /><section className="grid gap-3 sm:grid-cols-3"><Metric icon={UsersRound} label="Passengers in terminal" value={String(total)} /><Metric icon={Clock3} label="Longest wait" value={zones.sort((a, b) => Number.parseInt(b.wait) - Number.parseInt(a.wait))[0]?.wait ?? "—"} /><Metric icon={Accessibility} label="Assistance requests" value={String(assistanceCases.filter((item) => item.centreId === user?.centreId && item.type === "Accessibility" && item.status !== "Resolved").length)} /></section><section className="grid gap-4 xl:grid-cols-3">{zones.map((zone) => <article key={zone.id} className="rounded-lg border bg-card p-5"><div className="flex items-start justify-between"><div><p className="text-xs text-muted-foreground">{zone.id}</p><h2 className="mt-1 font-semibold">{zone.name}</h2></div><StatusBadge label={zone.level} tone={zone.level === "Critical" ? "danger" : zone.level === "Busy" ? "warning" : "good"} /></div><p className="mt-5 text-3xl font-semibold">{zone.passengers}</p><p className="text-sm text-muted-foreground">estimated passengers</p><div className="mt-5 grid grid-cols-2 gap-3 border-t pt-4 text-sm"><div><p className="text-xs text-muted-foreground">Average wait</p><p className="mt-1 font-medium">{zone.wait}</p></div><div><p className="text-xs text-muted-foreground">Trend</p><p className="mt-1 font-medium">{zone.trend}</p></div></div></article>)}</section></main>;
}

export function AssistancePage() {
  const { user } = useAuth();
  const scoped = assistanceCases.filter((item) => item.centreId === user?.centreId);
  const [statuses, setStatuses] = useState<Record<string, "Open" | "Assigned" | "Resolved">>({});
  return <main className="flex flex-1 flex-col gap-4 bg-muted/20 p-4"><PageHeading title="Passenger assistance" description="Coordinate accessibility requests and lost-property cases within this centre." action={<Button>Log new case</Button>} /><section className="overflow-hidden rounded-lg border bg-card"><div className="hidden grid-cols-[110px_150px_minmax(240px,1fr)_140px_160px] gap-4 border-b px-4 py-3 text-xs font-medium uppercase text-muted-foreground md:grid"><span>Case</span><span>Type</span><span>Details</span><span>Status</span><span>Action</span></div>{scoped.map((item) => { const status = statuses[item.id] ?? item.status; return <div key={item.id} className="grid gap-3 border-b px-4 py-4 md:grid-cols-[110px_150px_minmax(240px,1fr)_140px_160px] md:items-center"><p className="font-medium">{item.id}</p><p className="flex items-center gap-2 text-sm">{item.type === "Accessibility" ? <Accessibility className="size-4 text-blue-600" /> : <ArchiveRestore className="size-4 text-amber-600" />}{item.type}</p><div><p className="text-sm font-medium">{item.passenger}</p><p className="text-sm text-muted-foreground">{item.detail} · {item.owner}</p></div><StatusBadge label={status} tone={status === "Resolved" ? "good" : status === "Assigned" ? "warning" : "danger"} /><Button size="sm" variant="outline" disabled={status === "Resolved"} onClick={() => setStatuses((current) => ({ ...current, [item.id]: status === "Open" ? "Assigned" : "Resolved" }))}>{status === "Open" ? "Assign case" : status === "Assigned" ? "Resolve" : "Completed"}</Button></div>; })}</section></main>;
}

function Metric({ icon: Icon, label, value }: { icon: typeof UsersRound; label: string; value: string }) { return <article className="rounded-lg border bg-card p-4"><Icon className="size-4 text-primary" /><p className="mt-3 text-sm text-muted-foreground">{label}</p><p className="mt-1 text-2xl font-semibold">{value}</p></article>; }
