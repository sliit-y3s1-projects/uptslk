import {
  Building2,
  ChevronRight,
  ShieldAlert,
  UserPlus,
  UsersRound,
} from "lucide-react";
import { Link } from "react-router";
import { useQuery } from "@tanstack/react-query";
import { Button } from "@/components/ui/button";
import { PageHeading } from "@/components/shared/PageHeading";
import { StatusBadge } from "@/components/shared/StatusBadge";
import { useCentres } from "@/features/centres/hooks/useCentres";
import { apiClient } from "@/lib/api/api-client";

type Employee = {
  id: string;
  name: string;
  email: string;
  role: string;
  centreId?: string | null;
};

export function SuperAdminOverviewPage() {
  const { data: centres = [], isLoading: centresLoading } = useCentres();
  const { data: employees = [], isLoading: employeesLoading } = useQuery({
    queryKey: ["admin", "users"],
    queryFn: () => apiClient<Employee[]>("/api/v1/auth/users"),
  });
  const operating = centres.filter((item) => item.status === "Operating");
  return (
    <main className="flex flex-1 flex-col gap-5 bg-muted/20 p-5">
      <PageHeading
        title="Organization overview"
        description="Govern centres, people, and access across UPTSLK."
        action={
          <div className="flex gap-2">
            <Button
              variant="outline"
              render={<Link to="/admin/employees?create=true" />}
            >
              <UserPlus /> Add employee
            </Button>
            <Button render={<Link to="/admin/centres/new" />}>
              <Building2 /> Create centre
            </Button>
          </div>
        }
      />
      <section className="grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
        <Metric
          icon={Building2}
          label="Operating centres"
          value={centresLoading ? "—" : String(operating.length)}
          detail={
            centresLoading
              ? "Loading"
              : `${centres.length - operating.length} planned or closed`
          }
        />
        <Metric
          icon={UsersRound}
          label="UPTSLK employees"
          value={employeesLoading ? "—" : String(employees.length)}
          detail="Accounts in Identity"
        />
        <Metric
          icon={ShieldAlert}
          label="Access requests"
          value="—"
          detail="No access-request API"
        />
      </section>
      <section className="grid gap-4 xl:grid-cols-[minmax(0,1.2fr)_380px]">
        <article className="overflow-hidden rounded-lg border bg-card">
          <div className="flex items-center justify-between border-b bg-muted/30 px-4 py-3">
            <div>
              <h2 className="font-semibold">Centre portfolio</h2>
              <p className="text-sm text-muted-foreground">
                Live centres returned by the API
              </p>
            </div>
            <Button
              size="sm"
              variant="outline"
              render={<Link to="/admin/centres" />}
            >
              View all
            </Button>
          </div>
          <div className="divide-y">
            {centres.length === 0 && (
              <p className="p-8 text-center text-sm text-muted-foreground">
                No centres have been created yet.
              </p>
            )}
            {centres.map((centre) => (
              <Link
                key={centre.id}
                to={`/admin/centres/${centre.id}`}
                className="grid gap-3 px-4 py-3 sm:grid-cols-[minmax(0,1fr)_220px_120px_24px] sm:items-center"
              >
                <div>
                  <p className="font-medium">{centre.name}</p>
                  <p className="text-sm text-muted-foreground">
                    {centre.city}, {centre.district} · {centre.bayCount} bays
                  </p>
                </div>
                <p className="text-sm text-muted-foreground">
                  Centre manager assigned through Employees
                </p>
                <StatusBadge
                  label={centre.status}
                  tone={centre.status === "Operating" ? "good" : "neutral"}
                />
                <ChevronRight className="size-4 text-muted-foreground" />
              </Link>
            ))}
          </div>
        </article>
        <article className="overflow-hidden rounded-lg border bg-card">
          <div className="border-b bg-muted/30 px-4 py-3">
            <h2 className="font-semibold">Recent administration</h2>
            <p className="text-sm text-muted-foreground">
              Live audit events will appear here
            </p>
          </div>
          <p className="p-8 text-center text-sm text-muted-foreground">
            No administration events available.
          </p>
        </article>
      </section>
    </main>
  );
}

function Metric({
  icon: Icon,
  label,
  value,
  detail,
}: {
  icon: typeof Activity;
  label: string;
  value: string;
  detail: string;
}) {
  return (
    <article className="rounded-lg border bg-card p-4">
      <div className="flex items-center justify-between">
        <p className="text-sm text-muted-foreground">{label}</p>
        <Icon className="size-4 text-primary" />
      </div>
      <p className="mt-3 text-2xl font-semibold">{value}</p>
      <p className="mt-1 text-xs text-muted-foreground">{detail}</p>
    </article>
  );
}
