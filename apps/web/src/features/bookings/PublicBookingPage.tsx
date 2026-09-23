import { useMemo, useState } from "react";
import { CalendarDays, ChevronRight, Minus, Plus, Search } from "lucide-react";
import { useNavigate } from "react-router";
import { useQuery } from "@tanstack/react-query";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Calendar } from "@/components/ui/calendar";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover";
import { apiClient } from "@/lib/api/api-client";
import { useAuth } from "@/hooks/useAuth";
import { cn } from "@/lib/utils";
import {
  Dialog,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";

type Route = {
  id: string;
  routeNumber: string;
  name: string;
  origin: string;
  destination: string;
  isActive: boolean;
  directions?: {
    id: string;
    startCentre: { name: string };
    endCentre: { name: string };
    name: string;
    isActive: boolean;
  }[];
};

type JourneyOption = {
  id: string;
  routeId: string;
  routeNumber: string;
  name: string;
  origin: string;
  destination: string;
};

export function PublicBookingPage() {
  const navigate = useNavigate();
  const { user } = useAuth();
  const [origin, setOrigin] = useState("");
  const [destination, setDestination] = useState("");
  const [date, setDate] = useState("");
  const [passengers, setPassengers] = useState(1);
  const [selected, setSelected] = useState<JourneyOption | null>(null);
  const { data: routes = [], isLoading } = useQuery({
    queryKey: ["public", "routes"],
    queryFn: () => apiClient<Route[]>("/api/v1/routes"),
  });
  const results = useMemo(
    () =>
      routes
        .filter((route) => route.isActive)
        .flatMap((route) =>
          route.directions?.length
            ? route.directions
                .filter((direction) => direction.isActive)
                .map((direction) => ({
                  id: direction.id,
                  routeId: route.id,
                  routeNumber: route.routeNumber,
                  name: route.name,
                  origin: direction.startCentre.name,
                  destination: direction.endCentre.name,
                }))
            : [
                {
                  id: "",
                  routeId: route.id,
                  routeNumber: route.routeNumber,
                  name: route.name,
                  origin: route.origin,
                  destination: route.destination,
                },
              ],
        )
        .filter(
          (journey) =>
            (!origin ||
              journey.origin.toLowerCase().includes(origin.toLowerCase())) &&
            (!destination ||
              journey.destination
                .toLowerCase()
                .includes(destination.toLowerCase())),
        ),
    [destination, origin, routes],
  );

  return (
    <main className="min-h-screen bg-white">
      <div className="w-full space-y-5 px-8 py-7 sm:px-12 lg:px-16 xl:px-20">
        <section className="rounded-2xl border border-slate-300 bg-white p-5 shadow-sm">
          <div className="mb-4">
            <span className="inline-flex rounded-full bg-primary px-3 py-1.5 text-sm font-medium text-primary-foreground">
              One-way journey
            </span>
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
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {results.map((route) => (
              <article
                key={route.id}
                className="flex min-h-44 flex-col rounded-xl border-2 border-slate-400 bg-white p-4 shadow-md transition-shadow hover:border-primary/50 hover:shadow-lg"
              >
                <div className="grid flex-1 grid-cols-[minmax(0,1fr)_auto_minmax(0,1fr)] items-center gap-3">
                  <div>
                    <p className="text-base font-semibold text-indigo-950">
                      {route.origin}
                    </p>
                    <p className="mt-1 text-xs text-slate-500">
                      {route.routeNumber}
                    </p>
                  </div>
                  <span className="flex size-8 shrink-0 items-center justify-center rounded-full bg-indigo-50 text-indigo-700">
                    <ChevronRight className="size-5" strokeWidth={2.5} />
                  </span>
                  <div className="text-right">
                    <p className="text-base font-semibold text-indigo-950">
                      {route.destination}
                    </p>
                    <p className="mt-1 text-xs text-slate-500">{route.name}</p>
                  </div>
                </div>
                <Button
                  className="mt-4 h-9 w-full rounded-lg bg-primary"
                  onClick={() => setSelected(route)}
                >
                  Select departure
                </Button>
              </article>
            ))}
          </div>
        </section>
        <Dialog
          open={Boolean(selected)}
          onOpenChange={(open) => {
            if (!open) setSelected(null);
          }}
        >
          {selected && (
            <DialogContent className="sm:max-w-2xl">
              <DialogHeader>
                <DialogTitle>Confirm your journey</DialogTitle>
                <DialogDescription>
                  Review the route and passenger count before choosing a
                  departure.
                </DialogDescription>
              </DialogHeader>
              <div className="rounded-xl border border-indigo-200 bg-indigo-50 p-4">
                <p className="font-semibold text-indigo-950">
                  {selected.origin} → {selected.destination}
                </p>
                <p className="mt-1 text-sm text-indigo-700">
                  Route {selected.routeNumber} · {selected.name}
                </p>
                <div className="mt-4">
                  <p className="text-xs font-medium uppercase tracking-wide text-indigo-700">
                    Passengers
                  </p>
                  <PassengerCounter
                    className="mt-1 max-w-40 border border-indigo-300 bg-white"
                    value={passengers}
                    onChange={setPassengers}
                  />
                </div>
              </div>
              <DialogFooter>
                <DialogClose render={<Button variant="outline" />}>
                  Cancel
                </DialogClose>
                <Button
                  onClick={() => {
                    const checkoutUrl = `/booking/checkout?routeId=${selected.routeId}&directionId=${selected.id}&passengers=${passengers}&date=${date}`;
                    setSelected(null);
                    navigate(
                      user
                        ? checkoutUrl
                        : `/login?returnTo=${encodeURIComponent(checkoutUrl)}`,
                    );
                  }}
                >
                  {user ? "Continue to booking" : "Sign in to continue"}
                </Button>
              </DialogFooter>
            </DialogContent>
          )}
        </Dialog>
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
  className,
}: {
  value: number;
  onChange: (value: number) => void;
  className?: string;
}) {
  return (
    <div
      className={cn(
        "flex h-12 items-center justify-between rounded-full bg-[#f5f5f7] px-1 text-sm text-slate-600",
        className,
      )}
    >
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
        disabled={value >= 10}
        onClick={() => onChange(value + 1)}
      >
        <Plus />
      </Button>
    </div>
  );
}
