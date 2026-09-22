import { useState } from "react";
import { SlidersHorizontal } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Sheet, SheetContent, SheetDescription, SheetFooter, SheetHeader, SheetTitle } from "@/components/ui/sheet";
import {
  Panel,
  Field,
  QueryState,
  Feedback,
} from "@/features/riders/components/FeatureUi";
import { money } from "@/features/riders/components/format";
import type { PassengerCategory } from "@/features/riders/types/passengers";
import type { FareRule, RouteOption } from "../types/fares";
import {
  useFareRules,
  useFareRule,
  useFareRuleMutations,
} from "../hooks/useFareRules";
import { useRoutes } from "../hooks/useBookings";
import { useAuth } from "@/hooks/useAuth";

function FilterSelect({ label, value, onValueChange, items }: { label: string; value: string; onValueChange: (value: string) => void; items: { value: string; label: string }[] }) {
  return <div className="flex flex-col gap-1.5"><label className="text-sm font-medium">{label}</label><Select value={value} onValueChange={(nextValue) => onValueChange(String(nextValue ?? "all"))}><SelectTrigger className="h-11 w-full"><SelectValue /></SelectTrigger><SelectContent>{items.map((item) => <SelectItem key={item.value} value={item.value}>{item.label}</SelectItem>)}</SelectContent></Select></div>;
}

