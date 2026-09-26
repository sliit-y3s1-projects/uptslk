import { useState } from "react";
import { useSearchParams } from "react-router";
import { Button } from "@/components/ui/button";
import { PageHeading } from "@/components/shared/PageHeading";
import { usePassengers } from "./hooks/usePassengers";
import { PassengerProfile, PassengerForm } from "./components/PassengerProfile";
import {
  Panel,
  Field,
  SelectField,
  CategoryOptions,
  ActiveOptions,
  QueryState,
  DataTable,
  Feedback,
} from "./components/FeatureUi";
import { money } from "./components/format";

export function PassengerWorkspace({
  title,
  description,
}: {
  title: string;
  description: string;
}) {
  const [params, setParams] = useSearchParams();
  const id = params.get("passengerId") ?? "";
  const [search, setSearch] = useState("");
  const [category, setCategory] = useState("");
  const [active, setActive] = useState("");
  const [adding, setAdding] = useState(false);
  const [notice, setNotice] = useState("");
  const query = usePassengers({ search, category, active });
  return (
    <main className="flex flex-1 flex-col gap-4 bg-muted/20 p-4">
      <PageHeading
        title={title}
        description={description}
        action={<Button onClick={() => setAdding(true)}>Add passenger</Button>}
      />
      <Feedback success={notice} />
      {adding && (
        <PassengerForm
          onSaved={(newId) => {
            setAdding(false);
            setParams({ passengerId: newId });
            setNotice("Passenger created.");
          }}
          onClose={() => setAdding(false)}
        />
      )}
      <Panel title="Passenger accounts">
        <div className="grid gap-3 sm:grid-cols-[2fr_1fr_1fr]">
          <Field
            label="Search name, phone or email"
            type="search"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
          <SelectField
            label="Category"
            value={category}
            onChange={(e) => setCategory(e.target.value)}
          >
            <option value="">All categories</option>
            <CategoryOptions />
          </SelectField>
          <SelectField
            label="Account status"
            value={active}
            onChange={(e) => setActive(e.target.value)}
          >
            <ActiveOptions />
          </SelectField>
        </div>
        <QueryState query={query} empty={!query.data?.length} />
        {query.data && !query.error && (
          <DataTable
            headings={[
              "Passenger",
              "Phone",
              "Category",
              "Wallet",
              "Status",
              "Profile",
            ]}
          >
            {query.data.map((p) => (
              <tr key={p.id} className={id === p.id ? "bg-muted/40" : ""}>
                <td className="font-medium">{p.fullName}</td>
                <td>{p.phoneNumber}</td>
                <td>{p.category}</td>
                <td className="tabular-nums">{money(p.balance)}</td>
                <td>{p.isActive ? "Active" : "Inactive"}</td>
                <td>
                  <Button
                    size="sm"
                    variant="outline"
                    onClick={() => setParams({ passengerId: p.id })}
                  >
                    Open profile
                  </Button>
                </td>
              </tr>
            ))}
          </DataTable>
        )}
      </Panel>
      {id && (
        <>
          <div>
            <Button variant="ghost" onClick={() => setParams({})}>
              Close profile
            </Button>
          </div>
          <PassengerProfile key={id} id={id} />
        </>
      )}
    </main>
  );
}
export function RidersPage() {
  return (
    <PassengerWorkspace
      title="Passenger accounts"
      description="Manage passenger profiles, bookings and wallet balances."
    />
  );
}
