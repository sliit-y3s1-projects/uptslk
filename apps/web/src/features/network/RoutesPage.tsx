import { MapPinned, Plus, Trash2 } from "lucide-react";
import { Link, useNavigate, useParams } from "react-router";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { PageHeading } from "@/components/shared/PageHeading";
import { StatusBadge } from "@/components/shared/StatusBadge";
import { collections } from "@/mock/mock-data";
import { useMockData } from "@/context/MockDataContext";
import { useAuth } from "@/hooks/useAuth";
import { mockRowCentreId } from "@/mock/centre-scope";
import { centres } from "@/mock/centres";

const defaultRouteStops = ["Makumbura MMC", "Maharagama", "Nugegoda Junction", "Kirulapone", "Bambalapitiya", "Pettah Central"];
const routeStops: Record<string, string[]> = {
  "177": ["Kadawatha MMC", "Gonahena Junction", "Biyagama", "Malwana", "Kaduwela"],
  EX04: ["Kadawatha MMC", "Kiribathgoda", "Warakapola", "Kegalle", "Peradeniya", "Kandy"],
  "234": ["Kadawatha MMC", "Kiribathgoda", "Kelaniya", "Dematagoda", "Colombo Fort"],
  EX01: ["Makumbura MMC", "Gelanigama", "Welipenna", "Pinnaduwa", "Galle"],
  EX02: ["Makumbura MMC", "Gelanigama", "Welipenna", "Pinnaduwa", "Matara"],
};

export function RoutesPage({ centreId, basePath = "/network/routes", readOnly = false }: { centreId?: string; basePath?: string; readOnly?: boolean } = {}) {
  const { data } = useMockData();
  const { user } = useAuth();
  const effectiveCentreId = centreId ?? user?.centreId;
  const centre = centres.find((item) => item.id === effectiveCentreId);
  const routes = data.routes ?? collections.routes;
  const visibleRoutes = routes.filter((route, index) => !effectiveCentreId || mockRowCentreId(route, index) === effectiveCentreId);
  return <main className="flex flex-1 flex-col gap-4 bg-muted/20 p-4"><PageHeading title="Routes" description={`${readOnly ? "Read-only service corridors and performance" : "Plan service corridors and inspect performance"} for ${centre?.name ?? "this centre"}.`} action={readOnly ? <Button variant="outline" render={<Link to={`/admin/centres/${effectiveCentreId}`} />}>Back to centre</Button> : <Button render={<Link to="/network/routes/new" />}><Plus /> Create route</Button>} /><p className="text-sm text-muted-foreground">{visibleRoutes.length} routes serving {centre?.name ?? "this centre"}</p><section className="grid gap-4 lg:grid-cols-2 xl:grid-cols-3">{visibleRoutes.map((route) => <Link key={route.id} to={`${basePath}/${route.id}`} className="rounded-lg border bg-card p-5"><div className="flex items-start justify-between"><div className="flex size-10 items-center justify-center rounded-md bg-primary/10 text-primary"><MapPinned className="size-5" /></div><StatusBadge label={route.status} tone={route.tone} /></div><p className="mt-5 text-xs font-medium text-muted-foreground">ROUTE {route.id}</p><h2 className="mt-1 font-semibold">{route.title}</h2><p className="mt-1 text-sm text-muted-foreground">{route.subtitle}</p><div className="mt-5 grid grid-cols-2 gap-3 border-t pt-4 text-sm"><div><p className="text-xs text-muted-foreground">Reliability</p><p className="mt-1 font-medium">{route.updated}</p></div><div><p className="text-xs text-muted-foreground">Service</p><p className="mt-1 font-medium">{route.meta}</p></div></div></Link>)}</section></main>;
}

