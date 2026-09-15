import { useCentreSelection } from "@/features/fares/hooks/useCentreSelection";
import { useState } from "react";
import { Button } from "@/components/ui/button";
import {
  Panel,
  Field,
  SelectField,
  CategoryOptions,
  ActiveOptions,
  QueryState,
  DataTable,
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
import { CentrePicker } from "./CentrePicker";

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
    <Panel title={rule ? "Edit fare rule" : "Create fare rule"}>
      <form
        className="space-y-4"
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
        <fieldset disabled={mutation.isPending} className="space-y-4">
          <div className="grid gap-3 sm:grid-cols-3">
            {rule ? (
              <p className="text-sm">
                Route {rule.route.routeNumber} · {rule.route.name}
              </p>
            ) : (
              <SelectField
                label="Route"
                required
                value={routeId}
                onChange={(e) => setRouteId(e.target.value)}
              >
                <option value="">Select an active route</option>
                {routes
                  .filter((r) => r.isActive)
                  .map((r) => (
                    <option key={r.id} value={r.id}>
                      {r.routeNumber} · {r.name}
                    </option>
                  ))}
              </SelectField>
            )}
            <SelectField
              label="Passenger category"
              value={passengerCategory}
              onChange={(e) => setCategory(e.target.value as PassengerCategory)}
            >
              <CategoryOptions />
            </SelectField>
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
            <label className="flex items-center gap-2 text-sm">
              <input
                type="checkbox"
                checked={isActive}
                onChange={(e) => setActive(e.target.checked)}
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
    </Panel>
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
  const [selectedCentre, setCentre] = useState("");
  const { centreId } = useCentreSelection(selectedCentre);
  const routes = useRoutes(centreId);
  const [routeId, setRoute] = useState("");
  const [passengerCategory, setCategory] = useState("");
  const [active, setActive] = useState("");
  const query = useFareRules({ centreId, routeId, passengerCategory, active });
  const { deactivate } = useFareRuleMutations();
  const [editing, setEditing] = useState("");
  const [adding, setAdding] = useState(false);
  const [confirmation, setConfirmation] = useState<FareRule | null>(null);
  const [notice, setNotice] = useState("");
  return (
    <div className="space-y-4">
      <Panel title="Fare rules">
        <CentrePicker
          selected={selectedCentre}
          onChange={(id) => {
            setCentre(id);
            setRoute("");
            setAdding(false);
          }}
        />
        <div className="grid gap-3 sm:grid-cols-3">
          <SelectField
            label="Route filter"
            value={routeId}
            onChange={(e) => setRoute(e.target.value)}
          >
            <option value="">All routes</option>
            {routes.data?.map((r) => (
              <option key={r.id} value={r.id}>
                {r.routeNumber} · {r.name}
              </option>
            ))}
          </SelectField>
          <SelectField
            label="Category filter"
            value={passengerCategory}
            onChange={(e) => setCategory(e.target.value)}
          >
            <option value="">All categories</option>
            <CategoryOptions />
          </SelectField>
          <SelectField
            label="Fare status"
            value={active}
            onChange={(e) => setActive(e.target.value)}
          >
            <ActiveOptions />
          </SelectField>
        </div>
        {centreId && <QueryState query={routes} empty={!routes.data?.length} />}
        <Button
          disabled={!routes.data?.some((r) => r.isActive)}
          onClick={() => {
            setAdding(true);
            setEditing("");
          }}
        >
          Create fare rule
        </Button>
        <Feedback error={deactivate.error} success={notice} />
        <QueryState query={query} empty={!query.data?.length} />
        {query.data && !query.error && (
          <DataTable
            headings={["Route", "Category", "Fare", "Status", "Actions"]}
          >
            {query.data.map((r) => (
              <tr key={r.id}>
                <td>
                  <p className="font-medium">{r.route.routeNumber}</p>
                  <p className="text-muted-foreground">{r.route.name}</p>
                </td>
                <td>{r.passengerCategory}</td>
                <td>{money(r.amount)}</td>
                <td>{r.isActive ? "Active" : "Inactive"}</td>
                <td>
                  <div className="flex gap-2">
                    <Button
                      size="sm"
                      variant="outline"
                      onClick={() => {
                        setEditing(r.id);
                        setAdding(false);
                      }}
                    >
                      Edit
                    </Button>
                    {r.isActive && (
                      <Button
                        size="sm"
                        variant="ghost"
                        onClick={() => setConfirmation(r)}
                      >
                        Deactivate
                      </Button>
                    )}
                  </div>
                </td>
              </tr>
            ))}
          </DataTable>
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
    </div>
  );
}
