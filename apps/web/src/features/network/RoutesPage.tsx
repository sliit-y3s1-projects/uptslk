import { useState } from "react";
import { MapPinned, Plus, Trash2, Loader2 } from "lucide-react";
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
import { useAuth } from "@/hooks/useAuth";
import {
  useRoutes,
  useRoute,
  useCreateRoute,
  useUpdateRoute,
  useArchiveRoute,
} from "./hooks/useRoutes";
import { useCentre } from "@/features/centres/hooks/useCentres";

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
              {route.origin} to {route.destination}
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
  const navigate = useNavigate();

  if (isLoading)
    return (
      <main className="flex flex-1 items-center justify-center p-4">
        <Loader2 className="animate-spin text-primary" />
      </main>
    );
  if (error || !route) return <main className="p-5">Route not found.</main>;

  const handleDeactivate = () => {
    archiveMutation.mutate(route.id, {
      onSuccess: () => {
        console.log("Success");
        navigate(basePath);
      },
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
              <Button
                variant="destructive"
                onClick={handleDeactivate}
                disabled={archiveMutation.isPending}
              >
                {archiveMutation.isPending ? (
                  <Loader2 className="animate-spin size-4" />
                ) : (
                  <>
                    <Trash2 /> Deactivate
                  </>
                )}
              </Button>
            </div>
          )
        }
      />
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
          <ol className="mt-6 space-y-1">
            {route.stops?.map((stop, index) => (
              <li key={stop.id || index} className="flex gap-3">
                <div className="flex w-6 flex-col items-center">
                  <span className="flex size-6 items-center justify-center rounded-full bg-primary text-xs font-medium text-primary-foreground">
                    {stop.sequenceOrder}
                  </span>
                  {index < route.stops.length - 1 && (
                    <span className="h-7 w-px bg-primary/30" />
                  )}
                </div>
                <div className="pb-3">
                  <p className="text-sm font-medium">{stop.stopName}</p>
                </div>
              </li>
            ))}
          </ol>
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
            <Button
              className="mt-6 w-full"
              render={<Link to={`/network/timetables?route=${route.id}`} />}
            >
              Manage schedules
            </Button>
          )}
        </article>
      </section>
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
  const [stops, setStops] = useState([
    { stopName: "", latitude: "", longitude: "" },
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
            >
              <SelectTrigger className="w-full bg-muted/60">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="0">Normal</SelectItem>
                <SelectItem value="1">Semi-Luxury</SelectItem>
                <SelectItem value="2">Luxury</SelectItem>
              </SelectContent>
            </Select>
          </label>
        </div>
        <div className="mt-5">
          <p className="text-sm font-medium">Ordered stops</p>
          <div className="mt-2 space-y-2">
            {stops.map((stop, index) => (
              <div key={index} className="flex items-center gap-2">
                <span className="flex size-8 items-center justify-center rounded border bg-muted/60 text-xs">
                  {index + 1}
                </span>
                <Input
                  value={stop.stopName}
                  onChange={(e) => {
                    const newStops = [...stops];
                    newStops[index].stopName = e.target.value;
                    setStops(newStops);
                  }}
                  placeholder="Stop name"
                  required
                />
                <Input
                  type="number"
                  step="any"
                  min="-90"
                  max="90"
                  value={stop.latitude}
                  onChange={(e) => {
                    const newStops = [...stops];
                    newStops[index].latitude = e.target.value;
                    setStops(newStops);
                  }}
                  placeholder="Lat"
                  required
                  className="w-24"
                />
                <Input
                  type="number"
                  step="any"
                  min="-180"
                  max="180"
                  value={stop.longitude}
                  onChange={(e) => {
                    const newStops = [...stops];
                    newStops[index].longitude = e.target.value;
                    setStops(newStops);
                  }}
                  placeholder="Lng"
                  required
                  className="w-24"
                />
                <Button
                  type="button"
                  size="icon"
                  variant="ghost"
                  onClick={() => {
                    const newStops = [...stops];
                    newStops.splice(index, 1);
                    setStops(newStops);
                  }}
                >
                  <Trash2 />
                </Button>
              </div>
            ))}
          </div>
          <Button
            type="button"
            variant="outline"
            className="mt-3"
            onClick={() =>
              setStops([
                ...stops,
                { stopName: "", latitude: "", longitude: "" },
              ])
            }
          >
            Add stop
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
            disabled={createMutation.isPending || updateMutation.isPending}
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
