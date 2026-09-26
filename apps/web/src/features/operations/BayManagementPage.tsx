import { useMemo, useState } from "react";
import {
  Ban,
  BusFront,
  CheckCircle2,
  Pencil,
  Plus,
  RotateCcw,
} from "lucide-react";
import { Link } from "react-router";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { PageHeading } from "@/components/shared/PageHeading";
import { StatusBadge } from "@/components/shared/StatusBadge";
import { BayOperationsGrid } from "@/features/operations/components/BayOperationsGrid";
import { useAuth } from "@/hooks/useAuth";
import {
  useBays,
  useCentre,
  useCreateBay,
  useUpdateBay,
  useDeactivateBay,
} from "@/features/centres/hooks/useCentres";
import { useTrips } from "./hooks/useTrips";
import type { BayOperation } from "@/mock/centre-operations";
import type { Bay } from "@/features/centres/types";

export function BayManagementPage() {
  const { user } = useAuth();
  const centreId = user?.centreId;
  const { data: centre } = useCentre(centreId);
  const { data: apiBays = [], isLoading, error } = useBays(centreId);
  const { data: trips = [] } = useTrips({ centreId });
  const createBay = useCreateBay(centreId ?? "");
  const updateBay = useUpdateBay(centreId ?? "");
  const deactivateBay = useDeactivateBay(centreId ?? "");
  const bays = useMemo<BayOperation[]>(
    () =>
      apiBays.map((bay) => {
        const trip = trips.find(
          (item) =>
            item.bayId === bay.id &&
            !["Completed", "Cancelled"].includes(item.status),
        );
        const state: BayOperation["state"] =
          bay.status === "OutOfService"
            ? "Closed"
            : trip?.status === "Boarding"
              ? "Boarding"
              : trip
                ? "Occupied"
                : "Available";
        return {
          bay: bay.code,
          state,
          route: trip?.routeNumber,
          destination: trip?.routeName,
          vehicle: trip?.vehicle,
          departure: trip
            ? new Date(trip.scheduledTime).toLocaleTimeString([], {
                hour: "2-digit",
                minute: "2-digit",
              })
            : undefined,
          queue: 0,
        };
      }),
    [apiBays, trips],
  );
  const [selectedCode, setSelectedCode] = useState<string>();
  const [showCreate, setShowCreate] = useState(false);
  const [editingBay, setEditingBay] = useState<Bay | null>(null);
  const [createError, setCreateError] = useState("");
  const [editError, setEditError] = useState("");
  const selected = bays.find((bay) => bay.bay === selectedCode) ?? bays[0];
  const setBayStatus = (status: "Available" | "Occupied" | "OutOfService") => {
    const bay = apiBays.find((item) => item.code === selected?.bay);
    if (!bay) return;
    if (status === "OutOfService") deactivateBay.mutate(bay.id);
    else
      updateBay.mutate({
        bayId: bay.id,
        data: { code: bay.code, name: bay.name ?? undefined, status },
      });
  };
  if (isLoading) return <main className="p-5">Loading bays...</main>;
  if (error)
    return <main className="p-5 text-red-600">Failed to load bay data.</main>;
  return (
    <main className="flex flex-1 flex-col gap-4 bg-muted/20 p-4">
      <PageHeading
        title="Bay management"
        description={`Control bay availability and live assignments at ${centre?.name ?? "the selected centre"}.`}
        action={
          <div className="flex gap-2">
            <Button
              variant="outline"
              onClick={() => {
                setShowCreate((value) => !value);
                setCreateError("");
              }}
            >
              <Plus /> Create bay
            </Button>
            <Button render={<Link to="/operations/dispatch/new" />}>
              <BusFront /> Assign bus
            </Button>
          </div>
        }
      />
      <Dialog open={showCreate} onOpenChange={setShowCreate}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Create bay</DialogTitle>
            <DialogDescription>
              Add a boarding bay to {centre?.name ?? "this centre"}.
            </DialogDescription>
          </DialogHeader>
          <form
            className="grid gap-4"
            onSubmit={(event) => {
              event.preventDefault();
              setCreateError("");
              const form = new FormData(event.currentTarget);
              createBay.mutate(
                {
                  code: String(form.get("code")),
                  name: String(form.get("name") || "") || undefined,
                  status: String(form.get("status") || "Available") as
                    "Available" | "Occupied" | "OutOfService",
                },
                {
                  onSuccess: (bay) => {
                    setSelectedCode(bay.code);
                    setShowCreate(false);
                  },
                  onError: (reason) =>
                    setCreateError(
                      reason instanceof Error
                        ? reason.message
                        : "Unable to create bay.",
                    ),
                },
              );
            }}
          >
            <BayFormFields />
            {createError && (
              <p className="text-sm text-red-600">{createError}</p>
            )}
            <DialogFooter>
              <Button
                type="button"
                variant="outline"
                onClick={() => setShowCreate(false)}
              >
                Cancel
              </Button>
              <Button type="submit" disabled={createBay.isPending}>
                {createBay.isPending ? "Creating..." : "Create bay"}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
      <Dialog
        open={editingBay !== null}
        onOpenChange={(open) => {
          if (!open) setEditingBay(null);
        }}
      >
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Edit bay</DialogTitle>
            <DialogDescription>
              Update the bay information and availability status.
            </DialogDescription>
          </DialogHeader>
          {editingBay && (
            <form
              className="grid gap-4"
              onSubmit={(event) => {
                event.preventDefault();
                setEditError("");
                const form = new FormData(event.currentTarget);
                const code = String(form.get("code")).trim().toUpperCase();
                updateBay.mutate(
                  {
                    bayId: editingBay.id,
                    data: {
                      code,
                      name: String(form.get("name") || "") || undefined,
                      status: String(form.get("status")) as
                        | "Available"
                        | "Occupied"
                        | "OutOfService",
                    },
                  },
                  {
                    onSuccess: () => {
                      setSelectedCode(code);
                      setEditingBay(null);
                    },
                    onError: (reason) =>
                      setEditError(
                        reason instanceof Error
                          ? reason.message
                          : "Unable to update bay.",
                      ),
                  },
                );
              }}
            >
              <BayFormFields bay={editingBay} />
              {editError && (
                <p className="text-sm text-red-600">{editError}</p>
              )}
              <DialogFooter>
                <Button
                  type="button"
                  variant="outline"
                  onClick={() => setEditingBay(null)}
                >
                  Cancel
                </Button>
                <Button type="submit" disabled={updateBay.isPending}>
                  {updateBay.isPending ? "Saving..." : "Save changes"}
                </Button>
              </DialogFooter>
            </form>
          )}
        </DialogContent>
      </Dialog>
      <section className="grid gap-3 sm:grid-cols-3">
        <Metric
          label="Active bays"
          value={`${bays.filter((bay) => bay.state !== "Closed").length}/${bays.length}`}
        />
        <Metric
          label="Boarding"
          value={String(bays.filter((bay) => bay.state === "Boarding").length)}
        />
        <Metric
          label="Available"
          value={String(bays.filter((bay) => bay.state === "Available").length)}
        />
      </section>
      {!selected ? (
        <div className="rounded-lg border border-dashed bg-card p-10 text-center text-sm text-muted-foreground">
          No bays configured for this centre.
        </div>
      ) : (
        <section className="grid gap-4 xl:grid-cols-[minmax(0,1fr)_330px]">
          <BayOperationsGrid
            bays={bays}
            selected={selected}
            onSelect={(bay) => setSelectedCode(bay.bay)}
          />
          <aside className="rounded-lg border bg-card p-5">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-xs text-muted-foreground">Bay status</p>
                <h2 className="text-xl font-semibold">{selected.bay}</h2>
              </div>
              <StatusBadge
                label={selected.state}
                tone={
                  selected.state === "Closed"
                    ? "danger"
                    : selected.state === "Boarding"
                      ? "warning"
                      : selected.state === "Available"
                        ? "neutral"
                        : "good"
                }
              />
            </div>
            {selected.vehicle ? (
              <div className="mt-5 space-y-3 rounded-md bg-muted/50 p-4 text-sm">
                <Fact label="Vehicle" value={selected.vehicle} />
                <Fact
                  label="Service"
                  value={`Route ${selected.route} · ${selected.destination}`}
                />
                <Fact label="Departure" value={selected.departure ?? "—"} />
              </div>
            ) : (
              <p className="mt-5 rounded-md border border-dashed p-4 text-sm text-muted-foreground">
                No active trip is assigned to this bay.
              </p>
            )}
            <div className="mt-5 grid gap-2">
              <Button
                variant="outline"
                onClick={() => {
                  const bay = apiBays.find(
                    (item) => item.code === selected.bay,
                  );
                  if (!bay) return;
                  setEditError("");
                  setEditingBay(bay);
                }}
              >
                <Pencil /> Edit bay
              </Button>
              <Button
                onClick={() => setBayStatus("Occupied")}
                disabled={selected.state === "Closed" || updateBay.isPending}
              >
                <CheckCircle2 /> Mark occupied
              </Button>
              <Button
                variant="outline"
                onClick={() => setBayStatus("Available")}
                disabled={selected.state === "Closed" || updateBay.isPending}
              >
                <RotateCcw /> Release bay
              </Button>
              <Button
                variant="outline"
                onClick={() => setBayStatus("OutOfService")}
                disabled={
                  selected.state === "Closed" || deactivateBay.isPending
                }
              >
                <Ban /> Close bay
              </Button>
            </div>
          </aside>
        </section>
      )}
    </main>
  );
}

