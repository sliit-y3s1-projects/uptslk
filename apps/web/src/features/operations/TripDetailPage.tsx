import { useState } from "react";
import { ArrowLeft, Ban, CheckCircle2, Pencil, ShieldAlert } from "lucide-react";
import { Link, useNavigate, useParams } from "react-router";
import { AlertDialog, AlertDialogAction, AlertDialogCancel, AlertDialogContent, AlertDialogDescription, AlertDialogFooter, AlertDialogHeader, AlertDialogTitle } from "@/components/ui/alert-dialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { PageHeading } from "@/components/shared/PageHeading";
import { StatusBadge } from "@/components/shared/StatusBadge";
import { useAuth } from "@/hooks/useAuth";
import { useDispatchMock } from "@/context/DispatchMockContext";
import { TripLifecycle } from "@/features/operations/components/TripLifecycle";

export function TripDetailPage() {
  const { tripId } = useParams();
  const navigate = useNavigate();
  const { user } = useAuth();
  const { trips, incidents, approvals, transitionTrip, cancelTrip } = useDispatchMock();
  const trip = trips.find((item) => item.id === tripId && item.centreId === user?.centreId);
  const [cancelOpen, setCancelOpen] = useState(false);
  const [reason, setReason] = useState("");
  if (!trip) return <main className="p-5">Trip not found in your centre.</main>;
  const next = trip.status === "Scheduled" ? "Ready" : trip.status === "Ready" || trip.status === "Delayed" ? "Boarding" : trip.status === "Boarding" ? "Dispatched" : trip.status === "Dispatched" ? "Completed" : undefined;
  const linkedIncidents = incidents.filter((item) => item.tripId === trip.id);
  const linkedApprovals = approvals.filter((item) => item.tripId === trip.id);
  return <main className="flex flex-1 flex-col gap-4 bg-muted/20 p-4"><PageHeading title={trip.id} description={`Route ${trip.route} · ${trip.origin} to ${trip.destination}`} action={<div className="flex flex-wrap gap-2"><Button variant="outline" render={<Link to="/operations/dispatch" />}><ArrowLeft /> Dispatch board</Button>{!["Completed", "Cancelled"].includes(trip.status) && <Button variant="outline" render={<Link to={`/operations/dispatch/${trip.id}/edit`} />}><Pencil /> Edit</Button>}{next && <Button onClick={() => transitionTrip(trip.id, next)}><CheckCircle2 /> Mark {next.toLowerCase()}</Button>}</div>} /><section className="grid gap-4 xl:grid-cols-[minmax(0,1.2fr)_360px]"><article className="rounded-lg border bg-card p-5"><div className="flex items-center justify-between"><h2 className="font-semibold">Trip lifecycle</h2><StatusBadge label={trip.status} tone={trip.status === "Cancelled" ? "danger" : trip.status === "Delayed" ? "warning" : "good"} /></div><TripLifecycle status={trip.status} /><div className="mt-7 grid gap-5 border-t pt-5 sm:grid-cols-2 lg:grid-cols-3"><Fact label="Date and time" value={`${trip.serviceDate} · ${trip.scheduledTime}`} /><Fact label="Assigned bay" value={trip.bay} /><Fact label="Vehicle" value={trip.vehicleId} /><Fact label="Driver" value={trip.driverId} /><Fact label="Occupancy" value={`${trip.occupancy}%`} /><Fact label="Last update" value={trip.updatedAt} /></div><div className="mt-5 rounded-md bg-muted/50 p-4"><p className="text-xs text-muted-foreground">Operational notes</p><p className="mt-1 text-sm">{trip.notes || "No notes recorded."}</p></div></article><aside className="space-y-4"><article className="rounded-lg border bg-card p-5"><h2 className="font-semibold">Related workflows</h2><div className="mt-4 grid gap-2"><Button variant="outline" render={<Link to={`/operations/incidents/new?tripId=${trip.id}`} />}><ShieldAlert /> Report incident</Button><Button variant="outline" render={<Link to="/operations/approvals" />}>Request exceptional approval</Button></div><div className="mt-4 border-t pt-4 text-sm"><p>{linkedIncidents.length} linked incidents</p><p className="mt-1 text-muted-foreground">{linkedApprovals.length} approval requests</p></div></article>{!["Completed", "Cancelled"].includes(trip.status) && <article className="rounded-lg border border-red-200 bg-card p-5"><h2 className="font-semibold">Cancel trip</h2><p className="mt-2 text-sm text-muted-foreground">Cancellation removes this departure from active dispatch while retaining its audit history.</p><Button className="mt-4" variant="destructive" onClick={() => setCancelOpen(true)}><Ban /> Cancel trip</Button></article>}</aside></section><AlertDialog open={cancelOpen} onOpenChange={setCancelOpen}><AlertDialogContent><AlertDialogHeader><AlertDialogTitle>Cancel {trip.id}?</AlertDialogTitle><AlertDialogDescription>This action keeps the trip in history but releases its vehicle, driver, and bay assignment.</AlertDialogDescription></AlertDialogHeader><Input placeholder="Required cancellation reason" value={reason} onChange={(event) => setReason(event.target.value)} /><AlertDialogFooter><AlertDialogCancel>Keep trip</AlertDialogCancel><AlertDialogAction variant="destructive" disabled={!reason.trim()} onClick={() => { cancelTrip(trip.id, reason); setCancelOpen(false); navigate("/operations/history"); }}>Confirm cancellation</AlertDialogAction></AlertDialogFooter></AlertDialogContent></AlertDialog></main>;
}

function Fact({ label, value }: { label: string; value: string }) { return <div><p className="text-xs text-muted-foreground">{label}</p><p className="mt-1 text-sm font-medium">{value}</p></div>; }
