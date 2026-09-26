import { useState } from "react";
import {
  ArrowRight,
  Loader2,
  MapPinned,
  Plus,
  Power,
  PowerOff,
  Trash2,
} from "lucide-react";
import { Link, useNavigate, useParams } from "react-router";
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
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { useAuth } from "@/hooks/useAuth";
import {
  useRoutes,
  useRoute,
  useCreateRoute,
  useUpdateRoute,
  useArchiveRoute,
  useReactivateRoute,
  useCreateDirection,
  useUpdateDirection,
} from "./hooks/useRoutes";
import { useCentre, useCentres } from "@/features/centres/hooks/useCentres";
import type { RouteDirection } from "./types";

export function RoutesPage({
  centreId,
  basePath = "/network/routes",
  readOnly = false,
}: { centreId?: string; basePath?: string; readOnly?: boolean } = {}) {
  const { user } = useAuth();
  const effectiveCentreId = centreId ?? user?.centreId;
  const { data: centre } = useCentre(effectiveCentreId);
  const { data: routes, isLoading, error } = useRoutes(effectiveCentreId);

  if (isLoading)
    return (
      <main className="flex flex-1 items-center justify-center p-4">
        <Loader2 className="animate-spin text-primary" />
      </main>
    );
  if (error)
    return <main className="p-4 text-red-500">Failed to load routes.</main>;

  return (
    <main className="flex flex-1 flex-col gap-4 bg-muted/20 p-4">
      <PageHeading
        title="Routes"
        description={`${readOnly ? "Read-only service corridors and performance" : "Plan service corridors and inspect performance"} for ${centre?.name ?? "this centre"}.`}
        action={
          readOnly ? (
            <Button
              variant="outline"
              render={<Link to={`/admin/centres/${effectiveCentreId}`} />}
            >
              Back to centre
            </Button>
          ) : (
            <Button render={<Link to="/network/routes/new" />}>
              <Plus /> Create route
            </Button>
          )
        }
      />
      <p className="text-sm text-muted-foreground">
        {routes?.length || 0} routes serving {centre?.name ?? "this centre"}
      </p>
      <section className="grid gap-4 lg:grid-cols-2 xl:grid-cols-3">
        {routes?.map((route) => (
          <Link
            key={route.id}
            to={`${basePath}/${route.id}`}
            className="rounded-lg border bg-card p-5"
          >
            <div className="flex items-start justify-between">
              <div className="flex size-10 items-center justify-center rounded-md bg-primary/10 text-primary">
                <MapPinned className="size-5" />
              </div>
              <StatusBadge
                label={route.isActive ? "Active" : "Archived"}
                tone={route.isActive ? "good" : "neutral"}
              />
            </div>
            <p className="mt-5 text-xs font-medium text-muted-foreground">
              ROUTE {route.routeNumber}
            </p>
            <h2 className="mt-1 font-semibold">{route.name}</h2>
            <p className="mt-1 text-sm text-muted-foreground">
              {route.directions?.length ?? 0} direction
              {route.directions?.length === 1 ? "" : "s"} · {route.origin} to{" "}
              {route.destination}
            </p>
          </Link>
        ))}
      </section>
    </main>
  );
}

