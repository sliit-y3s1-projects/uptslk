import { useState } from "react";
import { useNavigate, useParams } from "react-router";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { PageHeading } from "@/components/shared/PageHeading";
import { useAuth } from "@/hooks/useAuth";
import { useDispatchMock } from "@/context/DispatchMockContext";
import { centres } from "@/mock/centres";
import { dispatchResources, type Trip } from "@/mock/dispatch";

export function TripFormPage() {
  const { tripId } = useParams();
  const navigate = useNavigate();
  const { user } = useAuth();
  const { trips, saveTrip } = useDispatchMock();
  const centre = centres.find((item) => item.id === user?.centreId) ?? centres[0];
  const resources = dispatchResources[centre.id as keyof typeof dispatchResources] ?? dispatchResources.makumbura;
  const existing = trips.find((trip) => trip.id === tripId && trip.centreId === centre.id);
  const [error, setError] = useState("");

  return <main className="flex flex-1 flex-col gap-4 bg-muted/20 p-4"><PageHeading title={existing ? `Edit ${existing.id}` : "Schedule trip"} description={`Assign a valid route, time, bay, vehicle, and driver at ${centre.name}.`} />{error && <div className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">{error}</div>}<form className="max-w-4xl rounded-lg border bg-card p-5" onSubmit={(event) => { event.preventDefault(); setError(""); const form = new FormData(event.currentTarget); const routeValue = String(form.get("route")); const [route, destination = "Destination"] = routeValue.split(" · "); const trip: Trip = { id: existing?.id ?? `TRP-${Date.now().toString().slice(-5)}`, centreId: centre.id, serviceDate: String(form.get("serviceDate")), scheduledTime: String(form.get("scheduledTime")), route, origin: centre.name, destination, vehicleId: String(form.get("vehicleId")), driverId: String(form.get("driverId")), bay: String(form.get("bay")), occupancy: existing?.occupancy ?? 0, status: existing?.status ?? "Scheduled", notes: String(form.get("notes")), updatedAt: "Updated just now" }; try { saveTrip(trip); navigate(`/operations/dispatch/${trip.id}`); } catch (reason) { setError(reason instanceof Error ? reason.message : "Unable to save this trip."); } }}><div className="grid gap-4 sm:grid-cols-2"><Field label="Service date"><Input name="serviceDate" type="date" defaultValue={existing?.serviceDate ?? "2026-09-07"} required /></Field><Field label="Departure time"><Input name="scheduledTime" type="time" defaultValue={existing?.scheduledTime} required /></Field><SelectField label="Route" name="route" value={existing ? `${existing.route} · ${existing.destination}` : resources.routes[0]} options={resources.routes} /><SelectField label="Bay" name="bay" value={existing?.bay ?? resources.bays[0]} options={resources.bays} /><SelectField label="Vehicle" name="vehicleId" value={existing?.vehicleId ?? resources.vehicles[0]} options={resources.vehicles} /><SelectField label="Driver" name="driverId" value={existing?.driverId ?? resources.drivers[0]} options={resources.drivers} /><label className="grid gap-1.5 text-sm font-medium sm:col-span-2">Operational notes<Input name="notes" defaultValue={existing?.notes} placeholder="Optional dispatch instructions" /></label></div><div className="mt-5 rounded-md border border-blue-200 bg-blue-50 p-3 text-sm text-blue-800">The form checks the selected time for bay, vehicle, and driver conflicts before saving.</div><div className="mt-5 flex justify-end gap-2"><Button type="button" variant="outline" onClick={() => navigate(-1)}>Cancel</Button><Button type="submit">{existing ? "Save trip" : "Schedule trip"}</Button></div></form></main>;
}

function Field({ label, children }: { label: string; children: React.ReactNode }) { return <label className="grid gap-1.5 text-sm font-medium">{label}{children}</label>; }
function SelectField({ label, name, value, options }: { label: string; name: string; value: string; options: string[] }) { return <Field label={label}><Select name={name} defaultValue={value}><SelectTrigger className="w-full bg-muted/60"><SelectValue /></SelectTrigger><SelectContent>{options.map((option) => <SelectItem key={option} value={option}>{option}</SelectItem>)}</SelectContent></Select></Field>; }
