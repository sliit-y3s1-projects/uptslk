import { useMemo, useState } from "react";
import {
  BusFront,
  CalendarDays,
  Minus,
  Plus,
  Search,
  SlidersHorizontal,
  Ticket,
} from "lucide-react";
import { useNavigate } from "react-router";
import { useQuery } from "@tanstack/react-query";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Calendar } from "@/components/ui/calendar";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover";
import { RadioGroup, RadioGroupItem } from "@/components/ui/radio-group";
import { apiClient } from "@/lib/api/api-client";
import { useAuth } from "@/hooks/useAuth";

type Route = {
  id: string;
  routeNumber: string;
  name: string;
  origin: string;
  destination: string;
  isActive: boolean;
  directions?: { id: string; startCentre: { name: string }; endCentre: { name: string }; name: string; isActive: boolean }[];
};

type JourneyOption = { id: string; routeId: string; routeNumber: string; name: string; origin: string; destination: string };

export function PublicBookingPage() {
  const navigate = useNavigate();
  const { user } = useAuth();
  const [origin, setOrigin] = useState("");
  const [destination, setDestination] = useState("");
  const [date, setDate] = useState("");
  const [passengers, setPassengers] = useState(1);
  const [busType, setBusType] = useState("all");
  const [selected, setSelected] = useState<JourneyOption | null>(null);
  const { data: routes = [], isLoading } = useQuery({
    queryKey: ["public", "routes"],
    queryFn: () => apiClient<Route[]>("/api/v1/routes"),
  });
  const results = useMemo(() => routes.filter((route) => route.isActive).flatMap((route) => (route.directions?.length ? route.directions.filter((direction) => direction.isActive).map((direction) => ({ id: direction.id, routeId: route.id, routeNumber: route.routeNumber, name: route.name, origin: direction.startCentre.name, destination: direction.endCentre.name })) : [{ id: "", routeId: route.id, routeNumber: route.routeNumber, name: route.name, origin: route.origin, destination: route.destination }])).filter((journey) => (!origin || journey.origin.toLowerCase().includes(origin.toLowerCase())) && (!destination || journey.destination.toLowerCase().includes(destination.toLowerCase()))), [destination, origin, routes]);

  return (
    <main className="min-h-screen bg-white">
      <div className="w-full space-y-5 px-6 py-7">
        <section className="rounded-2xl border border-slate-200 bg-white p-5">
          <div className="mb-4 flex gap-2">
            <Button size="sm" className="rounded-full bg-primary">
              One-way
            </Button>
            <Button size="sm" variant="outline" className="rounded-full">
              Round trip
            </Button>
          </div>
          <div className="grid gap-3 md:grid-cols-[1fr_1fr_1fr_150px_auto] md:items-end">
            <Field
              label="Boarding point"
              value={origin}
              onChange={setOrigin}
              placeholder="From"
            />
            <Field
              label="Drop-off point"
              value={destination}
              onChange={setDestination}
              placeholder="To"
            />
            <DatePicker value={date} onChange={setDate} />
            <PassengerCounter value={passengers} onChange={setPassengers} />
            <Button className="h-12 rounded-full bg-primary px-6">
              <Search /> Search bus
            </Button>
          </div>
        </section>
        <div className="grid gap-5 lg:grid-cols-[220px_1fr]">
          <aside className="rounded-2xl border border-slate-200 bg-white p-5">
            <div className="flex items-center justify-between">
              <h2 className="font-semibold text-black">Filters</h2>
              <Button
                variant="ghost"
                size="sm"
                className="text-red-500"
                onClick={() => setBusType("all")}
              >
                Reset
              </Button>
            </div>
            <div className="mt-6 border-t pt-5">
              <p className="text-sm font-medium">Bus type</p>
              <RadioGroup
                value={busType}
                onValueChange={setBusType}
                className="mt-3 space-y-3 text-sm text-slate-600"
              >
                <label className="flex items-center gap-2">
                  <RadioGroupItem value="all" id="bus-all" />
                  <Label htmlFor="bus-all">All</Label>
                </label>
                <label className="flex items-center gap-2">
                  <RadioGroupItem value="ac" id="bus-ac" />
                  <Label htmlFor="bus-ac">AC</Label>
                </label>
                <label className="flex items-center gap-2">
                  <RadioGroupItem value="non-ac" id="bus-non-ac" />
                  <Label htmlFor="bus-non-ac">Non-AC</Label>
                </label>
              </RadioGroup>
            </div>
            <div className="mt-6 border-t pt-5">
              <p className="text-sm font-medium">Operators</p>
              <p className="mt-3 text-sm text-slate-500">
                All available operators
              </p>
            </div>
            <Button className="mt-8 w-full rounded-full bg-primary">
              <SlidersHorizontal /> Apply filters
            </Button>
          </aside>
          <section className="space-y-3">
            <div className="flex items-center justify-between">
              <h2 className="text-lg font-semibold text-black">
                Bus from {origin || "all origins"} to{" "}
                {destination || "all destinations"}
              </h2>
              <span className="text-sm text-slate-500">
                {results.length} results found
              </span>
            </div>
            {isLoading && (
              <p className="rounded-xl bg-white p-8 text-center">
                Loading routes...
              </p>
            )}
            {!isLoading && results.length === 0 && (
              <p className="rounded-xl bg-white p-8 text-center text-slate-500">
                No active routes found.
              </p>
            )}
            <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">
              {results.map((route) => (
                <article
                  key={route.id}
                  className="rounded-2xl border border-slate-200 bg-white p-4"
                >
                  <div className="flex items-center justify-between">
                    <span className="flex items-center gap-2 text-sm font-medium">
                      <span className="flex size-8 items-center justify-center rounded-full bg-indigo-100 text-indigo-700">
                        <BusFront className="size-4" />
                      </span>{" "}
                      UPTSLK Transit
                    </span>
                    <span className="rounded-full bg-amber-50 px-2 py-1 text-xs text-amber-700">
                      Public bus
                    </span>
                  </div>
                  <div className="mt-5 flex items-end justify-between">
                    <div>
                      <p className="text-lg font-semibold text-indigo-950">
                        {route.origin}
                      </p>
                      <p className="text-xs text-slate-500">
                        {route.routeNumber}
                      </p>
                    </div>
                    <span>→</span>
                    <div className="text-right">
                      <p className="text-lg font-semibold text-indigo-950">
                        {route.destination}
                      </p>
                      <p className="text-xs text-slate-500">{route.name}</p>
                    </div>
                  </div>
                  <Button
                    className="mt-4 w-full rounded-full bg-primary"
                    onClick={() => setSelected(route)}
                  >
                    Select departure <Ticket />
                  </Button>
                </article>
              ))}
            </div>
          </section>
        </div>
        {selected && (
          <div className="rounded-2xl border border-indigo-200 bg-white p-5">
            <p className="font-semibold">
              {selected.origin} → {selected.destination}
            </p>
            <p className="mt-1 text-sm text-slate-500">
              {passengers} passenger{passengers === 1 ? "" : "s"}. Sign in to
              choose a departure and pay. Seating is first-come, first-served.
            </p>
            <Button
              className="mt-4 rounded-full bg-primary"
              onClick={() =>
                user
                  ? navigate(
                      `/booking/checkout?routeId=${selected.routeId}&directionId=${selected.id}&passengers=${passengers}&date=${date}`,
                    )
                  : navigate(
                      `/login?returnTo=${encodeURIComponent(`/booking/checkout?routeId=${selected.routeId}&directionId=${selected.id}&passengers=${passengers}&date=${date}`)}`,
                    )
              }
            >
              {user ? "Continue to booking" : "Sign in to continue"}
            </Button>
          </div>
        )}
      </div>
    </main>
  );
}