export function RouteDetailPage({
  basePath = "/network/routes",
  readOnly = false,
}: { basePath?: string; readOnly?: boolean } = {}) {
  const { routeId } = useParams();
  const { data: route, isLoading, error } = useRoute(routeId);
  const archiveMutation = useArchiveRoute();
  const reactivateMutation = useReactivateRoute();
  const createDirection = useCreateDirection(routeId ?? "");
  const updateDirection = useUpdateDirection(routeId ?? "");
  const { data: centres = [] } = useCentres();
  const [addDirectionOpen, setAddDirectionOpen] = useState(false);
  const [editingDirection, setEditingDirection] =
    useState<RouteDirection | null>(null);
  const [deactivateOpen, setDeactivateOpen] = useState(false);

  if (isLoading)
    return (
      <main className="flex flex-1 items-center justify-center p-4">
        <Loader2 className="animate-spin text-primary" />
      </main>
    );
  if (error || !route) return <main className="p-5">Route not found.</main>;

  const handleDeactivate = () => {
    archiveMutation.mutate(route.id, {
      onSuccess: () => setDeactivateOpen(false),
    });
  };

  return (
    <main className="flex flex-1 flex-col gap-4 bg-muted/20 p-4">
      <PageHeading
        title={`Route ${route.routeNumber}`}
        description={route.name}
        action={
          readOnly ? (
            <Button variant="outline" render={<Link to={basePath} />}>
              Back to routes
            </Button>
          ) : (
            <div className="flex gap-2">
              <Button
                variant="outline"
                render={<Link to={`/network/routes/${route.id}/edit`} />}
              >
                Edit route
              </Button>
              {route.isActive ? (
                <Button
                  variant="destructive"
                  onClick={() => setDeactivateOpen(true)}
                  disabled={archiveMutation.isPending}
                >
                  <PowerOff /> Deactivate
                </Button>
              ) : (
                <Button
                  onClick={() => reactivateMutation.mutate(route.id)}
                  disabled={reactivateMutation.isPending}
                >
                  {reactivateMutation.isPending ? (
                    <Loader2 className="animate-spin size-4" />
                  ) : (
                    <>
                      <Power /> Reactivate
                    </>
                  )}
                </Button>
              )}
            </div>
          )
        }
      />
      <AlertDialog open={deactivateOpen} onOpenChange={setDeactivateOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Deactivate this route?</AlertDialogTitle>
            <AlertDialogDescription>
              Route {route.routeNumber} will no longer be available for active
              operations. Its configuration and history will be kept, and you
              can reactivate it later.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel disabled={archiveMutation.isPending}>
              Keep active
            </AlertDialogCancel>
            <AlertDialogAction
              variant="destructive"
              onClick={handleDeactivate}
              disabled={archiveMutation.isPending}
            >
              {archiveMutation.isPending ? (
                <Loader2 className="animate-spin size-4" />
              ) : (
                "Deactivate route"
              )}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
      <section className="grid gap-4 xl:grid-cols-[minmax(0,1.1fr)_340px]">
        <article className="rounded-lg border bg-card p-5">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm text-muted-foreground">Service pattern</p>
              <h2 className="mt-1 text-lg font-semibold">{route.name}</h2>
            </div>
            <StatusBadge
              label={route.isActive ? "Active" : "Archived"}
              tone={route.isActive ? "good" : "neutral"}
            />
          </div>
          <div className="mt-5 space-y-3">
            {route.directions.length === 0 ? (
              <p className="rounded-lg border border-dashed p-4 text-sm text-muted-foreground">
                Add the first direction before building its timetable.
              </p>
            ) : (
              route.directions.map((direction) => (
                <article
                  key={direction.id}
                  className="rounded-lg border border-slate-300 p-4"
                >
                  <div className="flex flex-wrap items-start justify-between gap-3">
                    <div>
                      <p className="text-xs font-medium text-muted-foreground">
                        DIRECTION
                      </p>
                      <h3 className="mt-1 font-semibold">
                        {direction.startCentre.name}{" "}
                        <ArrowRight className="mx-1 inline size-4 text-primary" />{" "}
                        {direction.endCentre.name}
                      </h3>
                      <p className="mt-1 text-sm text-muted-foreground">
                        {direction.estimatedDurationMin} min ·{" "}
                        {direction.distanceKm} km ·{" "}
                        {
                          direction.schedules.filter(
                            (schedule) => schedule.isActive,
                          ).length
                        }{" "}
                        active timetable
                        {direction.schedules.filter(
                          (schedule) => schedule.isActive,
                        ).length === 1
                          ? ""
                          : "s"}
                      </p>
                    </div>
                    <StatusBadge
                      label={direction.isActive ? "Active" : "Archived"}
                      tone={direction.isActive ? "good" : "neutral"}
                    />
                  </div>
                  <div className="mt-4 rounded-md border bg-muted/20 p-3">
                    <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
                      Stops in travel order
                    </p>
                    {direction.stops.length > 0 ? (
                      <ol className="mt-2 flex flex-wrap items-center gap-2">
                        {direction.stops.map((stop, index) => (
                          <li
                            key={stop.id}
                            className="flex items-center gap-2 text-sm"
                          >
                            <span className="flex size-5 items-center justify-center rounded-full bg-primary/10 text-xs font-semibold text-primary">
                              {index + 1}
                            </span>
                            <span>{stop.stopName}</span>
                            {index < direction.stops.length - 1 && (
                              <ArrowRight className="size-3.5 text-muted-foreground" />
                            )}
                          </li>
                        ))}
                      </ol>
                    ) : (
                      <p className="mt-2 text-sm text-muted-foreground">
                        No stops have been recorded for this direction.
                      </p>
                    )}
                  </div>
                  {!readOnly && (
                    <div className="mt-4 flex gap-2">
                      <Button
                        size="sm"
                        variant="outline"
                        onClick={() => setEditingDirection(direction)}
                      >
                        Edit direction
                      </Button>
                      <Button
                        size="sm"
                        variant="outline"
                        className="text-destructive"
                        onClick={() =>
                          updateDirection.mutate({
                            directionId: direction.id,
                            data: {
                              startCentreId: direction.startCentreId,
                              endCentreId: direction.endCentreId,
                              name: direction.name,
                              distanceKm: direction.distanceKm,
                              estimatedDurationMin:
                                direction.estimatedDurationMin,
                              stops: direction.stops.map((stop) => ({
                                stopName: stop.stopName,
                                latitude: stop.latitude ?? 0,
                                longitude: stop.longitude ?? 0,
                              })),
                              isActive: !direction.isActive,
                            },
                          })
                        }
                      >
                        {direction.isActive ? "Deactivate" : "Reactivate"}
                      </Button>
                    </div>
                  )}
                </article>
              ))
            )}
          </div>
        </article>
        <article className="rounded-lg border bg-card p-5">
          <h2 className="font-semibold">Route details</h2>
          <div className="mt-4 space-y-4">
            <Detail label="Distance" value={`${route.distanceKm} km`} />
            <Detail
              label="Estimated duration"
              value={`${route.estimatedDurationMin} min`}
            />
          </div>
          {!readOnly && (
            <div className="mt-6 grid gap-2">
              <Button
                render={<Link to={`/network/timetables?route=${route.id}`} />}
              >
                Manage timetables
              </Button>
              <Button
                variant="outline"
                onClick={() => setAddDirectionOpen(true)}
              >
                <Plus /> Add direction
              </Button>
            </div>
          )}
        </article>
      </section>
      <Dialog open={addDirectionOpen} onOpenChange={setAddDirectionOpen}>
        <DialogContent className="max-h-[calc(100dvh-2rem)] overflow-y-auto sm:max-w-4xl">
          <DialogHeader className="mb-4">
            <DialogTitle>Add route direction</DialogTitle>
            <DialogDescription>
              Directions represent the actual journey passengers take. The
              departure bay and timetable will belong to the direction’s start
              centre.
            </DialogDescription>
          </DialogHeader>
          <DirectionForm
            centres={centres}
            pending={createDirection.isPending}
            onCancel={() => setAddDirectionOpen(false)}
            onSubmit={(data) =>
              createDirection.mutate(data, {
                onSuccess: () => setAddDirectionOpen(false),
              })
            }
          />
        </DialogContent>
      </Dialog>
      <Dialog
        open={editingDirection !== null}
        onOpenChange={(open) => !open && setEditingDirection(null)}
      >
        <DialogContent className="max-h-[calc(100dvh-2rem)] overflow-y-auto sm:max-w-4xl">
          <DialogHeader className="mb-4">
            <DialogTitle>Edit route direction</DialogTitle>
            <DialogDescription>
              Update the terminal pair and travel time. Existing timetables
              remain associated with this direction.
            </DialogDescription>
          </DialogHeader>
          {editingDirection && (
            <DirectionForm
              direction={editingDirection}
              centres={centres}
              pending={updateDirection.isPending}
              onCancel={() => setEditingDirection(null)}
              onSubmit={(data) =>
                updateDirection.mutate(
                  {
                    directionId: editingDirection.id,
                    data: { ...data, isActive: editingDirection.isActive },
                  },
                  { onSuccess: () => setEditingDirection(null) },
                )
              }
            />
          )}
        </DialogContent>
      </Dialog>
    </main>
  );
}

export function RouteFormPage() {
  const { routeId } = useParams();
  const navigate = useNavigate();
  const { user } = useAuth();
  const { data: existing, isLoading, error: existingError } = useRoute(routeId);
  const createMutation = useCreateRoute();
  const updateMutation = useUpdateRoute(routeId!);
  const { data: centres = [] } = useCentres();
  const [startCentreId, setStartCentreId] = useState("");
  const [endCentreId, setEndCentreId] = useState("");
  const [stops, setStops] = useState([
    { stopName: "", latitude: "0", longitude: "0" },
    { stopName: "", latitude: "0", longitude: "0" },
  ]);
  const [initialized, setInitialized] = useState(false);

  // Set initial stops when editing
  if (existing && existing.stops?.length > 0 && !initialized) {
    setStops(
      existing.stops.map((s) => ({
        stopName: s.stopName,
        latitude: String(s.latitude ?? ""),
        longitude: String(s.longitude ?? ""),
      })),
    );
    setInitialized(true);
  }

  const editing = Boolean(routeId);

  function setRouteTerminal(terminal: "start" | "end", centreId: string) {
    const centreName =
      centres.find((centre) => centre.id === centreId)?.name ?? "";
    setStops((current) =>
      current.map((stop, index) =>
        index === (terminal === "start" ? 0 : current.length - 1)
          ? { ...stop, stopName: centreName }
          : stop,
      ),
    );
  }

  if (editing && isLoading)
    return (
      <main className="flex flex-1 items-center justify-center p-4">
        <Loader2 className="animate-spin text-primary" />
      </main>
    );
  if (editing && existingError)
    return (
      <main className="p-4 text-red-500">
        Failed to load route for editing.
      </main>
    );
  if (editing && !existing)
    return <main className="p-4">Route not found.</main>;

  return (
    <main className="flex flex-1 flex-col gap-4 bg-muted/20 p-4">
      <PageHeading
        title={editing ? `Edit route ${existing?.routeNumber}` : "Create route"}
        description="Set a clear service corridor before scheduling vehicles and drivers."
      />
      <form
        onSubmit={(event) => {
          event.preventDefault();
          const form = new FormData(event.currentTarget);
          const payload = {
            centreId: user?.centreId || "",
            routeNumber: String(form.get("routeNumber")),
            name: String(form.get("name")),
            origin: String(form.get("origin")),
            destination: String(form.get("destination")),
            serviceType: Number(form.get("serviceType") || 0),
            distanceKm: Number(form.get("distanceKm")),
            estimatedDurationMin: Number(form.get("estimatedDurationMin")),
            isActive: editing ? (existing?.isActive ?? true) : true,
            stops: stops.map((s, i) => ({
              stopName: s.stopName,
              latitude: Number(s.latitude),
              longitude: Number(s.longitude),
              sequenceOrder: i + 1,
            })),
            ...(!editing && { startCentreId, endCentreId }),
          };

          if (editing) {
            updateMutation.mutate(payload, {
              onSuccess: () => {
                console.log("Success");
                navigate(`/network/routes/${routeId}`);
              },
              onError: () => console.error("Error"),
            });
          } else {
            createMutation.mutate(payload, {
              onSuccess: (data) => {
                console.log("Success");
                navigate(`/network/routes/${data.id}`);
              },
              onError: () => console.error("Error"),
            });
          }
        }}
        className="max-w-3xl rounded-lg border bg-card p-5"
      >
        <div className="grid gap-4 sm:grid-cols-2">
          <Field
            name="routeNumber"
            label="Route number"
            value={existing?.routeNumber}
            placeholder="138"
            disabled={editing}
          />
          <Field
            name="name"
            label="Route Name"
            value={existing?.name}
            placeholder="Maharagama - Pettah"
          />
          {editing ? (
            <>
              <Field
                name="origin"
                label="Origin"
                value={existing?.origin}
                placeholder="Maharagama"
              />
              <Field
                name="destination"
                label="Destination"
                value={existing?.destination}
                placeholder="Pettah"
              />
            </>
          ) : (
            <>
              <label className="grid gap-1.5 text-sm font-medium">
                Outbound departure centre
                <Select
                  value={startCentreId || null}
                  onValueChange={(value) => {
                    const nextValue = value ?? "";
                    setStartCentreId(nextValue);
                    setRouteTerminal("start", nextValue);
                  }}
                  itemToStringLabel={(value) => {
                    const centre = centres.find((item) => item.id === value);
                    return centre ? `${centre.name} (${centre.code})` : value;
                  }}
                >
                  <SelectTrigger className="w-full bg-muted/60">
                    <SelectValue placeholder="Choose departure centre" />
                  </SelectTrigger>
                  <SelectContent>
                    {centres
                      .filter((centre) => centre.status === "Operating")
                      .map((centre) => (
                        <SelectItem key={centre.id} value={centre.id}>
                          {centre.name} ({centre.code})
                        </SelectItem>
                      ))}
                  </SelectContent>
                </Select>
              </label>
              <label className="grid gap-1.5 text-sm font-medium">
                Outbound arrival centre
                <Select
                  value={endCentreId || null}
                  onValueChange={(value) => {
                    const nextValue = value ?? "";
                    setEndCentreId(nextValue);
                    setRouteTerminal("end", nextValue);
                  }}
                  itemToStringLabel={(value) => {
                    const centre = centres.find((item) => item.id === value);
                    return centre ? `${centre.name} (${centre.code})` : value;
                  }}
                >
                  <SelectTrigger className="w-full bg-muted/60">
                    <SelectValue placeholder="Choose arrival centre" />
                  </SelectTrigger>
                  <SelectContent>
                    {centres
                      .filter(
                        (centre) =>
                          centre.status === "Operating" &&
                          centre.id !== startCentreId,
                      )
                      .map((centre) => (
                        <SelectItem key={centre.id} value={centre.id}>
                          {centre.name} ({centre.code})
                        </SelectItem>
                      ))}
                  </SelectContent>
                </Select>
              </label>
              <input
                type="hidden"
                name="origin"
                value={
                  centres.find((centre) => centre.id === startCentreId)?.name ??
                  "Pending selection"
                }
              />
              <input
                type="hidden"
                name="destination"
                value={
                  centres.find((centre) => centre.id === endCentreId)?.name ??
                  "Pending selection"
                }
              />
            </>
          )}
          <Field
            name="distanceKm"
            label="Distance (km)"
            value={String(existing?.distanceKm ?? "")}
            type="number"
            min="0.1"
            step="0.1"
            placeholder="15"
          />
          <Field
            name="estimatedDurationMin"
            label="Estimated duration (min)"
            value={String(existing?.estimatedDurationMin ?? "")}
            type="number"
            min="1"
            placeholder="45"
          />
          <label className="grid gap-1.5 text-sm font-medium">
            Service Type
            <Select
              name="serviceType"
              defaultValue={String(existing?.serviceType ?? "0")}
              itemToStringLabel={(value) =>
                ({ "0": "Normal", "1": "Semi-luxury", "2": "AC express" })[
                  value
                ] ?? value
              }
            >
              <SelectTrigger className="w-full bg-muted/60">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="0">Normal</SelectItem>
                <SelectItem value="1">Semi-Luxury</SelectItem>
                <SelectItem value="2">AC express</SelectItem>
              </SelectContent>
            </Select>
          </label>
        </div>
        <div className="mt-5 rounded-lg border border-primary/20 bg-primary/5 p-4">
          <p className="text-sm font-medium">
            {editing ? "Ordered stops" : "Outbound stops"}
          </p>
          {!editing && (
            <p className="mt-1 text-sm text-muted-foreground">
              The first and last stops are your selected terminals. Add the
              intermediate stops in travel order. You will add the return
              journey after this route is saved.
            </p>
          )}
          <div className="mt-2 space-y-2">
            {stops.map((stop, index) => {
              const isTerminal =
                !editing && (index === 0 || index === stops.length - 1);
              return (
                <div
                  key={`${index}-${stop.stopName}`}
                  className="flex items-center gap-2"
                >
                  <span className="flex size-8 shrink-0 items-center justify-center rounded-full bg-primary text-xs font-semibold text-primary-foreground">
                    {index + 1}
                  </span>
                  <Input
                    value={stop.stopName}
                    onChange={(e) => {
                      const newStops = [...stops];
                      newStops[index].stopName = e.target.value;
                      setStops(newStops);
                    }}
                    readOnly={isTerminal}
                    className={
                      isTerminal
                        ? "border-emerald-200 bg-emerald-50 font-medium text-emerald-900 focus-visible:border-emerald-400 focus-visible:ring-emerald-200"
                        : undefined
                    }
                    placeholder={
                      isTerminal
                        ? "Select a terminal centre above"
                        : "Intermediate stop name"
                    }
                    required
                  />
                  {isTerminal ? (
                    <span className="w-20 shrink-0 rounded-full bg-emerald-100 px-2 py-1 text-center text-xs font-semibold text-emerald-800">
                      Terminal
                    </span>
                  ) : (
                    <Button
                      type="button"
                      size="icon"
                      variant="ghost"
                      className="shrink-0 text-destructive"
                      onClick={() =>
                        setStops((current) =>
                          current.filter((_, stopIndex) => stopIndex !== index),
                        )
                      }
                    >
                      <Trash2 />
                    </Button>
                  )}
                </div>
              );
            })}
          </div>
          <Button
            type="button"
            variant="outline"
            className="mt-3"
            onClick={() =>
              setStops((current) =>
                !editing
                  ? [
                      ...current.slice(0, -1),
                      { stopName: "", latitude: "0", longitude: "0" },
                      current[current.length - 1],
                    ]
                  : [
                      ...current,
                      { stopName: "", latitude: "0", longitude: "0" },
                    ],
              )
            }
          >
            <Plus /> Add {editing ? "stop" : "intermediate stop"}
          </Button>
        </div>
        <div className="mt-5 flex justify-end gap-2">
          {(createMutation.isError || updateMutation.isError) && (
            <span className="text-sm text-red-500 self-center mr-auto">
              Failed to save route. Please try again.
            </span>
          )}
          <Button type="button" variant="outline" onClick={() => navigate(-1)}>
            Cancel
          </Button>
          <Button
            type="submit"
            disabled={
              createMutation.isPending ||
              updateMutation.isPending ||
              (!editing && (!startCentreId || !endCentreId))
            }
          >
            {createMutation.isPending || updateMutation.isPending ? (
              <Loader2 className="animate-spin size-4" />
            ) : editing ? (
              "Save route"
            ) : (
              "Create route"
            )}
          </Button>
        </div>
      </form>
    </main>
  );
}

function DirectionForm({
  direction,
  centres,
  pending,
  onCancel,
  onSubmit,
}: {
  direction?: RouteDirection;
  centres: { id: string; name: string; code: string; status: string }[];
  pending: boolean;
  onCancel: () => void;
  onSubmit: (data: {
    startCentreId: string;
    endCentreId: string;
    name: string;
    distanceKm: number;
    estimatedDurationMin: number;
    stops: { stopName: string; latitude: number; longitude: number }[];
  }) => void;
}) {
  const [startCentreId, setStartCentreId] = useState(
    direction?.startCentreId ?? "",
  );
  const [endCentreId, setEndCentreId] = useState(direction?.endCentreId ?? "");
  const [stops, setStops] = useState(() =>
    direction?.stops.length
      ? direction.stops.map((stop) => ({
          stopName: stop.stopName,
          latitude: stop.latitude ?? 0,
          longitude: stop.longitude ?? 0,
        }))
      : [
          { stopName: "", latitude: 0, longitude: 0 },
          { stopName: "", latitude: 0, longitude: 0 },
        ],
  );
  const centreLabel = (value: string) => {
    const centre = centres.find((item) => item.id === value);
    return centre ? `${centre.name} (${centre.code})` : value;
  };
  function setTerminal(terminal: "start" | "end", centreId: string) {
    const centreName =
      centres.find((centre) => centre.id === centreId)?.name ?? "";
    setStops((current) =>
      current.map((stop, index) =>
        index === (terminal === "start" ? 0 : current.length - 1)
          ? { ...stop, stopName: centreName }
          : stop,
      ),
    );
  }
  function updateIntermediateStop(index: number, stopName: string) {
    setStops((current) =>
      current.map((stop, stopIndex) =>
        stopIndex === index ? { ...stop, stopName } : stop,
      ),
    );
  }
  return (
    <form
      className="grid gap-4"
      onSubmit={(event) => {
        event.preventDefault();
        const form = new FormData(event.currentTarget);
        onSubmit({
          startCentreId,
          endCentreId,
          name: String(form.get("name")),
          distanceKm: Number(form.get("distanceKm")),
          estimatedDurationMin: Number(form.get("estimatedDurationMin")),
          stops,
        });
      }}
    >
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        <label className="grid gap-1.5 text-sm font-medium">
          Departure centre
          <Select
            value={startCentreId || null}
            onValueChange={(value) => {
              const nextValue = value ?? "";
              setStartCentreId(nextValue);
              setTerminal("start", nextValue);
            }}
            itemToStringLabel={centreLabel}
          >
            <SelectTrigger>
              <SelectValue placeholder="Choose departure centre" />
            </SelectTrigger>
            <SelectContent>
              {centres
                .filter((centre) => centre.status === "Operating")
                .map((centre) => (
                  <SelectItem key={centre.id} value={centre.id}>
                    {centre.name} ({centre.code})
                  </SelectItem>
                ))}
            </SelectContent>
          </Select>
        </label>
        <label className="grid gap-1.5 text-sm font-medium">
          Arrival centre
          <Select
            value={endCentreId || null}
            onValueChange={(value) => {
              const nextValue = value ?? "";
              setEndCentreId(nextValue);
              setTerminal("end", nextValue);
            }}
            itemToStringLabel={centreLabel}
          >
            <SelectTrigger>
              <SelectValue placeholder="Choose arrival centre" />
            </SelectTrigger>
            <SelectContent>
              {centres
                .filter(
                  (centre) =>
                    centre.status === "Operating" &&
                    centre.id !== startCentreId,
                )
                .map((centre) => (
                  <SelectItem key={centre.id} value={centre.id}>
                    {centre.name} ({centre.code})
                  </SelectItem>
                ))}
            </SelectContent>
          </Select>
        </label>
        <Field
          name="name"
          label="Direction name"
          value={direction?.name}
          placeholder="Kadawatha Centre to Makumbura Centre"
        />
        <Field
          name="distanceKm"
          label="Distance (km)"
          value={direction ? String(direction.distanceKm) : undefined}
          type="number"
          min="0.1"
          step="0.1"
          placeholder="32"
        />
        <Field
          name="estimatedDurationMin"
          label="Estimated duration (min)"
          value={direction ? String(direction.estimatedDurationMin) : undefined}
          type="number"
          min="1"
          placeholder="45"
        />
      </div>
      <section className="rounded-xl border border-slate-300 bg-slate-50 p-4">
        <div className="flex flex-wrap items-start justify-between gap-2">
          <div>
            <h3 className="font-medium">Ordered stops</h3>
            <p className="mt-1 text-sm text-muted-foreground">
              The first and last stops follow the selected terminal centres. Add
              the intermediate stops in travel order.
            </p>
          </div>
          <span className="rounded-full border border-slate-300 bg-background px-2.5 py-1 text-xs font-medium text-muted-foreground">
            {stops.length} stops
          </span>
        </div>
        <div className="mt-4 space-y-2">
          {stops.map((stop, index) => {
            const isTerminal = index === 0 || index === stops.length - 1;
            return (
              <div
                key={`${index}-${stop.stopName}`}
                className="flex items-center gap-2"
              >
                <span className="flex size-8 shrink-0 items-center justify-center rounded-full bg-primary text-xs font-semibold text-primary-foreground">
                  {index + 1}
                </span>
                <Input
                  value={stop.stopName}
                  onChange={(event) =>
                    updateIntermediateStop(index, event.target.value)
                  }
                  readOnly={isTerminal}
                  className={
                    isTerminal
                      ? "border-slate-400 bg-slate-100 font-medium text-slate-900 focus-visible:border-primary"
                      : "border-slate-400 bg-white focus-visible:border-primary"
                  }
                  placeholder={
                    isTerminal
                      ? "Select a terminal centre above"
                      : "Intermediate stop name"
                  }
                  required
                />
                {isTerminal ? (
                  <span className="w-20 shrink-0 rounded-full border border-slate-300 bg-slate-100 px-2 py-1 text-center text-xs font-semibold text-slate-700">
                    Terminal
                  </span>
                ) : (
                  <Button
                    type="button"
                    size="icon"
                    variant="ghost"
                    className="shrink-0 text-destructive"
                    onClick={() =>
                      setStops((current) =>
                        current.filter((_, stopIndex) => stopIndex !== index),
                      )
                    }
                    aria-label={`Remove stop ${index + 1}`}
                  >
                    <Trash2 />
                  </Button>
                )}
              </div>
            );
          })}
        </div>
        <Button
          type="button"
          variant="outline"
          className="mt-3"
          onClick={() =>
            setStops((current) => [
              ...current.slice(0, -1),
              { stopName: "", latitude: 0, longitude: 0 },
              current[current.length - 1],
            ])
          }
        >
          <Plus /> Add intermediate stop
        </Button>
      </section>
      <div className="flex justify-end gap-2">
        <Button type="button" variant="outline" onClick={onCancel}>
          Cancel
        </Button>
        <Button
          type="submit"
          disabled={
            pending ||
            !startCentreId ||
            !endCentreId ||
            stops.some((stop) => !stop.stopName.trim())
          }
        >
          {pending
            ? "Saving..."
            : direction
              ? "Save direction"
              : "Add direction"}
        </Button>
      </div>
    </form>
  );
}

function Field({
  label,
  value,
  ...props
}: {
  label: string;
  name: string;
  value?: string;
  placeholder?: string;
  type?: string;
  disabled?: boolean;
  min?: string;
  max?: string;
  step?: string;
}) {
  return (
    <label className="grid gap-1.5 text-sm font-medium">
      {label}
      <Input defaultValue={value} {...props} required />
    </label>
  );
}
function Detail({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex items-center justify-between border-b pb-3 text-sm">
      <span className="text-muted-foreground">{label}</span>
      <span className="font-medium">{value}</span>
    </div>
  );
}