function FareForm({
  rule,
  routes,
  onSaved,
  onClose,
}: {
  rule?: FareRule;
  routes: RouteOption[];
  onSaved: () => void;
  onClose: () => void;
}) {
  const { create, update } = useFareRuleMutations();
  const [routeId, setRouteId] = useState(rule?.routeId ?? "");
  const [passengerCategory, setCategory] = useState<PassengerCategory>(
    rule?.passengerCategory ?? "Adult",
  );
  const [amount, setAmount] = useState(rule ? String(rule.amount) : "");
  const [isActive, setActive] = useState(rule?.isActive ?? true);
  const mutation = rule ? update : create;
  return (
    <form
      className="space-y-5"
      onSubmit={(e) => {
        e.preventDefault();
        if (rule)
          update.mutate(
            {
              id: rule.id,
              body: { passengerCategory, amount: Number(amount), isActive },
            },
            { onSuccess: onSaved },
          );
        else
          create.mutate(
            { routeId, passengerCategory, amount: Number(amount) },
            { onSuccess: onSaved },
          );
      }}
    >
      <fieldset disabled={mutation.isPending} className="space-y-5 [&_input]:h-12">
          <div className="space-y-5">
            {rule ? (
              <p className="rounded-lg border border-primary/20 bg-primary/10 px-4 py-3 text-base font-medium text-primary">
                Route {rule.route.routeNumber} · {rule.route.name}
              </p>
            ) : (
              <div className="flex min-w-0 flex-col gap-1.5 text-sm">
                <label className="font-medium">Route</label>
                <Select value={routeId || undefined} onValueChange={(value) => setRouteId(String(value ?? ""))}>
                  <SelectTrigger className="h-12 w-full"><SelectValue placeholder="Select an active route" /></SelectTrigger>
                  <SelectContent>
                    {routes.filter((route) => route.isActive).map((route) => <SelectItem key={route.id} value={route.id}>{route.routeNumber} · {route.name}</SelectItem>)}
                  </SelectContent>
                </Select>
              </div>
            )}
            <div className="flex min-w-0 flex-col gap-1.5 text-sm">
              <label className="font-medium">Passenger category</label>
              <Select value={passengerCategory} onValueChange={(value) => setCategory(value as PassengerCategory)}>
                <SelectTrigger className="h-12 w-full"><SelectValue /></SelectTrigger>
                <SelectContent>
                  {(["Adult", "Student", "Senior", "Child"] as PassengerCategory[]).map((category) => <SelectItem key={category} value={category}>{category}</SelectItem>)}
                </SelectContent>
              </Select>
            </div>
            <Field
              label="Fare (LKR)"
              type="number"
              min="0.01"
              max="1000000"
              step="0.01"
              required
              value={amount}
              onChange={(e) => setAmount(e.target.value)}
            />
          </div>
          {rule && (
            <label htmlFor="active-fare-rule" className="flex cursor-pointer items-center gap-3 rounded-lg border border-border bg-muted/30 px-4 py-3 text-sm font-medium">
              <Checkbox
                id="active-fare-rule"
                checked={isActive}
                onCheckedChange={(checked) => setActive(checked === true)}
              />
              Active fare rule
            </label>
          )}
          <Feedback error={mutation.error} />
          <div className="flex gap-2">
            <Button type="submit">
              {mutation.isPending ? "Saving..." : "Save fare rule"}
            </Button>
            <Button type="button" variant="outline" onClick={onClose}>
              Cancel
            </Button>
          </div>
      </fieldset>
    </form>
  );
}
function EditFare({
  id,
  onClose,
  onSaved,
}: {
  id: string;
  onClose: () => void;
  onSaved: () => void;
}) {
  const query = useFareRule(id);
  return (
    <>
      <QueryState query={query} />
      {query.data && (
        <FareForm
          rule={query.data}
          routes={[]}
          onClose={onClose}
          onSaved={onSaved}
        />
      )}
    </>
  );
}
export function FareRules() {
  const { user } = useAuth();
  const centreId = user?.centreId ?? "";
  const routes = useRoutes(centreId);
  const [filtersOpen, setFiltersOpen] = useState(false);
  const [filters, setFilters] = useState({ routeId: "all", passengerCategory: "all", active: "all" });
  const [draftFilters, setDraftFilters] = useState(filters);
  const query = useFareRules({
    centreId,
    routeId: filters.routeId === "all" ? "" : filters.routeId,
    passengerCategory: filters.passengerCategory === "all" ? "" : filters.passengerCategory,
    active: filters.active === "all" ? "" : filters.active,
  });
  const { deactivate } = useFareRuleMutations();
  const [editing, setEditing] = useState("");
  const [adding, setAdding] = useState(false);
  const [confirmation, setConfirmation] = useState<FareRule | null>(null);
  const [notice, setNotice] = useState("");
  const appliedFilterCount = Object.values(filters).filter((value) => value !== "all").length;
  return (
    <div className="space-y-4">
      <Panel title="Fare rules">
        <div className="flex flex-wrap items-center justify-between gap-3 border-b pb-4">
          <div><p className="font-medium">Current centre fare directory</p><p className="mt-1 text-sm text-muted-foreground">Create and maintain fares for routes assigned to your centre.</p></div>
          <div className="flex flex-wrap gap-2">
            <Button variant="outline" className="gap-2" onClick={() => { setDraftFilters(filters); setFiltersOpen(true); }}><SlidersHorizontal className="size-4" />All filters{appliedFilterCount > 0 && <span className="rounded-full bg-primary px-1.5 text-xs text-primary-foreground">{appliedFilterCount}</span>}</Button>
            <Button disabled={!routes.data?.some((route) => route.isActive)} onClick={() => { setAdding(true); setEditing(""); }}>Create fare rule</Button>
          </div>
        </div>
        {!centreId && <p className="rounded-lg border border-amber-200 bg-amber-50 p-3 text-sm text-amber-800">Your account is not assigned to a centre, so fare rules cannot be loaded.</p>}
        {centreId && <QueryState query={routes} empty={!routes.data?.length} />}
        <Feedback error={deactivate.error} success={notice} />
        <QueryState query={query} empty={!query.data?.length} />
        {query.data && !query.error && (
          <div className="overflow-hidden rounded-xl border">
            <div className="overflow-x-auto"><table className="w-full min-w-[760px] text-left text-sm"><thead className="bg-muted/60 text-muted-foreground"><tr><th className="px-5 py-3 font-medium">Route</th><th className="px-5 py-3 font-medium">Passenger category</th><th className="px-5 py-3 font-medium">Fare</th><th className="px-5 py-3 font-medium">Status</th><th className="px-5 py-3 text-right font-medium">Actions</th></tr></thead><tbody className="divide-y">{query.data.map((rule) => <tr key={rule.id} className="bg-card transition-colors hover:bg-muted/30"><td className="px-5 py-4"><p className="font-semibold">{rule.route.routeNumber}</p><p className="mt-0.5 text-muted-foreground">{rule.route.name}</p></td><td className="px-5 py-4"><span className="rounded-full bg-secondary px-2.5 py-1 text-xs font-medium">{rule.passengerCategory}</span></td><td className="px-5 py-4 text-base font-semibold">{money(rule.amount)}</td><td className="px-5 py-4"><span className={rule.isActive ? "rounded-full bg-emerald-100 px-2.5 py-1 text-xs font-semibold text-emerald-800" : "rounded-full bg-slate-100 px-2.5 py-1 text-xs font-semibold text-slate-700"}>{rule.isActive ? "Active" : "Inactive"}</span></td><td className="px-5 py-4"><div className="flex justify-end gap-2"><Button size="sm" className="bg-primary/10 text-primary hover:bg-primary/20" variant="ghost" onClick={() => { setEditing(rule.id); setAdding(false); }}>Edit</Button>{rule.isActive && <Button size="sm" className="text-destructive hover:bg-destructive/10 hover:text-destructive" variant="ghost" onClick={() => setConfirmation(rule)}>Deactivate</Button>}</div></td></tr>)}</tbody></table></div>
          </div>
        )}
        {confirmation && (
          <div className="space-y-3 rounded-md border p-3">
            <p className="text-sm">
              Deactivate the {confirmation.passengerCategory} fare for route{" "}
              {confirmation.route.routeNumber}? Existing tickets retain their
              recorded fares.
            </p>
            <div className="flex gap-2">
              <Button
                disabled={deactivate.isPending}
                onClick={() =>
                  deactivate.mutate(confirmation.id, {
                    onSuccess: () => {
                      setConfirmation(null);
                      setNotice("Fare rule deactivated.");
                    },
                  })
                }
              >
                Confirm deactivation
              </Button>
              <Button
                variant="outline"
                disabled={deactivate.isPending}
                onClick={() => setConfirmation(null)}
              >
                Keep active
              </Button>
            </div>
          </div>
        )}
      </Panel>
      <Sheet open={filtersOpen} onOpenChange={setFiltersOpen}>
        <SheetContent side="right" className="sm:!max-w-sm">
          <SheetHeader><SheetTitle>Filter fare rules</SheetTitle><SheetDescription>Choose one or more filters, then apply them to the fare directory.</SheetDescription></SheetHeader>
          <div className="space-y-5 py-2">
            <FilterSelect label="Route" value={draftFilters.routeId} onValueChange={(value) => setDraftFilters((current) => ({ ...current, routeId: value }))} items={[{ value: "all", label: "All routes" }, ...(routes.data ?? []).map((route) => ({ value: route.id, label: `${route.routeNumber} · ${route.name}` }))]} />
            <FilterSelect label="Passenger category" value={draftFilters.passengerCategory} onValueChange={(value) => setDraftFilters((current) => ({ ...current, passengerCategory: value }))} items={[{ value: "all", label: "All categories" }, ...(["Adult", "Student", "Senior", "Child"].map((category) => ({ value: category, label: category })))]} />
            <FilterSelect label="Fare status" value={draftFilters.active} onValueChange={(value) => setDraftFilters((current) => ({ ...current, active: value }))} items={[{ value: "all", label: "All statuses" }, { value: "true", label: "Active" }, { value: "false", label: "Inactive" }]} />
          </div>
          <SheetFooter><Button variant="outline" onClick={() => setDraftFilters({ routeId: "all", passengerCategory: "all", active: "all" })}>Clear</Button><Button onClick={() => { setFilters(draftFilters); setFiltersOpen(false); }}>Apply filters</Button></SheetFooter>
        </SheetContent>
      </Sheet>
      <Dialog
        open={adding || !!editing}
        onOpenChange={(open) => {
          if (!open) {
            setAdding(false);
            setEditing("");
          }
        }}
      >
        <DialogContent className="max-h-[calc(100dvh-2rem)] overflow-y-auto p-6 sm:max-w-2xl sm:p-8">
          <DialogHeader>
            <DialogTitle>{editing ? "Edit fare rule" : "Create fare rule"}</DialogTitle>
            <DialogDescription>{editing ? "Update the fare or its active status. Changes affect future bookings only." : "Add a fare for an active route and passenger category."}</DialogDescription>
          </DialogHeader>
          {adding && <FareForm key={centreId} routes={routes.data ?? []} onClose={() => setAdding(false)} onSaved={() => { setAdding(false); setNotice("Fare rule created."); }} />}
          {editing && <EditFare key={editing} id={editing} onClose={() => setEditing("")} onSaved={() => { setEditing(""); setNotice("Fare rule updated."); }} />}
        </DialogContent>
      </Dialog>
    </div>
  );
}