function Field({
  label,
  value,
  onChange,
  placeholder,
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  placeholder: string;
}) {
  return (
    <label className="grid gap-1 text-xs text-slate-500">
      <span>{label}</span>
      <Input
        className="h-12 rounded-full bg-[#f5f5f7] px-4"
        value={value}
        onChange={(event) => onChange(event.target.value)}
        placeholder={placeholder}
      />
    </label>
  );
}
function DatePicker({
  value,
  onChange,
}: {
  value: string;
  onChange: (value: string) => void;
}) {
  const [open, setOpen] = useState(false);
  const selected = value ? new Date(`${value}T00:00:00`) : undefined;
  const label = selected
    ? selected.toLocaleDateString(undefined, {
        month: "short",
        day: "numeric",
        year: "numeric",
      })
    : "Select date";
  return (
    <div className="grid gap-1 text-xs text-slate-500">
      <span>Departure date</span>
      <Popover open={open} onOpenChange={setOpen}>
        <PopoverTrigger
          render={
            <Button
              type="button"
              variant="outline"
              className="h-12 w-full justify-start rounded-full border-input bg-[#f5f5f7] px-4 font-normal text-slate-600"
            />
          }
        >
          <CalendarDays className="mr-2 size-4" />
          {label}
        </PopoverTrigger>
        <PopoverContent className="w-auto p-0">
          <Calendar
            mode="single"
            selected={selected}
            onSelect={(day) => {
              if (day) {
                onChange(day.toISOString().slice(0, 10));
                setOpen(false);
              }
            }}
            disabled={{ before: new Date() }}
          />
        </PopoverContent>
      </Popover>
    </div>
  );
}
function PassengerCounter({
  value,
  onChange,
}: {
  value: number;
  onChange: (value: number) => void;
}) {
  return (
    <div className="flex h-12 items-center justify-between rounded-full bg-[#f5f5f7] px-1 text-sm text-slate-600">
      <Button
        type="button"
        variant="ghost"
        size="icon-sm"
        aria-label="Decrease passengers"
        disabled={value <= 1}
        onClick={() => onChange(value - 1)}
      >
        <Minus />
      </Button>
      <span>{value}</span>
      <Button
        type="button"
        variant="ghost"
        size="icon-sm"
        aria-label="Increase passengers"
        disabled={value >= 8}
        onClick={() => onChange(value + 1)}
      >
        <Plus />
      </Button>
    </div>
  );
}
