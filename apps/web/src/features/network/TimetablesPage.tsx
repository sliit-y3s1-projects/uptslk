import { useState } from "react";
import { CalendarClock, Loader2, Pencil, Plus, Sparkles, Trash2 } from "lucide-react";
import { Link, useSearchParams } from "react-router";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { DatePicker } from "@/components/custom/DatePicker";
import { TimePicker } from "@/components/custom/TimePicker";
import { PageHeading } from "@/components/shared/PageHeading";
import { StatusBadge } from "@/components/shared/StatusBadge";
import { useAuth } from "@/hooks/useAuth";
import { useBays } from "@/features/centres/hooks/useCentres";
import {
  useCreateSchedule,
  useDeactivateSchedule,
  useGenerateScheduleTrips,
  useRoutes,
  useSchedules,
  useUpdateSchedule,
} from "./hooks/useRoutes";
import type { GenerateScheduleTripsResult, RouteSchedule } from "./types";

const today = new Date().toISOString().slice(0, 10);
const time = (value: string) => value.slice(0, 5);

export function TimetablesPage() {
  const { user } = useAuth();
  const [searchParams, setSearchParams] = useSearchParams();
  const routeId = searchParams.get("route") ?? "";
  const directionId = searchParams.get("direction") ?? "";
  const [scheduleDialog, setScheduleDialog] = useState<RouteSchedule | "new" | null>(null);
  const [generateSchedule, setGenerateSchedule] = useState<RouteSchedule | null>(null);
  const [generationResult, setGenerationResult] = useState<GenerateScheduleTripsResult | null>(null);
  const { data: routes = [], isLoading: loadingRoutes } = useRoutes(user?.centreId);
  const route = routes.find((item) => item.id === routeId);
  const direction = route?.directions?.find((item) => item.id === directionId) ?? route?.directions?.[0];
  const activeDirectionId = direction?.id ?? "";
  const { data: bays = [] } = useBays(direction?.startCentreId ?? route?.centreId ?? user?.centreId);
  const { data: schedules = [], isLoading, error } = useSchedules(routeId || undefined);
  const createSchedule = useCreateSchedule(routeId);
  const updateSchedule = useUpdateSchedule(routeId);
  const deactivateSchedule = useDeactivateSchedule(routeId);
  const generateTrips = useGenerateScheduleTrips(routeId);

  const updateRoute = (value: string | null) => {
    if (value) {
      const nextRoute = routes.find((item) => item.id === value);
      const nextDirection = nextRoute?.directions?.[0]?.id;
      setSearchParams(nextDirection ? { route: value, direction: nextDirection } : { route: value });
    }
  };

  const updateDirection = (value: string | null) => {
    if (value && routeId) setSearchParams({ route: routeId, direction: value });
  };

  const saveSchedule = (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    const data = {
      bayId: String(form.get("bayId")),
      routeDirectionId: activeDirectionId,
      firstDeparture: `${form.get("firstDeparture")}:00`,
      lastDeparture: `${form.get("lastDeparture")}:00`,
      headwayMinutes: Number(form.get("headwayMinutes")),
      operatingDays: String(form.get("operatingDays")),
    };
    const close = () => setScheduleDialog(null);
    if (scheduleDialog && scheduleDialog !== "new") {
      updateSchedule.mutate({ scheduleId: scheduleDialog.id, data: { ...data, isActive: scheduleDialog.isActive } }, { onSuccess: close });
    } else {
      createSchedule.mutate(data, { onSuccess: close });
    }
  };

  const generate = (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!generateSchedule) return;
    const date = String(new FormData(event.currentTarget).get("serviceDate"));
    generateTrips.mutate({ scheduleId: generateSchedule.id, data: { serviceDate: date } }, {
      onSuccess: (result) => {
        setGenerationResult(result);
        setGenerateSchedule(null);
      },
    });
  };

  return (
    <main className="flex flex-1 flex-col gap-5 bg-muted/20 p-4">
      <PageHeading
        title="Timetables & schedules"
        description="Timetables are repeating plans. Generate a day’s departures, then manage them on the Dispatch board."
        action={<Button onClick={() => setScheduleDialog("new")} disabled={!activeDirectionId}><Plus /> Add timetable</Button>}
      />

      <section className="rounded-xl border border-slate-300 bg-card p-5">
        <p className="mb-2 text-sm font-medium">Route</p>
        <Select
          value={routeId || null}
          onValueChange={updateRoute}
          itemToStringLabel={(value) => routes.find((item) => item.id === value) ? `${routes.find((item) => item.id === value)?.routeNumber} · ${routes.find((item) => item.id === value)?.name}` : value}
        >
          <SelectTrigger className="w-full max-w-xl bg-muted/30"><SelectValue placeholder={loadingRoutes ? "Loading routes..." : "Choose a route"} /></SelectTrigger>
          <SelectContent>{routes.map((item) => <SelectItem key={item.id} value={item.id}>{item.routeNumber} · {item.name}</SelectItem>)}</SelectContent>
        </Select>
        {route && <div className="mt-4 grid gap-3 border-t pt-4 text-sm sm:grid-cols-3"><label className="grid gap-1.5"><span className="text-muted-foreground">Travel direction</span><Select value={activeDirectionId || null} onValueChange={updateDirection} itemToStringLabel={(value) => route.directions?.find((item) => item.id === value)?.name ?? value}><SelectTrigger className="w-full"><SelectValue placeholder="Add a route direction first" /></SelectTrigger><SelectContent>{route.directions?.map((item) => <SelectItem key={item.id} value={item.id}>{item.startCentre.name} → {item.endCentre.name}</SelectItem>)}</SelectContent></Select></label><div><p className="text-muted-foreground">Departure terminal</p><p className="font-medium">{direction?.startCentre.name ?? "—"}</p></div><div><p className="text-muted-foreground">Service duration</p><p className="font-medium">{direction ? `${direction.estimatedDurationMin} minutes` : "—"}</p></div></div>}
      </section>

      {generationResult && <GenerationSummary result={generationResult} />}
      {!routeId ? <EmptyState text="Choose a route to manage its recurring timetables." /> : !activeDirectionId ? <EmptyState text="Add a travel direction to this route before creating a timetable." /> : error ? <EmptyState text="Could not load timetable data. Please try again." /> : isLoading ? <div className="flex justify-center py-16"><Loader2 className="animate-spin text-primary" /></div> : (
        <section className="rounded-xl border border-slate-300 bg-card">
          <header className="flex flex-col gap-1 border-b border-slate-300 px-5 py-4 sm:flex-row sm:items-center sm:justify-between"><div><h2 className="font-semibold">Recurring timetables</h2><p className="text-sm text-muted-foreground">Each timetable is a service pattern for this direction. Generate trips when you are ready to dispatch a date.</p></div><span className="text-sm text-muted-foreground">{schedules.filter((schedule) => schedule.routeDirectionId === activeDirectionId).length} total</span></header>
          {schedules.filter((schedule) => schedule.routeDirectionId === activeDirectionId).length === 0 ? <EmptyState text="No timetable patterns yet for this travel direction." /> : <div className="divide-y divide-slate-200">{schedules.filter((schedule) => schedule.routeDirectionId === activeDirectionId).map((schedule) => <ScheduleRow key={schedule.id} schedule={schedule} onEdit={() => setScheduleDialog(schedule)} onGenerate={() => setGenerateSchedule(schedule)} onDeactivate={() => deactivateSchedule.mutate(schedule.id)} busy={deactivateSchedule.isPending} />)}</div>}
        </section>
      )}

      <Dialog open={scheduleDialog !== null} onOpenChange={(open) => !open && setScheduleDialog(null)}>
        <DialogContent className="sm:max-w-lg">
          <DialogHeader><DialogTitle>{scheduleDialog === "new" ? "Add recurring timetable" : "Edit recurring timetable"}</DialogTitle><DialogDescription>A timetable is the planned service pattern. It does not change previously generated trips.</DialogDescription></DialogHeader>
          <ScheduleForm schedule={scheduleDialog === "new" ? undefined : scheduleDialog ?? undefined} bays={bays.filter((bay) => bay.status === "Available")} onSubmit={saveSchedule} pending={createSchedule.isPending || updateSchedule.isPending} onCancel={() => setScheduleDialog(null)} />
        </DialogContent>
      </Dialog>

      <Dialog open={generateSchedule !== null} onOpenChange={(open) => !open && setGenerateSchedule(null)}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader><DialogTitle>Generate daily trips</DialogTitle><DialogDescription>Creates the day’s departures from this timetable. The system chooses available buses and drivers, and does not duplicate a departure already on the board.</DialogDescription></DialogHeader>
          <form className="grid gap-4" onSubmit={generate}><label className="grid gap-1.5 text-sm font-medium">Service date<DatePicker name="serviceDate" defaultValue={today} required /></label>{generateTrips.error && <p className="text-sm text-destructive">Unable to generate trips. Check vehicle, driver, bay availability, and conflicts.</p>}<div className="flex justify-end gap-2"><Button type="button" variant="outline" onClick={() => setGenerateSchedule(null)}>Cancel</Button><Button type="submit" disabled={generateTrips.isPending}>{generateTrips.isPending ? "Generating..." : "Generate trips"}</Button></div></form>
        </DialogContent>
      </Dialog>
    </main>
  );
}