function BayFormFields({ bay }: { bay?: Bay }) {
  return (
    <div className="grid gap-4">
      <label className="grid gap-1.5 text-sm font-medium">
        Bay code
        <Input name="code" defaultValue={bay?.code} placeholder="B14" required />
      </label>
      <label className="grid gap-1.5 text-sm font-medium">
        Bay name
        <Input
          name="name"
          defaultValue={bay?.name ?? ""}
          placeholder="Express boarding bay"
        />
      </label>
      <label className="grid gap-1.5 text-sm font-medium">
        Status
        <Select name="status" defaultValue={bay?.status ?? "Available"}>
          <SelectTrigger>
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="Available">Available</SelectItem>
            <SelectItem value="Occupied">Occupied</SelectItem>
            <SelectItem value="OutOfService">Out of service</SelectItem>
          </SelectContent>
        </Select>
      </label>
    </div>
  );
}

function Metric({ label, value }: { label: string; value: string }) {
  return (
    <article className="rounded-lg border bg-card p-4">
      <p className="text-sm text-muted-foreground">{label}</p>
      <p className="mt-2 text-2xl font-semibold">{value}</p>
    </article>
  );
}
function Fact({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <p className="text-xs text-muted-foreground">{label}</p>
      <p className="mt-1 font-medium">{value}</p>
    </div>
  );
}