export function RouteDetailPage({ centreId, basePath = "/network/routes", readOnly = false }: { centreId?: string; basePath?: string; readOnly?: boolean } = {}) {
  const { data, deactivateRecord } = useMockData();
  const { user } = useAuth();
  const { routeId } = useParams();
  const routes = data.routes ?? collections.routes;
  const effectiveCentreId = centreId ?? user?.centreId;
  const scopedRoutes = routes.filter((item, index) => !effectiveCentreId || mockRowCentreId(item, index) === effectiveCentreId);
  const route = scopedRoutes.find((item) => item.id === routeId) ?? scopedRoutes[0];
  if (!route) return <main className="p-5">Route not found for this centre.</main>;
  const stops = routeStops[route.id] ?? defaultRouteStops;
  return <main className="flex flex-1 flex-col gap-4 bg-muted/20 p-4"><PageHeading title={`Route ${route.id}`} description={route.title} action={readOnly ? <Button variant="outline" render={<Link to={basePath} />}>Back to routes</Button> : <div className="flex gap-2"><Button variant="outline" render={<Link to={`/network/routes/${route.id}/edit`} />}>Edit route</Button><Button variant="destructive" onClick={() => deactivateRecord("routes", route.id)}><Trash2 /> Deactivate</Button></div>} /><section className="grid gap-4 xl:grid-cols-[minmax(0,1.1fr)_340px]"><article className="rounded-lg border bg-card p-5"><div className="flex items-center justify-between"><div><p className="text-sm text-muted-foreground">Service pattern</p><h2 className="mt-1 text-lg font-semibold">{route.title}</h2></div><StatusBadge label={route.status} tone={route.tone} /></div><ol className="mt-6 space-y-1">{stops.map((stop, index) => <li key={stop} className="flex gap-3"><div className="flex w-6 flex-col items-center"><span className="flex size-6 items-center justify-center rounded-full bg-primary text-xs font-medium text-primary-foreground">{index + 1}</span>{index < stops.length - 1 && <span className="h-7 w-px bg-primary/30" />}</div><div className="pb-3"><p className="text-sm font-medium">{stop}</p><p className="text-xs text-muted-foreground">Estimated {index === 0 ? "departure" : `+${index * 8} min`}</p></div></li>)}</ol></article><article className="rounded-lg border bg-card p-5"><h2 className="font-semibold">Today’s service</h2><div className="mt-4 space-y-4"><Detail label="Scheduled trips" value="18" /><Detail label="Vehicles assigned" value="17 of 18" /><Detail label="On-time performance" value="92%" /><Detail label="Average occupancy" value="68%" /></div>{!readOnly && <Button className="mt-6 w-full">Schedule trip</Button>}</article></section></main>;
}

export function RouteFormPage() {
  const { routeId } = useParams();
  const navigate = useNavigate();
  const { data, saveRecord } = useMockData();
  const { user } = useAuth();
  const editing = Boolean(routeId);
  const existing = data.routes?.find((item) => item.id === routeId);
  return <main className="flex flex-1 flex-col gap-4 bg-muted/20 p-4"><PageHeading title={editing ? `Edit route ${routeId}` : "Create route"} description="Set a clear service corridor before scheduling vehicles and drivers." /><form onSubmit={(event) => { event.preventDefault(); const form = new FormData(event.currentTarget); const id = String(form.get("routeNumber") || routeId || "NEW"); const origin = String(form.get("origin") || "Origin"); const destination = String(form.get("destination") || "Destination"); saveRecord("routes", { id, title: `${origin} → ${destination}`, subtitle: `3 stops · ${String(form.get("serviceArea") || "Unassigned")} service area`, status: existing?.status ?? "Active", tone: existing?.tone ?? "good", updated: "No performance data yet", meta: "No trips scheduled", district: String(form.get("district") || "Colombo"), centreId: user?.centreId }); navigate(`/network/routes/${id}`); }} className="max-w-3xl rounded-lg border bg-card p-5"><div className="grid gap-4 sm:grid-cols-2"><Field name="routeNumber" label="Route number" value={routeId ?? ""} placeholder="138" /><Field name="serviceArea" label="Service area" placeholder="Western" /><Field name="district" label="District" value={existing?.district} placeholder="Colombo" /><Field name="origin" label="Origin" placeholder="Maharagama" /><Field name="destination" label="Destination" placeholder="Pettah" /></div><div className="mt-5"><p className="text-sm font-medium">Ordered stops</p><div className="mt-2 space-y-2">{defaultRouteStops.slice(0, 3).map((stop, index) => <div key={stop} className="flex items-center gap-2"><span className="flex size-8 items-center justify-center rounded border bg-muted/60 text-xs">{index + 1}</span><Input defaultValue={stop} /><Button type="button" size="icon" variant="ghost" aria-label={`Remove ${stop}`}><Trash2 /></Button></div>)}</div><Button type="button" variant="outline" className="mt-3">Add stop</Button></div><div className="mt-5 flex justify-end gap-2"><Button type="button" variant="outline" onClick={() => navigate(-1)}>Cancel</Button><Button type="submit">{editing ? "Save route" : "Create route"}</Button></div></form></main>;
}

function Field({ label, value, ...props }: { label: string; name: string; value?: string; placeholder?: string }) { return <label className="grid gap-1.5 text-sm font-medium">{label}<Input defaultValue={value} {...props} /></label>; }
function Detail({ label, value }: { label: string; value: string }) { return <div className="flex items-center justify-between border-b pb-3 text-sm"><span className="text-muted-foreground">{label}</span><span className="font-medium">{value}</span></div>; }
