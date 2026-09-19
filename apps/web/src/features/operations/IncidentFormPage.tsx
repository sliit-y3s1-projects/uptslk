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
  return (
    <main className="flex flex-1 flex-col gap-4 bg-muted/20 p-4">
      <PageHeading
        title="Report incident"
        description="Connect the issue to a trip so dispatch staff can respond with full context."
      />
      <form
        className="max-w-3xl rounded-lg border bg-card p-5"
        onSubmit={(event) => {
          event.preventDefault();
          const form = new FormData(event.currentTarget);
          createMutation.mutate(
            {
              centreId: user?.centreId ?? "",
              tripId: String(form.get("tripId")) || undefined,
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
        <div className="grid gap-4 sm:grid-cols-2">
          <label className="grid gap-1.5 text-sm font-medium">
            Related trip
            <Select
              name="tripId"
              defaultValue={params.get("tripId") ?? scopedTrips[0]?.id}
            >
              <SelectTrigger className="w-full bg-muted/60">
                <SelectValue placeholder="Centre-wide incident" />
              </SelectTrigger>
              <SelectContent>
                {scopedTrips.map((trip) => (
                  <SelectItem key={trip.id} value={trip.id}>
                    {trip.id} · Route {trip.routeNumber} ·{" "}
                    {new Date(trip.scheduledTime).toLocaleString()}
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
          <label className="grid gap-1.5 text-sm font-medium sm:col-span-2">
            Incident summary
            <Input
              name="title"
              placeholder="Describe the operational issue"
              required
            />
          </label>
          <label className="grid gap-1.5 text-sm font-medium sm:col-span-2">
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
