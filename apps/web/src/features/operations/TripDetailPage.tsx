import { useState } from "react";
import {
  ArrowLeft,
  Ban,
  CheckCircle2,
  Pencil,
  ShieldAlert,
} from "lucide-react";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { PageHeading } from "@/components/shared/PageHeading";
import { StatusBadge } from "@/components/shared/StatusBadge";
import { useTrip, useUpdateTripStatus, useCancelTrip } from "./hooks/useTrips";
import { TripLifecycle } from "./components/TripLifecycle";
import { Link, useNavigate, useParams } from "react-router";

export function TripDetailPage() {
  const { tripId } = useParams();
  const navigate = useNavigate();
  const { data: trip, isLoading, error } = useTrip(tripId);
  const statusMutation = useUpdateTripStatus();
  const cancelMutation = useCancelTrip();
  const [cancelOpen, setCancelOpen] = useState(false);
  const [reason, setReason] = useState("");
  if (isLoading) return <main className="p-5">Loading trip...</main>;
  if (error || !trip) return <main className="p-5">Trip not found.</main>;
  const next =
    trip.status === "Scheduled"
      ? "Ready"
      : trip.status === "Ready" || trip.status === "Delayed"
        ? "Boarding"
        : trip.status === "Boarding"
          ? "Dispatched"
          : trip.status === "Dispatched"
            ? "Completed"
            : undefined;
  const change = () =>
    next && statusMutation.mutate({ id: trip.id, status: next });
  return (
    <main className="flex flex-1 flex-col gap-4 bg-muted/20 p-4">
      <PageHeading
        title={`Trip ${trip.route.routeNumber}`}
        description={`${trip.route.origin} to ${trip.route.destination} · ${new Date(trip.scheduledTime).toLocaleDateString()}`}
        action={
          <div className="flex flex-wrap gap-2">
            <Button
              variant="outline"
              render={<Link to="/operations/dispatch" />}
            >
              <ArrowLeft /> Dispatch board
            </Button>
            {!["Completed", "Cancelled"].includes(trip.status) && (
              <Button
                variant="outline"
                render={<Link to={`/operations/dispatch/${trip.id}/edit`} />}
              >
                <Pencil /> Edit
              </Button>
            )}
            {next && (
              <Button onClick={change} disabled={statusMutation.isPending}>
                <CheckCircle2 /> Mark {next.toLowerCase()}
              </Button>
            )}
          </div>
        }
      />
      <section className="grid gap-4 xl:grid-cols-[minmax(0,1.2fr)_360px]">
        <article className="rounded-lg border bg-card p-5">
          <div className="flex items-center justify-between">
            <h2 className="font-semibold">Trip lifecycle</h2>
            <StatusBadge
              label={trip.status}
              tone={
                trip.status === "Cancelled"
                  ? "danger"
                  : trip.status === "Delayed"
                    ? "warning"
                    : "good"
              }
            />
          </div>
          <TripLifecycle status={trip.status} />
          <div className="mt-7 grid gap-5 border-t pt-5 sm:grid-cols-2 lg:grid-cols-3">
            <Fact
              label="Date and time"
              value={new Date(trip.scheduledTime).toLocaleString()}
            />
            <Fact
              label="Assigned bay"
              value={trip.bayDetails?.code ?? "Unassigned"}
            />
            <Fact
              label="Vehicle"
              value={
                trip.vehicleDetails
                  ? `${trip.vehicleDetails.plateNumber} · ${trip.vehicleDetails.model}`
                  : "Unassigned"
              }
            />
            <Fact
              label="Driver"
              value={
                trip.driverDetails
                  ? `${trip.driverDetails.fullName} · ${trip.driverDetails.licenseNumber}`
                  : "Unassigned"
              }
            />
            <Fact
              label="Capacity"
              value={
                trip.vehicleDetails
                  ? `${trip.vehicleDetails.capacity} seats`
                  : "—"
              }
            />
            <Fact
              label="Last update"
              value={new Date(trip.updatedAt).toLocaleString()}
            />
          </div>
          <div className="mt-5 rounded-md bg-muted/50 p-4">
            <p className="text-xs text-muted-foreground">Operational notes</p>
            <p className="mt-1 text-sm">{trip.notes || "No notes recorded."}</p>
          </div>
        </article>
        <aside className="space-y-4">
          <article className="rounded-lg border bg-card p-5">
            <h2 className="font-semibold">Related workflows</h2>
            <div className="mt-4 grid gap-2">
              <Button
                variant="outline"
                render={
                  <Link to={`/operations/incidents/new?tripId=${trip.id}`} />
                }
              >
                <ShieldAlert /> Report incident
              </Button>
            </div>
          </article>
          {!["Completed", "Cancelled"].includes(trip.status) && (
            <article className="rounded-lg border border-red-200 bg-card p-5">
              <h2 className="font-semibold">Cancel trip</h2>
              <p className="mt-2 text-sm text-muted-foreground">
                Cancellation retains the trip in history.
              </p>
              <Button
                className="mt-4"
                variant="destructive"
                onClick={() => setCancelOpen(true)}
              >
                <Ban /> Cancel trip
              </Button>
            </article>
          )}
        </aside>
      </section>
      <AlertDialog open={cancelOpen} onOpenChange={setCancelOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Cancel {trip.id}?</AlertDialogTitle>
            <AlertDialogDescription>
              Provide a reason for the cancellation.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <Input
            placeholder="Required cancellation reason"
            value={reason}
            onChange={(event) => setReason(event.target.value)}
          />
          <AlertDialogFooter>
            <AlertDialogCancel>Keep trip</AlertDialogCancel>
            <AlertDialogAction
              variant="destructive"
              disabled={!reason.trim() || cancelMutation.isPending}
              onClick={() =>
                cancelMutation.mutate(
                  { id: trip.id, reason },
                  { onSuccess: () => navigate("/operations/history") },
                )
              }
            >
              Confirm cancellation
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </main>
  );
}
function Fact({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <p className="text-xs text-muted-foreground">{label}</p>
      <p className="mt-1 text-sm font-medium">{value}</p>
    </div>
  );
}
