import { useState } from "react";
import { useNavigate, useParams } from "react-router";
import { CalendarDays } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Calendar } from "@/components/ui/calendar";
import { Input } from "@/components/ui/input";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import { TimePicker } from "@/components/custom/TimePicker";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { PageHeading } from "@/components/shared/PageHeading";
import { useAuth } from "@/hooks/useAuth";
import { useRoutes } from "@/features/network/hooks/useRoutes";
import { useBays } from "@/features/centres/hooks/useCentres";
import { useDrivers, useVehicles } from "@/features/fleet/hooks";
import { useCreateTrip, useTrip, useUpdateTrip } from "./hooks/useTrips";

export function TripFormPage() {
  const { tripId } = useParams();
  const navigate = useNavigate();
  const { user } = useAuth();
  const centreId = user?.centreId;
  const editing = Boolean(tripId);
  const { data: existing, isLoading } = useTrip(tripId);
  const { data: routes = [] } = useRoutes(centreId);
  const [selectedRouteId, setSelectedRouteId] = useState("");
  const [selectedDirectionId, setSelectedDirectionId] = useState("");
  const selectedRoute = routes.find((route) => route.id === (selectedRouteId || existing?.routeId));
  const activeDirectionId = selectedDirectionId || existing?.routeDirectionId || selectedRoute?.directions?.[0]?.id || "";
  const selectedDirection = selectedRoute?.directions?.find((direction) => direction.id === activeDirectionId);
  const { data: vehicles = [] } = useVehicles({ centreId, status: "Active" });
  const { data: drivers = [] } = useDrivers({ centreId, status: "Active" });
  const { data: bays = [] } = useBays(selectedDirection?.startCentreId ?? centreId);
  const createMutation = useCreateTrip();
  const updateMutation = useUpdateTrip(tripId ?? "");
  const [error, setError] = useState("");
  const [serviceDate, setServiceDate] = useState("");
  if (editing && isLoading) return <main className="p-5">Loading trip...</main>;
  if (editing && !existing) return <main className="p-5">Trip not found.</main>;
  const scheduled = existing?.scheduledTime
    ? new Date(existing.scheduledTime)
    : undefined;
  const dateValue = serviceDate || scheduled?.toISOString().slice(0, 10) || "";
  const submit = (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setError("");
    const form = new FormData(event.currentTarget);
    const data = {
      centreId: centreId ?? "",
      routeId: selectedRoute?.id ?? String(form.get("routeId")),
      routeDirectionId: activeDirectionId || undefined,
      vehicleId: String(form.get("vehicleId")),
      driverId: String(form.get("driverId")),
      bayId: String(form.get("bayId")),
      scheduledTime: new Date(`${dateValue}T${form.get("time")}`).toISOString(),
      notes: String(form.get("notes") || ""),
    };
    const mutation = editing ? updateMutation : createMutation;
    mutation.mutate(data, {
      onSuccess: (result) =>
        navigate(`/operations/dispatch/${editing ? tripId : result?.id}`),
      onError: (reason) =>
        setError(
          reason instanceof Error
            ? reason.message
            : "Unable to save this trip.",
        ),
    });
  };
  return (
    <main className="flex flex-1 flex-col gap-4 bg-muted/20 p-4">
      <PageHeading
        title={editing ? "Edit trip" : "Schedule trip"}
        description="Assign a valid route, time, bay, vehicle, and driver."
      />
      {error && (
        <div className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
          {error}
        </div>
      )}
      <form
        className="max-w-4xl rounded-lg border bg-card p-5"
        onSubmit={submit}
      >
        <div className="grid gap-4 sm:grid-cols-2">
          <ServiceDatePicker value={dateValue} onChange={setServiceDate} />
          <Field label="Departure time">
            <TimePicker
              name="time"
              defaultValue={scheduled?.toTimeString().slice(0, 5)}
              required
            />
          </Field>
          <SelectField
            label="Route"
            name="routeId"
            value={selectedRoute?.id ?? existing?.routeId}
            onValueChange={(value) => { setSelectedRouteId(value); setSelectedDirectionId(""); }}
            options={routes.map((route) => ({
              value: route.id,
              label: `${route.routeNumber} · ${route.name}`,
            }))}
          />
          <SelectField
            label="Travel direction"
            name="routeDirectionId"
            value={activeDirectionId}
            onValueChange={setSelectedDirectionId}
            options={(selectedRoute?.directions ?? []).filter((direction) => direction.isActive).map((direction) => ({ value: direction.id, label: `${direction.startCentre.name} → ${direction.endCentre.name}` }))}
          />
          <SelectField
            label="Bay"
            name="bayId"
            value={existing?.bayId}
            options={bays
              .filter((bay) => bay.status === "Available")
              .map((bay) => ({ value: bay.id, label: bay.code }))}
          />
          <SelectField
            label="Vehicle"
            name="vehicleId"
            value={existing?.vehicleId}
            options={vehicles.map((vehicle) => ({
              value: vehicle.id,
              label: `${vehicle.plateNumber} · ${vehicle.model}`,
            }))}
          />
          <SelectField
            label="Driver"
            name="driverId"
            value={existing?.driverId}
            options={drivers.map((driver) => ({
              value: driver.id,
              label: `${driver.fullName} · ${driver.licenseNumber}`,
            }))}
          />
          <Field label="Operational notes">
            <Input
              name="notes"
              defaultValue={existing?.notes}
              placeholder="Optional dispatch instructions"
            />
          </Field>
        </div>
        <div className="mt-5 rounded-md border border-blue-200 bg-blue-50 p-3 text-sm text-blue-800">
          The bus and driver belong to your operating centre. The departure bay belongs to the selected direction’s start centre; the API validates availability and conflicts before saving.
        </div>
        <div className="mt-5 flex justify-end gap-2">
          <Button type="button" variant="outline" onClick={() => navigate(-1)}>
            Cancel
          </Button>
          <Button
            type="submit"
            disabled={createMutation.isPending || updateMutation.isPending}
          >
            {createMutation.isPending || updateMutation.isPending
              ? "Saving..."
              : editing
                ? "Save trip"
                : "Schedule trip"}
          </Button>
        </div>
      </form>
    </main>
  );
}
function Field({
  label,
  children,
}: {
  label: string;
  children: React.ReactNode;
}) {
  return (
    <label className="grid gap-1.5 text-sm font-medium">
      {label}
      {children}
    </label>
  );
}

