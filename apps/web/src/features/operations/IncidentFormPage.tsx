import { useSearchParams, useNavigate } from "react-router";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { PageHeading } from "@/components/shared/PageHeading";
import { useAuth } from "@/hooks/useAuth";
import { useDispatchMock } from "@/context/DispatchMockContext";
import type { DispatchIncident } from "@/mock/dispatch";

export function IncidentFormPage() {
  const { user } = useAuth();
  const { trips, addIncident } = useDispatchMock();
  const [params] = useSearchParams();
  const navigate = useNavigate();
  const scopedTrips = trips.filter((trip) => trip.centreId === user?.centreId && trip.status !== "Cancelled");
  return <main className="flex flex-1 flex-col gap-4 bg-muted/20 p-4"><PageHeading title="Report incident" description="Connect the issue to a trip so dispatch staff can respond with full context." /><form className="max-w-3xl rounded-lg border bg-card p-5" onSubmit={(event) => { event.preventDefault(); const form = new FormData(event.currentTarget); const incident: DispatchIncident = { id: `INC-${Date.now().toString().slice(-4)}`, centreId: user?.centreId ?? "makumbura", tripId: String(form.get("tripId")) || undefined, category: String(form.get("category")), title: String(form.get("title")), severity: String(form.get("severity")) as DispatchIncident["severity"], status: "Open", owner: String(form.get("owner")), reportedAt: "Just now" }; addIncident(incident); navigate("/operations/incidents"); }}><div className="grid gap-4 sm:grid-cols-2"><label className="grid gap-1.5 text-sm font-medium">Related trip<Select name="tripId" defaultValue={params.get("tripId") ?? scopedTrips[0]?.id}><SelectTrigger className="w-full bg-muted/60"><SelectValue /></SelectTrigger><SelectContent>{scopedTrips.map((trip) => <SelectItem key={trip.id} value={trip.id}>{trip.id} · Route {trip.route} · {trip.scheduledTime}</SelectItem>)}</SelectContent></Select></label><label className="grid gap-1.5 text-sm font-medium">Category<Select name="category" defaultValue="Delay"><SelectTrigger className="w-full bg-muted/60"><SelectValue /></SelectTrigger><SelectContent>{["Delay", "Vehicle", "Passenger", "Safety", "Bay conflict"].map((item) => <SelectItem key={item} value={item}>{item}</SelectItem>)}</SelectContent></Select></label><label className="grid gap-1.5 text-sm font-medium sm:col-span-2">Incident summary<Input name="title" placeholder="Describe the operational issue" required /></label><label className="grid gap-1.5 text-sm font-medium">Severity<Select name="severity" defaultValue="Medium"><SelectTrigger className="w-full bg-muted/60"><SelectValue /></SelectTrigger><SelectContent>{["Low", "Medium", "High"].map((item) => <SelectItem key={item} value={item}>{item}</SelectItem>)}</SelectContent></Select></label><label className="grid gap-1.5 text-sm font-medium">Assigned owner<Input name="owner" placeholder="Dispatch desk" required /></label></div><div className="mt-5 flex justify-end gap-2"><Button type="button" variant="outline" onClick={() => navigate(-1)}>Cancel</Button><Button type="submit">Report incident</Button></div></form></main>;
}