function ScheduleRow({ schedule, onEdit, onGenerate, onDeactivate, busy }: { schedule: RouteSchedule; onEdit: () => void; onGenerate: () => void; onDeactivate: () => void; busy: boolean }) {
  return <article className="flex flex-col gap-4 px-5 py-4 lg:flex-row lg:items-center lg:justify-between"><div className="flex min-w-0 items-start gap-3"><div className="mt-0.5 rounded-md bg-muted p-2 text-primary"><CalendarClock className="size-4" /></div><div><div className="flex flex-wrap items-center gap-2"><h3 className="font-medium">{schedule.operatingDays}</h3><StatusBadge label={schedule.isActive ? "Active" : "Archived"} tone={schedule.isActive ? "good" : "neutral"} /></div><p className="mt-1 text-sm text-muted-foreground">Every {schedule.headwayMinutes} min · Bay {schedule.bayCode}</p><p className="mt-1 text-sm font-medium">{time(schedule.firstDeparture)} – {time(schedule.lastDeparture)}</p></div></div><div className="flex flex-wrap gap-2">{schedule.isActive && <><Button size="sm" variant="outline" onClick={onEdit}><Pencil /> Edit</Button><Button size="sm" onClick={onGenerate}><Sparkles /> Generate trips</Button><Button size="sm" variant="outline" className="text-destructive hover:bg-destructive/10" onClick={onDeactivate} disabled={busy}><Trash2 /> Deactivate</Button></>}</div></article>;
}

