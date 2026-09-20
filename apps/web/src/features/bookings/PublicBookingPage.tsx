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
import { Link, useNavigate } from "react-router";
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
};

export function PublicBookingPage() {
  const navigate = useNavigate();
  const { user, logout } = useAuth();
  const [origin, setOrigin] = useState("");
  const [destination, setDestination] = useState("");
  const [date, setDate] = useState("");
  const [passengers, setPassengers] = useState(1);
  const [busType, setBusType] = useState("all");
  const [selected, setSelected] = useState<Route | null>(null);
  const portal = user?.role === "Admin" || user?.role === "SuperAdmin"
    ? { label: "Go to Admin portal", path: "/admin" }
    : user && ["CentreManager", "Dispatcher", "FleetOfficer", "Driver"].includes(user.role)
      ? { label: "Go to Operations portal", path: "/operations" }
      : null;
  const { data: routes = [], isLoading } = useQuery({
    queryKey: ["public", "routes"],
    queryFn: () => apiClient<Route[]>("/api/v1/routes"),
  });
  const results = useMemo(
    () =>
      routes.filter(
        (route) =>
          route.isActive &&
          (!origin ||
            route.origin.toLowerCase().includes(origin.toLowerCase())) &&
          (!destination ||
            route.destination
              .toLowerCase()
              .includes(destination.toLowerCase())),
      ),
    [destination, origin, routes],
  );

  return (
    <main className="min-h-screen bg-white">
      <header className="border-b border-slate-200 bg-white">
        <div className="mx-auto flex max-w-6xl items-center justify-between px-6 py-4">
          <Link
            to="/"
            className="text-2xl font-bold tracking-tight text-indigo-950"
          >
            UPTSLK{" "}
            <span className="font-normal text-slate-500">Seat Reservation</span>
          </Link>
          <div className="flex items-center gap-3">
            {user ? (
              <>
                {portal && (
                  <Button
                    variant="outline"
                    className="h-12 rounded-full border-primary/30 bg-primary/5 px-6 text-primary hover:bg-primary/10"
                    onClick={() => navigate(portal.path)}
                  >
                    {portal.label}
                  </Button>
                )}
                <Button
                  variant="outline"
                  className="h-12 rounded-full border-slate-300 bg-white px-6"
                  onClick={() => navigate("/profile")}
                >
                  My Profile
                </Button>
                <Button
                  className="h-12 rounded-full bg-red-600 px-6 hover:bg-red-700"
                  onClick={() => {
                    logout();
                    navigate("/");
                  }}
                >
                  Sign out
                </Button>
              </>
            ) : (
              <Link to="/login">
                <Button className="h-12 rounded-full bg-indigo-950 px-6">
                  Sign in
                </Button>
              </Link>
            )}
          </div>
        </div>
      </header>
      <div className="mx-auto max-w-6xl space-y-5 px-6 py-7">
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
              choose a departure, select seats, and pay.
            </p>
            <Button
              className="mt-4 rounded-full bg-primary"
              onClick={() =>
                user
                  ? navigate(
                      `/booking/checkout?routeId=${selected.id}&passengers=${passengers}`,
                    )
                  : navigate("/login")
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
