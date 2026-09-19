import { useState } from "react";
import { CalendarClock, Plus, Loader2, Trash2 } from "lucide-react";
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
import { StatusBadge } from "@/components/shared/StatusBadge";
import { useAuth } from "@/hooks/useAuth";
import {
  useRoutes,
  useSchedules,
  useCreateSchedule,
  useDeactivateSchedule,
} from "./hooks/useRoutes";
import { useBays } from "@/features/centres/hooks/useCentres";

import { useSearchParams } from "react-router";

export function TimetablesPage() {
  const { user } = useAuth();
  const [searchParams, setSearchParams] = useSearchParams();
  const routeIdParam = searchParams.get("route");

  const selectedRouteId = routeIdParam || undefined;
  const [showForm, setShowForm] = useState(false);

  const {
    data: routes,
    isLoading: loadingRoutes,
    error: routesError,
  } = useRoutes(user?.centreId);
  const selectedRoute = routes?.find((r) => r.id === selectedRouteId);
  const { data: bays, error: baysError } = useBays(
    selectedRoute?.centreId ?? user?.centreId,
  );
  const {
    data: schedules,
    isLoading: loadingSchedules,
    error: schedulesError,
  } = useSchedules(selectedRouteId);

  const createMutation = useCreateSchedule(selectedRouteId || "");
  const deactivateMutation = useDeactivateSchedule(selectedRouteId || "");

  const handleRouteSelect = (val: string | null) => {
    if (!val) return;
    {
      setSearchParams({ route: val });
    }
  };

  return (
    <main className="flex flex-1 flex-col gap-4 bg-muted/20 p-4">
      <PageHeading
        title="Timetables & Schedules"
        description="Manage recurring departures, headways, and assigned bays for your routes."
        action={
          <Button
            onClick={() => setShowForm((value) => !value)}
            disabled={!selectedRouteId}
          >
            <Plus /> Add schedule
          </Button>
        }
      />

      <div className="flex items-center gap-4 bg-card p-4 rounded-lg border">
        <span className="text-sm font-medium">Select Route:</span>
        <div className="w-64">
          <Select value={selectedRouteId} onValueChange={handleRouteSelect}>
            <SelectTrigger>
              <SelectValue
                placeholder={loadingRoutes ? "Loading..." : "Choose a route"}
              />
            </SelectTrigger>
            <SelectContent>
              {routes?.map((r) => (
                <SelectItem key={r.id} value={r.id}>
                  {r.routeNumber} - {r.name}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      </div>

      {showForm && selectedRouteId && (
        <form
          className="rounded-lg border bg-card p-5"
          onSubmit={(event) => {
            event.preventDefault();
            const form = new FormData(event.currentTarget);
            const payload = {
              bayId: String(form.get("bayId")),
              firstDeparture: String(form.get("firstDeparture")) + ":00",
              lastDeparture: String(form.get("lastDeparture")) + ":00",
              headwayMinutes: Number(form.get("headwayMinutes")),
              operatingDays: String(form.get("operatingDays")),
            };

            createMutation.mutate(payload, {
              onSuccess: () => {
                console.log("Success");
                setShowForm(false);
              },
              onError: () => console.error("Error"),
            });
          }}
        >
          <h2 className="font-semibold">New Schedule Pattern</h2>
          <div className="mt-4 grid gap-3 md:grid-cols-2 xl:grid-cols-5">
            <label className="grid gap-1.5 text-sm font-medium">
              Assigned Bay
              <Select name="bayId" required>
                <SelectTrigger>
                  <SelectValue placeholder="Select bay" />
                </SelectTrigger>
                <SelectContent>
                  {bays
                    ?.filter((b) => b.status === "Available")
                    .map((b) => (
                      <SelectItem key={b.id} value={b.id}>
                        {b.code}
                      </SelectItem>
                    ))}
                </SelectContent>
              </Select>
            </label>
            <label className="grid gap-1.5 text-sm font-medium">
              First Departure
              <Input name="firstDeparture" type="time" required />
            </label>
            <label className="grid gap-1.5 text-sm font-medium">
              Last Departure
              <Input name="lastDeparture" type="time" required />
            </label>
            <label className="grid gap-1.5 text-sm font-medium">
              Headway (min)
              <Input
                name="headwayMinutes"
                type="number"
                min="1"
                placeholder="15"
                required
              />
            </label>
            <label className="grid gap-1.5 text-sm font-medium">
              Operating Days
              <Select name="operatingDays" defaultValue="Everyday">
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="Everyday">Everyday</SelectItem>
                  <SelectItem value="Weekdays">Weekdays</SelectItem>
                  <SelectItem value="Weekends">Weekends</SelectItem>
                </SelectContent>
              </Select>
            </label>
          </div>
          <div className="mt-4 flex justify-end gap-2">
            <Button
              type="button"
              variant="outline"
              onClick={() => setShowForm(false)}
            >
              Cancel
            </Button>
            <Button type="submit" disabled={createMutation.isPending}>
              {createMutation.isPending ? "Saving..." : "Save schedule"}
            </Button>
          </div>
        </form>
      )}

      {!selectedRouteId ? (
        <div className="rounded-lg border border-dashed bg-card p-10 text-center text-sm text-muted-foreground">
          Select a route above to view and manage its schedules.
        </div>
      ) : routesError || baysError || schedulesError ? (
        <div className="rounded-lg border border-red-200 bg-red-50 p-10 text-center text-sm text-red-600">
          Failed to load schedule data. Please check your connection.
        </div>
      ) : loadingSchedules ? (
        <div className="flex justify-center p-10">
          <Loader2 className="animate-spin text-primary" />
        </div>
      ) : schedules?.length === 0 ? (
        <div className="rounded-lg border border-dashed bg-card p-10 text-center text-sm text-muted-foreground">
          No schedules exist for this route.
        </div>
      ) : (
        <div className="grid gap-4">
          {schedules?.map((schedule) => (
            <div
              key={schedule.id}
              className="flex items-center justify-between rounded-lg border bg-card p-4"
            >
              <div>
                <div className="flex items-center gap-2">
                  <CalendarClock className="size-4 text-primary" />
                  <h3 className="font-semibold">
                    {schedule.operatingDays} Schedule
                  </h3>
                  <StatusBadge
                    label={schedule.isActive ? "Active" : "Archived"}
                    tone={schedule.isActive ? "good" : "neutral"}
                  />
                </div>
                <p className="mt-1 text-sm text-muted-foreground">
                  First: {schedule.firstDeparture} | Last:{" "}
                  {schedule.lastDeparture} | Headway: {schedule.headwayMinutes}{" "}
                  mins | Bay: {schedule.bayCode}
                </p>
              </div>
              {schedule.isActive && (
                <Button
                  variant="ghost"
                  size="sm"
                  className="text-red-500 hover:text-red-600 hover:bg-red-50 disabled:opacity-50"
                  disabled={deactivateMutation.isPending}
                  onClick={() => {
                    deactivateMutation.mutate(schedule.id, {
                      onSuccess: () => console.log("Success"),
                    });
                  }}
                >
                  {deactivateMutation.isPending ? (
                    <Loader2 className="size-4 mr-2 animate-spin" />
                  ) : (
                    <Trash2 className="size-4 mr-2" />
                  )}{" "}
                  Deactivate
                </Button>
              )}
            </div>
          ))}
        </div>
      )}
    </main>
  );
}