function ScheduleForm({ schedule, bays, onSubmit, pending, onCancel }: { schedule?: RouteSchedule; bays: { id: string; code: string; status: string }[]; onSubmit: (event: React.FormEvent<HTMLFormElement>) => void; pending: boolean; onCancel: () => void }) {
  return <form className="grid gap-4" onSubmit={onSubmit}><div className="grid gap-4 sm:grid-cols-2"><label className="grid gap-1.5 text-sm font-medium">Departure bay<Select name="bayId" defaultValue={schedule?.bayId ?? bays[0]?.id} required itemToStringLabel={(value) => bays.find((bay) => bay.id === value)?.code ?? value}><SelectTrigger><SelectValue placeholder="Select bay" /></SelectTrigger><SelectContent>{bays.map((bay) => <SelectItem key={bay.id} value={bay.id}>{bay.code}</SelectItem>)}</SelectContent></Select></label><label className="grid gap-1.5 text-sm font-medium">Operating days<Select name="operatingDays" defaultValue={schedule?.operatingDays ?? "Everyday"}><SelectTrigger><SelectValue /></SelectTrigger><SelectContent><SelectItem value="Everyday">Every day</SelectItem><SelectItem value="Weekdays">Weekdays</SelectItem><SelectItem value="Weekends">Weekends</SelectItem></SelectContent></Select></label><label className="grid gap-1.5 text-sm font-medium">First departure<TimePicker name="firstDeparture" defaultValue={schedule ? time(schedule.firstDeparture) : "06:00"} required /></label><label className="grid gap-1.5 text-sm font-medium">Last departure<TimePicker name="lastDeparture" defaultValue={schedule ? time(schedule.lastDeparture) : "18:30"} required /></label><label className="grid gap-1.5 text-sm font-medium sm:col-span-2">Departure interval (minutes)<Select name="headwayMinutes" defaultValue={String(schedule?.headwayMinutes ?? 30)}><SelectTrigger><SelectValue /></SelectTrigger><SelectContent>{[10, 15, 20, 30, 45, 60].map((minutes) => <SelectItem key={minutes} value={String(minutes)}>Every {minutes} minutes</SelectItem>)}</SelectContent></Select></label></div><div className="flex justify-end gap-2"><Button type="button" variant="outline" onClick={onCancel}>Cancel</Button><Button type="submit" disabled={pending}>{pending ? "Saving..." : "Save timetable"}</Button></div></form>;
}

