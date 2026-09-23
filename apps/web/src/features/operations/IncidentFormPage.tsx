import { useState } from "react";
import { useSearchParams, useNavigate } from "react-router";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { PageHeading } from "@/components/shared/PageHeading";
import { useAuth } from "@/hooks/useAuth";
import { useTrips } from "./hooks/useTrips";
import { useCreateIncident } from "./hooks/useIncidents";

export function IncidentFormPage() {
  const { user } = useAuth();
  const { data: trips = [] } = useTrips({ centreId: user?.centreId });
  const createMutation = useCreateIncident();
  const [params] = useSearchParams();
  const navigate = useNavigate();
  const scopedTrips = trips.filter((trip) => trip.status !== "Cancelled");
  const defaultTripId =
    params.get("tripId") ?? scopedTrips[0]?.id ?? "centre-wide";
  const [selectedTripId, setSelectedTripId] = useState(
    params.get("tripId") ?? "",
  );
  const effectiveTripId = selectedTripId || defaultTripId;
  const selectedTrip = scopedTrips.find((trip) => trip.id === effectiveTripId);
  const tripLabel = (trip: (typeof scopedTrips)[number]) => {
    const departure = new Date(trip.scheduledTime).toLocaleString([], {
      month: "short",
      day: "numeric",
      hour: "2-digit",
      minute: "2-digit",
    });
    const direction =
      trip.directionName ??
      `${trip.origin ?? "Origin"} → ${trip.destination ?? "Destination"}`;
    return `${departure} · ${direction} · ${trip.vehicle} · ${trip.bay}`;
  };
  return (
    <main className="flex flex-1 flex-col gap-4 bg-muted/20 p-4">
      <PageHeading
        title="Report incident"
        description="Connect the issue to a trip so dispatch staff can respond with full context."
      />
      <form
        className="w-full rounded-xl border border-slate-300 bg-card p-6"
        onSubmit={(event) => {
          event.preventDefault();
          const form = new FormData(event.currentTarget);
          createMutation.mutate(
            {
              centreId: user?.centreId ?? "",
              tripId:
                form.get("tripId") === "centre-wide"
                  ? undefined
                  : String(form.get("tripId") || ""),
              reportedByName: user?.name ?? "Dispatch operator",
              type:
                String(form.get("category")) === "Vehicle"
                  ? "Breakdown"
                  : (String(form.get("category")) as
                      "Delay" | "Safety" | "Other"),
              severity: String(form.get("severity")) as
                "Low" | "Medium" | "High",
              title: String(form.get("title")),
              description: String(form.get("description")),
              assignedTo: String(form.get("owner")),
            },
            {
              onSuccess: () => navigate("/operations/incidents"),
              onError: () => undefined,
            },
          );
        }}
      >
        <div className="space-y-6">
          <section className="rounded-xl border border-slate-300 p-5">
            <div className="mb-4">
              <h2 className="font-semibold">Trip context</h2>
              <p className="text-sm text-muted-foreground">
                Link this report to the affected departure, or keep it
                centre-wide.
              </p>
            </div>
            <div className="grid gap-4 md:grid-cols-2">
              <label className="grid gap-1.5 text-sm font-medium">
                Related trip
                <Select
                  name="tripId"
                  value={effectiveTripId}
                  onValueChange={(value) =>
                    setSelectedTripId(value ?? "centre-wide")
                  }
                  itemToStringLabel={(selected) => {
                    if (selected === "centre-wide")
                      return "Centre-wide incident";
                    const trip = scopedTrips.find(
                      (item) => item.id === selected,
                    );
                    return trip ? tripLabel(trip) : selected;
                  }}
                >
                  <SelectTrigger className="w-full bg-muted/60">
                    <SelectValue placeholder="Centre-wide incident" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="centre-wide">
                      Centre-wide incident
                    </SelectItem>
                    {scopedTrips.map((trip) => (
                      <SelectItem key={trip.id} value={trip.id}>
                        <span className="flex min-w-0 flex-col py-0.5">
                          <span className="font-medium">
                            {new Date(trip.scheduledTime).toLocaleString([], {
                              month: "short",
                              day: "numeric",
                              hour: "2-digit",
                              minute: "2-digit",
                            })}{" "}
                            ·{" "}
                            {trip.directionName ??
                              `${trip.origin ?? "Origin"} → ${trip.destination ?? "Destination"}`}
                          </span>
                          <span className="text-xs text-muted-foreground">
                            {trip.vehicle} · {trip.bay} · {trip.driver}
                          </span>
                        </span>
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </label>
              <label className="grid gap-1.5 text-sm font-medium">
                Category
                <Select name="category" defaultValue="Delay">
                  <SelectTrigger className="w-full bg-muted/60">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {["Delay", "Vehicle", "Safety", "Other"].map((item) => (
                      <SelectItem key={item} value={item}>
                        {item}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </label>
            </div>
            {selectedTrip ? (
              <div className="mt-4 flex flex-col gap-3 rounded-lg border border-emerald-300 bg-emerald-50 p-4 sm:flex-row sm:items-center sm:justify-between">
                <div className="min-w-0">
                  <p className="font-medium text-emerald-900">
                    {selectedTrip.routeNumber} ·{" "}
                    {selectedTrip.directionName ??
                      `${selectedTrip.origin ?? "Origin"} → ${selectedTrip.destination ?? "Destination"}`}
                  </p>
                  <p className="text-sm text-emerald-700">
                    Affected scheduled departure · {selectedTrip.vehicle} ·{" "}
                    {selectedTrip.bay}
                  </p>
                </div>
                <div className="text-sm font-medium text-emerald-800 sm:text-right">
                  {new Date(selectedTrip.scheduledTime).toLocaleString()}
                </div>
              </div>
            ) : (
              <div className="mt-4 rounded-lg border border-dashed border-slate-300 px-4 py-3 text-sm text-muted-foreground">
                This report applies to the centre, not a single departure.
              </div>
            )}
          </section>

          <section className="rounded-xl border border-slate-300 p-5">
            <div className="mb-4">
              <h2 className="font-semibold">Incident details</h2>
              <p className="text-sm text-muted-foreground">
                Record what happened and how urgently it needs attention.
              </p>
            </div>
            <div className="grid gap-4 md:grid-cols-2">
              <label className="grid gap-1.5 text-sm font-medium md:col-span-2">
                Incident summary
                <Input
                  name="title"
                  placeholder="Describe the operational issue"
                  required
                />
              </label>
              <label className="grid gap-1.5 text-sm font-medium md:col-span-2">
                Details
                <Input
                  name="description"
                  placeholder="Add operational context"
                  required
                />
              </label>
              <label className="grid gap-1.5 text-sm font-medium">
                Severity
                <Select name="severity" defaultValue="Medium">
                  <SelectTrigger className="w-full bg-muted/60">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {["Low", "Medium", "High"].map((item) => (
                      <SelectItem key={item} value={item}>
                        {item}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </label>
              <label className="grid gap-1.5 text-sm font-medium">
                Assigned owner
                <Input name="owner" placeholder="Dispatch desk" />
              </label>
            </div>
          </section>
        </div>
        <div className="mt-5 flex justify-end gap-2">
          <Button type="button" variant="outline" onClick={() => navigate(-1)}>
            Cancel
          </Button>
          <Button type="submit" disabled={createMutation.isPending}>
            {createMutation.isPending ? "Reporting..." : "Report incident"}
          </Button>
        </div>
      </form>
    </main>
  );
}
