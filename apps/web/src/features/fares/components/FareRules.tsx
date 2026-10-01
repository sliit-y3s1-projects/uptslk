import { useState } from "react";
import { Plus } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import {
  AlertDialog,
  AlertDialogCancel,
  AlertDialogAction,
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
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { PageHeading } from "@/components/shared/PageHeading";
import { StatusBadge } from "@/components/shared/StatusBadge";
import { LabeledSelect } from "@/components/shared/LabeledSelect";
import {
  Field,
  QueryState,
  Feedback,
} from "@/features/riders/components/FeatureUi";
import { money } from "@/features/riders/components/format";
import type { FareRule, RouteOption } from "../types/fares";
import {
  useFareRules,
  useFareRule,
  useFareRuleMutations,
} from "../hooks/useFareRules";
import { useRoutes } from "../hooks/useBookings";
import { useAuth } from "@/hooks/useAuth";

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
              body: { amount: Number(amount), isActive },
            },
            { onSuccess: onSaved },
          );
        else
          create.mutate(
            { routeId, amount: Number(amount) },
            { onSuccess: onSaved },
          );
      }}
    >
      <fieldset disabled={mutation.isPending} className="space-y-5">
        <div className="space-y-5">
          {rule ? (
            <p className="rounded-lg border bg-muted/40 px-4 py-3 text-sm font-medium">
              Route {rule.route.routeNumber} · {rule.route.name}
            </p>
          ) : (
            <div className="flex min-w-0 flex-col gap-1.5 text-sm">
              <label className="font-medium">Route</label>
              <Select
                value={routeId || undefined}
                onValueChange={(value) => setRouteId(String(value ?? ""))}
                itemToStringLabel={(value) => {
                  const route = routes.find((item) => item.id === value);
                  return route ? `${route.routeNumber} · ${route.name}` : value;
                }}
              >
                <SelectTrigger className="w-full">
                  <SelectValue placeholder="Select an active route" />
                </SelectTrigger>
                <SelectContent>
                  {routes
                    .filter((route) => route.isActive)
                    .map((route) => (
                      <SelectItem key={route.id} value={route.id}>
                        {route.routeNumber} · {route.name}
                      </SelectItem>
                    ))}
                </SelectContent>
              </Select>
            </div>
          )}
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
          <label
            htmlFor="active-fare-rule"
            className="flex cursor-pointer items-center gap-3 rounded-lg border border-border bg-muted/30 px-4 py-3 text-sm font-medium"
          >
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
export function FareRulesPage() {
  const { user } = useAuth();
  const centreId = user?.centreId ?? "";
  const routes = useRoutes(centreId);
  const [routeId, setRouteId] = useState("all");
  const [active, setActive] = useState("all");
  const query = useFareRules({
    centreId,
    routeId: routeId === "all" ? "" : routeId,
    active: active === "all" ? "" : active,
  });
  const { deactivate } = useFareRuleMutations();
  const [editing, setEditing] = useState("");
  const [adding, setAdding] = useState(false);
  const [confirmation, setConfirmation] = useState<FareRule | null>(null);
  const [notice, setNotice] = useState("");
  const filtered = routeId !== "all" || active !== "all";
  const canCreate = !!routes.data?.some((route) => route.isActive);
  const rules = query.data ?? [];

  return (
    <div className="flex flex-1 flex-col gap-4 bg-muted/20 p-4">
      <PageHeading
        title="Fare rules"
        description="One standard fare per route."
        action={
          <Button
            disabled={!canCreate}
            onClick={() => {
              setAdding(true);
              setEditing("");
            }}
          >
            <Plus /> Create fare rule
          </Button>
        }
      />

      {!centreId && (
        <p className="rounded-lg border border-amber-200 bg-amber-50 p-3 text-sm text-amber-800">
          Your account is not assigned to a centre, so fare rules cannot be
          loaded.
        </p>
      )}
      {centreId && <QueryState query={routes} />}
      <Feedback success={notice} />

      <section className="overflow-hidden rounded-lg border bg-card">
        <div className="flex flex-col gap-3 border-b p-3 sm:flex-row sm:items-end">
          <div className="grid flex-1 gap-3 sm:max-w-xl sm:grid-cols-2">
            <LabeledSelect
              label="Route"
              value={routeId}
              onChange={setRouteId}
              options={[
                { value: "all", label: "All routes" },
                ...(routes.data ?? []).map((route) => ({
                  value: route.id,
                  label: `${route.routeNumber} · ${route.name}`,
                })),
              ]}
            />
            <LabeledSelect
              label="Status"
              value={active}
              onChange={setActive}
              options={[
                { value: "all", label: "All statuses" },
                { value: "true", label: "Active" },
                { value: "false", label: "Inactive" },
              ]}
            />
          </div>
          {filtered && (
            <Button
              variant="ghost"
              onClick={() => {
                setRouteId("all");
                setActive("all");
              }}
            >
              Clear filters
            </Button>
          )}
          <p className="text-sm text-muted-foreground sm:ml-auto">
            {rules.length} fare{rules.length === 1 ? "" : "s"}
          </p>
        </div>

        {query.isPending && centreId ? (
          <p className="p-6 text-sm text-muted-foreground">Loading fares...</p>
        ) : query.error ? (
          <div className="p-4">
            <QueryState query={query} />
          </div>
        ) : rules.length === 0 ? (
          <p className="p-8 text-center text-sm text-muted-foreground">
            {filtered
              ? "No fares match these filters."
              : "No fares have been set for this centre yet."}
          </p>
        ) : (
          <Table>
            <TableHeader>
              <TableRow className="bg-muted/40 hover:bg-muted/40">
                <TableHead className="px-4">Route</TableHead>
                <TableHead>Fare per passenger</TableHead>
                <TableHead>Status</TableHead>
                <TableHead className="px-4 text-right">Actions</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {rules.map((rule) => (
                <TableRow key={rule.id}>
                  <TableCell className="px-4">
                    <p className="font-medium">{rule.route.routeNumber}</p>
                    <p className="text-xs text-muted-foreground">
                      {rule.route.name}
                    </p>
                  </TableCell>
                  <TableCell className="font-medium tabular-nums">
                    {money(rule.amount)}
                  </TableCell>
                  <TableCell>
                    <StatusBadge
                      label={rule.isActive ? "Active" : "Inactive"}
                      tone={rule.isActive ? "good" : "neutral"}
                    />
                  </TableCell>
                  <TableCell className="px-4">
                    <div className="flex justify-end gap-2">
                      <Button
                        size="sm"
                        variant="outline"
                        onClick={() => {
                          setEditing(rule.id);
                          setAdding(false);
                        }}
                      >
                        Edit
                      </Button>
                      {rule.isActive && (
                        <Button
                          size="sm"
                          variant="outline"
                          className="text-destructive"
                          onClick={() => setConfirmation(rule)}
                        >
                          Deactivate
                        </Button>
                      )}
                    </div>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}
      </section>

      <AlertDialog
        open={!!confirmation}
        onOpenChange={(open) => {
          if (!open) {
            setConfirmation(null);
            deactivate.reset();
          }
        }}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Deactivate this fare?</AlertDialogTitle>
            <AlertDialogDescription>
              Route {confirmation?.route.routeNumber} will no longer accept new
              bookings until a fare is active again. Existing tickets keep their
              recorded fares.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <Feedback error={deactivate.error} />
          <AlertDialogFooter>
            <AlertDialogCancel disabled={deactivate.isPending}>
              Keep active
            </AlertDialogCancel>
            <AlertDialogAction
              className="bg-destructive text-white hover:bg-destructive/90"
              disabled={deactivate.isPending}
              onClick={(event) => {
                event.preventDefault();
                if (!confirmation) return;
                deactivate.mutate(confirmation.id, {
                  onSuccess: () => {
                    setConfirmation(null);
                    setNotice("Fare rule deactivated.");
                  },
                });
              }}
            >
              Deactivate
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>

      <Dialog
        open={adding || !!editing}
        onOpenChange={(open) => {
          if (!open) {
            setAdding(false);
            setEditing("");
          }
        }}
      >
        <DialogContent className="max-h-[calc(100dvh-2rem)] overflow-y-auto sm:max-w-md">
          <DialogHeader>
            <DialogTitle>
              {editing ? "Edit fare rule" : "Create fare rule"}
            </DialogTitle>
            <DialogDescription>
              {editing
                ? "Changes affect future bookings only."
                : "Set the standard fare for an active route."}
            </DialogDescription>
          </DialogHeader>
          {adding && (
            <FareForm
              key={centreId}
              routes={routes.data ?? []}
              onClose={() => setAdding(false)}
              onSaved={() => {
                setAdding(false);
                setNotice("Fare rule created.");
              }}
            />
          )}
          {editing && (
            <EditFare
              key={editing}
              id={editing}
              onClose={() => setEditing("")}
              onSaved={() => {
                setEditing("");
                setNotice("Fare rule updated.");
              }}
            />
          )}
        </DialogContent>
      </Dialog>
    </div>
  );
}