function EmptyState({ text }: { text: string }) { return <div className="rounded-xl border border-dashed border-slate-300 bg-card px-6 py-14 text-center text-sm text-muted-foreground">{text}</div>; }

function GenerationSummary({ result }: { result: GenerateScheduleTripsResult }) {
  const date = new Date(`${result.serviceDate}T00:00:00`).toLocaleDateString(undefined, { weekday: "long", day: "numeric", month: "long" });
  return <section className="rounded-xl border border-slate-300 bg-card p-5"><div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between"><div><h2 className="font-semibold">Trip generation summary</h2><p className="text-sm text-muted-foreground">{date} · Generated departures appear on the Dispatch board.</p></div><Button variant="outline" render={<Link to={`/operations/dispatch?date=${result.serviceDate}`} />}>View on Dispatch board</Button></div><div className="mt-4 grid gap-3 sm:grid-cols-3"><SummaryStat label="Added to dispatch" value={result.created} detail="Ready for the dispatch board" tone="green" /><SummaryStat label="Already on the board" value={result.existing} detail="Left unchanged" tone="neutral" /><SummaryStat label="Needs attention" value={result.conflicts} detail="Could not be assigned automatically" tone="amber" /></div>{result.createdDepartures.length > 0 && <div className="mt-4 border-t border-slate-200 pt-4"><p className="text-sm font-medium">Departures added</p><div className="mt-2 flex flex-wrap gap-2">{result.createdDepartures.map((departure) => <span key={departure} className="rounded-md border border-emerald-200 bg-emerald-50 px-2.5 py-1 text-sm text-emerald-800">{departure}</span>)}</div></div>}{result.skippedDepartures.length > 0 && <div className="mt-4 border-t border-slate-200 pt-4"><p className="text-sm font-medium">Departures to review</p><p className="mt-1 text-sm text-muted-foreground">Use the Dispatch board to assign a different bus, driver, or bay for these times.</p><div className="mt-2 divide-y rounded-lg border border-slate-300">{result.skippedDepartures.map((departure) => <div key={`${departure.time}-${departure.reason}`} className="flex items-center justify-between gap-4 px-3 py-2 text-sm"><span className="font-medium">{departure.time}</span><span className="text-right text-muted-foreground">{departure.reason}</span></div>)}</div></div>}</section>;
}

function SummaryStat({ label, value, detail, tone }: { label: string; value: number; detail: string; tone: "green" | "neutral" | "amber" }) {
  const colours = tone === "green" ? "border-emerald-300 bg-emerald-50 text-emerald-900" : tone === "amber" ? "border-amber-300 bg-amber-50 text-amber-900" : "border-slate-300 bg-slate-50 text-slate-900";
  return <div className={`rounded-lg border p-3 ${colours}`}><p className="text-sm font-medium">{label}</p><p className="mt-1 text-2xl font-semibold">{value}</p><p className="mt-1 text-xs opacity-75">{detail}</p></div>;
}
