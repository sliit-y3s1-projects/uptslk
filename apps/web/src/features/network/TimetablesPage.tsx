import { useState } from "react";
import { ArrowRight, CalendarClock, Loader2, Pencil, Plus, Sparkles, Trash2 } from "lucide-react";
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
import { useTrips } from "@/features/operations/hooks/useTrips";
import type { TripListItem } from "@/features/operations/types/trips";
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
const tripTime = (value: string) => new Date(value).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" });

export function TimetablesPage() {
  const { user } = useAuth();
  const [searchParams, setSearchParams] = useSearchParams();
  const routeId = searchParams.get("route") ?? "";
  const directionId = searchParams.get("direction") ?? "";
  const reviewDate = searchParams.get("date") ?? today;
  const [scheduleDialog, setScheduleDialog] = useState<RouteSchedule | "new" | null>(null);
  const [generateSchedule, setGenerateSchedule] = useState<RouteSchedule | null>(null);
  const [generationResult, setGenerationResult] = useState<GenerateScheduleTripsResult | null>(null);
  const [generationDirection, setGenerationDirection] = useState<RouteDirectionContext | null>(null);
  const [tripSummary, setTripSummary] = useState<{ direction: RouteDirectionContext; trips: TripListItem[] } | null>(null);
  const { data: routes = [], isLoading: loadingRoutes } = useRoutes(user?.centreId);
  const route = routes.find((item) => item.id === routeId);
  const direction = route?.directions?.find((item) => item.id === directionId) ?? route?.directions?.[0];
  const activeDirectionId = direction?.id ?? "";
  const { data: bays = [] } = useBays(direction?.startCentreId ?? route?.centreId ?? user?.centreId);
  const { data: schedules = [], isLoading, error } = useSchedules(routeId || undefined);
  const { data: generatedTrips = [], isLoading: loadingGeneratedTrips } = useTrips({ routeId: routeId || undefined, date: reviewDate });
  const createSchedule = useCreateSchedule(routeId);
  const updateSchedule = useUpdateSchedule(routeId);
  const deactivateSchedule = useDeactivateSchedule(routeId);
  const generateTrips = useGenerateScheduleTrips(routeId);

  const updateRoute = (value: string | null) => {
    if (value) {
      const nextRoute = routes.find((item) => item.id === value);
      const nextDirection = nextRoute?.directions?.[0]?.id;
      setSearchParams(nextDirection ? { route: value, direction: nextDirection, date: reviewDate } : { route: value, date: reviewDate });
    }
  };

  const updateDirection = (value: string | null) => {
    if (value && routeId) setSearchParams({ route: routeId, direction: value, date: reviewDate });
  };

  const updateReviewDate = (value: string) => {
    setSearchParams(directionId || activeDirectionId ? { route: routeId, direction: directionId || activeDirectionId, date: value } : { route: routeId, date: value });
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
        setGenerationDirection(direction ? {
          startCentreName: direction.startCentre.name,
          endCentreName: direction.endCentre.name,
          bayCode: generateSchedule.bayCode,
        } : null);
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
        {route && <><div className="mt-4 flex flex-col gap-3 border-t pt-4 sm:flex-row sm:items-end sm:justify-between"><div><p className="text-sm font-medium">Daily trip check</p><p className="mt-1 text-sm text-muted-foreground">Choose a date to verify the actual departures already created for each direction.</p></div><label className="grid gap-1.5 text-sm font-medium"><span className="text-muted-foreground">Service date</span><DatePicker name="reviewDate" value={reviewDate} onValueChange={updateReviewDate} /></label></div><div className="mt-5"><p className="text-sm font-medium">Travel direction</p><p className="mt-1 text-sm text-muted-foreground">Create and generate a timetable for each direction separately.</p><div className="mt-3 grid gap-3 md:grid-cols-2">{route.directions?.map((item) => {
          const selected = item.id === activeDirectionId;
          const directionSchedules = schedules.filter((schedule) => schedule.routeDirectionId === item.id && schedule.isActive);
          const directionTrips = generatedTrips.filter((trip) => trip.routeDirectionId === item.id).sort((left, right) => left.scheduledTime.localeCompare(right.scheduledTime));
          const timetableCount = directionSchedules.length;
          return <button key={item.id} type="button" onClick={() => updateDirection(item.id)} className={`rounded-lg border p-4 text-left transition ${selected ? "border-primary bg-primary/5 ring-1 ring-primary" : "border-slate-300 bg-card hover:border-primary/50 hover:bg-muted/20"}`}>
            <div className="flex items-start justify-between gap-3"><div><p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">{selected ? "Selected direction" : "Direction"}</p><p className="mt-1 font-semibold">{item.startCentre.name} <ArrowRight className="mx-1 inline size-4 text-primary" /> {item.endCentre.name}</p></div><span className={`rounded-full px-2.5 py-1 text-xs font-medium ${selected ? "bg-primary text-primary-foreground" : "bg-muted text-muted-foreground"}`}>{timetableCount} timetable{timetableCount === 1 ? "" : "s"}</span></div>
            <p className="mt-2 text-sm text-muted-foreground">Departs from {item.startCentre.code} · {item.estimatedDurationMin} minutes</p>
            {directionSchedules.length > 0 ? <div className="mt-3 grid gap-1 rounded-md border border-slate-200 bg-background/70 px-3 py-2 text-sm"><p className="font-medium text-foreground">{time(directionSchedules[0].firstDeparture)} – {time(directionSchedules[0].lastDeparture)} · Every {directionSchedules[0].headwayMinutes} min</p><p className="text-muted-foreground">Departure bay {directionSchedules[0].bayCode} · {directionSchedules[0].operatingDays}</p>{directionSchedules.length > 1 && <p className="text-xs text-muted-foreground">+ {directionSchedules.length - 1} additional active timetable{directionSchedules.length === 2 ? "" : "s"}</p>}</div> : <p className="mt-3 rounded-md border border-dashed border-slate-300 px-3 py-2 text-sm text-muted-foreground">No timetable created for this direction yet.</p>}
            <div role={directionTrips.length > 0 ? "button" : undefined} tabIndex={directionTrips.length > 0 ? 0 : undefined} onClick={(event) => { if (directionTrips.length > 0) { event.stopPropagation(); setTripSummary({ direction: { startCentreName: item.startCentre.name, endCentreName: item.endCentre.name, bayCode: directionTrips[0].bay }, trips: directionTrips }); } }} onKeyDown={(event) => { if (directionTrips.length > 0 && (event.key === "Enter" || event.key === " ")) { event.preventDefault(); event.stopPropagation(); setTripSummary({ direction: { startCentreName: item.startCentre.name, endCentreName: item.endCentre.name, bayCode: directionTrips[0].bay }, trips: directionTrips }); } }} className={`mt-3 rounded-md border px-3 py-2 text-sm ${directionTrips.length > 0 ? "cursor-pointer border-emerald-200 bg-emerald-50 text-emerald-950 hover:border-emerald-400" : "border-slate-200 bg-muted/20 text-muted-foreground"}`}><p className="font-medium">{loadingGeneratedTrips ? "Checking generated trips…" : directionTrips.length > 0 ? `${directionTrips.length} trip${directionTrips.length === 1 ? "" : "s"} created for ${reviewDate}` : `No trips generated for ${reviewDate}`}</p>{!loadingGeneratedTrips && directionTrips.length > 0 && <p className="mt-1 text-emerald-800">{directionTrips.map((trip) => tripTime(trip.scheduledTime)).join(" · ")} · View details</p>}</div>
          </button>;
        })}</div></div></>}
      </section>

      {generationResult && <GenerationSummary result={generationResult} direction={generationDirection} />}
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
          {direction && generateSchedule && <div className="rounded-lg border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-950"><p className="font-semibold">{direction.startCentre.name} <ArrowRight className="mx-1 inline size-4" /> {direction.endCentre.name}</p><p className="mt-1 text-emerald-800">Generating from bay {generateSchedule.bayCode} for this direction only.</p></div>}
          <form className="grid gap-4" onSubmit={generate}><label className="grid gap-1.5 text-sm font-medium">Service date<DatePicker name="serviceDate" defaultValue={today} required /></label>{generateTrips.error && <p className="text-sm text-destructive">Unable to generate trips. Check vehicle, driver, bay availability, and conflicts.</p>}<div className="flex justify-end gap-2"><Button type="button" variant="outline" onClick={() => setGenerateSchedule(null)}>Cancel</Button><Button type="submit" disabled={generateTrips.isPending}>{generateTrips.isPending ? "Generating..." : "Generate trips"}</Button></div></form>
        </DialogContent>
      </Dialog>

      <Dialog open={tripSummary !== null} onOpenChange={(open) => !open && setTripSummary(null)}>
        <DialogContent className="max-h-[calc(100dvh-2rem)] overflow-y-auto sm:max-w-3xl">
          <DialogHeader><DialogTitle>Generated trips</DialogTitle><DialogDescription>{tripSummary && <>{tripSummary.direction.startCentreName} <ArrowRight className="mx-1 inline size-4" /> {tripSummary.direction.endCentreName} · Service date {reviewDate}</>}</DialogDescription></DialogHeader>
          {tripSummary && <div className="overflow-x-auto rounded-lg border border-slate-300"><table className="w-full min-w-[620px] text-sm"><thead className="bg-muted/50 text-left"><tr><th className="px-4 py-3 font-medium">Departure</th><th className="px-4 py-3 font-medium">Status</th><th className="px-4 py-3 font-medium">Bus</th><th className="px-4 py-3 font-medium">Driver</th><th className="px-4 py-3 font-medium">Bay</th><th className="px-4 py-3 font-medium">Capacity</th></tr></thead><tbody className="divide-y">{tripSummary.trips.map((trip) => <tr key={trip.id}><td className="px-4 py-3 font-semibold">{tripTime(trip.scheduledTime)}</td><td className="px-4 py-3"><StatusBadge label={trip.status} tone={trip.status === "Delayed" ? "danger" : trip.status === "Cancelled" ? "neutral" : "good"} /></td><td className="px-4 py-3">{trip.vehicle}</td><td className="px-4 py-3">{trip.driver}</td><td className="px-4 py-3">{trip.bay}</td><td className="px-4 py-3">{trip.available} available / {trip.capacity}</td></tr>)}</tbody></table></div>}
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

interface RouteDirectionContext {
  startCentreName: string;
  endCentreName: string;
  bayCode: string;
}

function GenerationSummary({ result, direction }: { result: GenerateScheduleTripsResult; direction: RouteDirectionContext | null }) {
  const date = new Date(`${result.serviceDate}T00:00:00`).toLocaleDateString(undefined, { weekday: "long", day: "numeric", month: "long" });
  return <section className="rounded-xl border border-slate-300 bg-card p-5"><div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between"><div><h2 className="font-semibold">Trip generation summary</h2><p className="text-sm text-muted-foreground">{date} · Generated departures appear on the Dispatch board.</p></div><Button variant="outline" render={<Link to={`/operations/dispatch?date=${result.serviceDate}`} />}>View on Dispatch board</Button></div>{direction && <div className="mt-4 rounded-lg border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-950"><span className="font-semibold">Generated direction: </span>{direction.startCentreName} <ArrowRight className="mx-1 inline size-4" /> {direction.endCentreName}<span className="text-emerald-800"> · Bay {direction.bayCode}</span></div>}<div className="mt-4 grid gap-3 sm:grid-cols-3"><SummaryStat label="Added to dispatch" value={result.created} detail="Ready for the dispatch board" tone="green" /><SummaryStat label="Already on the board" value={result.existing} detail="Left unchanged" tone="neutral" /><SummaryStat label="Needs attention" value={result.conflicts} detail="Could not be assigned automatically" tone="amber" /></div>{result.createdDepartures.length > 0 && <div className="mt-4 border-t border-slate-200 pt-4"><p className="text-sm font-medium">Departures added</p><div className="mt-2 flex flex-wrap gap-2">{result.createdDepartures.map((departure) => <span key={departure} className="rounded-md border border-emerald-200 bg-emerald-50 px-2.5 py-1 text-sm text-emerald-800">{departure}</span>)}</div></div>}{result.skippedDepartures.length > 0 && <div className="mt-4 border-t border-slate-200 pt-4"><p className="text-sm font-medium">Departures to review</p><p className="mt-1 text-sm text-muted-foreground">Use the Dispatch board to assign a different bus, driver, or bay for these times.</p><div className="mt-2 divide-y rounded-lg border border-slate-300">{result.skippedDepartures.map((departure) => <div key={`${departure.time}-${departure.reason}`} className="flex items-center justify-between gap-4 px-3 py-2 text-sm"><span className="font-medium">{departure.time}</span><span className="text-right text-muted-foreground">{departure.reason}</span></div>)}</div></div>}</section>;
}

function SummaryStat({ label, value, detail, tone }: { label: string; value: number; detail: string; tone: "green" | "neutral" | "amber" }) {
  const colours = tone === "green" ? "border-emerald-300 bg-emerald-50 text-emerald-900" : tone === "amber" ? "border-amber-300 bg-amber-50 text-amber-900" : "border-slate-300 bg-slate-50 text-slate-900";
  return <div className={`rounded-lg border p-3 ${colours}`}><p className="text-sm font-medium">{label}</p><p className="mt-1 text-2xl font-semibold">{value}</p><p className="mt-1 text-xs opacity-75">{detail}</p></div>;
}