function ServiceDatePicker({ value, onChange }: { value: string; onChange: (value: string) => void }) {
  const [open, setOpen] = useState(false);
  const selected = value ? new Date(`${value}T00:00:00`) : undefined;
  return <Field label="Service date"><input type="hidden" name="date" value={value} required /><Popover open={open} onOpenChange={setOpen}><PopoverTrigger render={<Button type="button" variant="outline" className="h-10 w-full justify-start font-normal" />}><CalendarDays className="mr-2 size-4" />{selected ? selected.toLocaleDateString() : "Select service date"}</PopoverTrigger><PopoverContent align="start" className="w-auto p-0"><Calendar mode="single" selected={selected} onSelect={(day) => { if (day) { onChange(day.toISOString().slice(0, 10)); setOpen(false); } }} /></PopoverContent></Popover></Field>;
}
function SelectField({
  label,
  name,
  value,
  onValueChange,
  options,
}: {
  label: string;
  name: string;
  value?: string;
  onValueChange?: (value: string) => void;
  options: { value: string; label: string }[];
}) {
  return (
    <Field label={label}>
      <Select
        name={name}
        value={value || options[0]?.value || null}
        onValueChange={(selected) => onValueChange?.(String(selected ?? ""))}
        itemToStringLabel={(selected) =>
          options.find((option) => option.value === selected)?.label ?? selected
        }
        required
      >
        <SelectTrigger className="w-full bg-muted/60">
          <SelectValue placeholder={`Select ${label.toLowerCase()}`} />
        </SelectTrigger>
        <SelectContent>
          {options.map((option) => (
            <SelectItem key={option.value} value={option.value}>
              {option.label}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>
    </Field>
  );
}
