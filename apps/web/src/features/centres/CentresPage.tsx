import {
  ArrowLeft,
  Building2,
  BusFront,
  ChevronRight,
  Eye,
  Plus,
  Route as RouteIcon,
  TrainFront,
  UsersRound,
  Loader2,
} from "lucide-react";
import { Link, useNavigate, useParams } from "react-router";
import { useQuery } from "@tanstack/react-query";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { PageHeading } from "@/components/shared/PageHeading";
import { StatusBadge } from "@/components/shared/StatusBadge";
import {
  useCentres,
  useCentre,
  useCreateCentre,
  useUpdateCentre,
} from "./hooks/useCentres";
import type { CentreStatus } from "./types";
import { apiClient } from "@/lib/api/api-client";

type CentreEmployee = {
  id: string;
  name: string;
  role: string;
  centreId?: string | null;
};

function useCentreEmployees() {
  return useQuery({
    queryKey: ["admin", "users"],
    queryFn: () =>
      apiClient<CentreEmployee[]>("/api/v1/auth/users?staffOnly=true"),
  });
}

export function CentresPage() {
  const { data: centres, isLoading, error } = useCentres();
  const { data: employees = [] } = useCentreEmployees();

  if (isLoading)
    return (
      <main className="p-5 flex justify-center">
        <Loader2 className="animate-spin" />
      </main>
    );
  if (error)
    return <main className="p-5 text-red-500">Failed to load centres.</main>;

  return (
    <main className="flex flex-1 flex-col gap-4 bg-muted/20 p-5">
      <PageHeading
        title="Multimodal centres"
        description="Create and govern every terminal in the UPTSLK organization."
        action={
          <Button render={<Link to="/admin/centres/new" />}>
            <Plus /> Create centre
          </Button>
        }
      />
      <section className="overflow-hidden rounded-lg border bg-card">
        <div className="grid gap-3 border-b px-4 py-3 text-xs font-medium uppercase text-muted-foreground md:grid-cols-[minmax(0,1.2fr)_140px_180px_100px_24px]">
          <span>Centre</span>
          <span>Status</span>
          <span>Centre manager</span>
          <span>Employees</span>
          <span />
        </div>
        {centres?.length === 0 ? (
          <div className="p-10 text-center text-sm text-muted-foreground">
            No centres found. Create one to get started.
          </div>
        ) : (
          centres?.map((centre) => {
            const centreEmployees = employees.filter(
              (employee) => employee.centreId === centre.id,
            );
            const manager = centreEmployees.find(
              (employee) => employee.role === "CentreManager",
            );
            return (
              <Link
                key={centre.id}
                to={`/admin/centres/${centre.id}`}
                className="grid gap-3 border-b px-4 py-4 hover:bg-muted/40 md:grid-cols-[minmax(0,1.2fr)_140px_180px_100px_24px] md:items-center"
              >
                <div className="flex items-center gap-3">
                  <div className="flex size-9 items-center justify-center rounded-md bg-primary/10 text-primary">
                    <Building2 className="size-4" />
                  </div>
                  <div>
                    <p className="font-medium">{centre.name}</p>
                    <p className="text-sm text-muted-foreground">
                      {centre.city} - {centre.bayCount} bays
                    </p>
                  </div>
                </div>
                <StatusBadge
                  label={centre.status}
                  tone={centre.status === "Operating" ? "good" : "neutral"}
                />
                <p className="text-sm text-muted-foreground">
                  {manager?.name ?? "Unassigned"}
                </p>
                <p className="text-sm font-medium">{centreEmployees.length}</p>
                <ChevronRight className="size-4 text-muted-foreground" />
              </Link>
            );
          })
        )}
      </section>
    </main>
  );
}

export function CentreProfilePage() {
  const { centreId } = useParams();
  const { data: centre, isLoading, error } = useCentre(centreId);
  const { data: employees = [] } = useCentreEmployees();

  if (isLoading)
    return (
      <main className="p-5 flex justify-center">
        <Loader2 className="animate-spin" />
      </main>
    );
  if (error)
    return <main className="p-5 text-red-500">Failed to load centre.</main>;
  if (!centre) return <main className="p-5">Centre not found.</main>;

  const centreEmployees = employees.filter(
    (employee) => employee.centreId === centre.id,
  );
  const manager = centreEmployees.find(
    (employee) => employee.role === "CentreManager",
  );

  return (
    <main className="flex flex-1 flex-col gap-4 bg-muted/20 p-5">
      <PageHeading
        title={centre.name}
        description={`${centre.city}, ${centre.district} District`}
        action={
          <div className="flex gap-2">
            <Button
              variant="outline"
              render={<Link to={`/admin/centres/${centre.id}/edit`} />}
            >
              Edit centre
            </Button>
            {centre.status === "Operating" && (
              <Button
                render={<Link to={`/admin/centres/${centre.id}/operations`} />}
              >
                <Eye /> View operations
              </Button>
            )}
          </div>
        }
      />
      <section className="grid gap-4 xl:grid-cols-[minmax(0,1fr)_340px]">
        <article className="rounded-lg border bg-card p-5">
          <div className="flex items-start justify-between">
            <div className="flex size-11 items-center justify-center rounded-md bg-primary/10 text-primary">
              <Building2 />
            </div>
            <StatusBadge
              label={centre.status}
              tone={centre.status === "Operating" ? "good" : "neutral"}
            />
          </div>
          <h2 className="mt-5 font-semibold">Centre details</h2>
          <div className="mt-4 grid gap-4 sm:grid-cols-2">
            <Fact label="Centre code" value={centre.code.toUpperCase()} />
            <Fact label="District" value={centre.district} />
            <Fact label="Bus bays" value={String(centre.bays.length)} />
            <Fact
              label="Connected modes"
              value="Bus - Rail - Taxi - Park & Ride"
            />
            <Fact
              label="Centre manager"
              value={manager?.name ?? "Unassigned"}
            />
            <Fact label="Employees" value={String(centreEmployees.length)} />
          </div>
          <p className="mt-5 border-t pt-4 text-sm leading-6 text-muted-foreground">
            {centre.description || "No description provided."}
          </p>
        </article>
        <article className="rounded-lg border bg-card p-5">
          <h2 className="font-semibold">Governance readiness</h2>
          <div className="mt-4 space-y-3">
            <Check label="Centre manager assigned" complete={!!manager} />
            <Check
              label="Emergency contacts configured"
              complete={centre.status === "Operating"}
            />
            <Check
              label="Transport integrations connected"
              complete={centre.status === "Operating"}
            />
            <Check
              label="Employee roles reviewed"
              complete={centreEmployees.length > 0}
            />
          </div>
          <Button
            className="mt-5 w-full"
            variant="outline"
            render={<Link to={`/admin/employees?centre=${centre.id}`} />}
          >
            <UsersRound /> Manage employees
          </Button>
        </article>
      </section>
      <section>
        <h2 className="mb-3 font-semibold">Operational records</h2>
        <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
          <Summary
            icon={Eye}
            label="Live operations"
            value="Bays, departures & capacity"
            href={`/admin/centres/${centre.id}/operations`}
          />
          <Summary
            icon={RouteIcon}
            label="Routes"
            value="Service corridors & stops"
            href={`/admin/centres/${centre.id}/routes`}
          />
          <Summary
            icon={BusFront}
            label="Fleet"
            value="Vehicles & readiness"
            href={`/admin/centres/${centre.id}/vehicles`}
          />
          <Summary
            icon={UsersRound}
            label="Workforce"
            value="Employee service"
            href={`/admin/employees?centre=${centre.id}`}
          />
        </div>
      </section>
    </main>
  );
}

export function CentreFormPage() {
  const { centreId } = useParams();
  const navigate = useNavigate();
  const {
    data: existing,
    isLoading: isLoadingExisting,
    error: existingError,
  } = useCentre(centreId);
  const createMutation = useCreateCentre();
  const updateMutation = useUpdateCentre(centreId!);

  if (centreId && isLoadingExisting)
    return (
      <main className="p-5 flex justify-center">
        <Loader2 className="animate-spin" />
      </main>
    );
  if (centreId && existingError)
    return (
      <main className="p-5 text-red-500">
        Failed to load centre for editing.
      </main>
    );
  if (centreId && !existing)
    return <main className="p-5">Centre not found.</main>;

  return (
    <main className="flex flex-1 flex-col gap-4 bg-muted/20 p-5">
      <PageHeading
        title={existing ? `Edit ${existing.name}` : "Create multimodal centre"}
        description={
          existing
            ? "Update its core information and operating status."
            : "Add a centre to the UPTSLK network."
        }
        action={
          <Button
            variant="outline"
            render={<Link to="/admin/centres" />}
          >
            <ArrowLeft /> All centres
          </Button>
        }
      />
      <form
        className="w-full overflow-hidden rounded-xl border border-slate-300 bg-card"
        onSubmit={(event) => {
          event.preventDefault();
          const form = new FormData(event.currentTarget);
          const payload = {
            code: String(form.get("code") || ""),
            name: String(form.get("name") || ""),
            city: String(form.get("city") || ""),
            district: String(form.get("district") || ""),
            description: String(form.get("description") || ""),
            status: String(form.get("status")) as CentreStatus,
          };

          if (existing) {
            updateMutation.mutate(payload, {
              onSuccess: () => {
                console.log("Success");
                navigate(`/admin/centres/${existing.id}`);
              },
              onError: () => console.error("Error"),
            });
          } else {
            createMutation.mutate(payload, {
              onSuccess: (data) => {
                console.log("Success");
                navigate(`/admin/centres/${data.id}`);
              },
              onError: () => console.error("Error"),
            });
          }
        }}
      >
        <div className="grid gap-x-5 gap-y-5 p-6 md:grid-cols-2 xl:grid-cols-12">
          <div className="xl:col-span-3">
            <Field
              name="code"
              label="Centre code"
              value={existing?.code}
              placeholder="MAK"
              disabled={!!existing}
            />
          </div>
          <div className="xl:col-span-5">
            <Field
              name="name"
              label="Centre name"
              value={existing?.name}
              placeholder="Makumbura MMC"
            />
          </div>
          <label className="grid gap-1.5 text-sm font-medium md:col-span-2 xl:col-span-4">
            Operational status
            <Select name="status" defaultValue={existing?.status ?? "Planned"}>
              <SelectTrigger className="h-11 w-full bg-background px-3">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="Planned">Planned</SelectItem>
                <SelectItem value="Operating">Operating</SelectItem>
                <SelectItem value="Closed">Closed</SelectItem>
                <SelectItem value="Suspended">Suspended</SelectItem>
              </SelectContent>
            </Select>
          </label>
          <div className="xl:col-span-4">
            <Field
              name="city"
              label="City"
              value={existing?.city}
              placeholder="Kottawa"
            />
          </div>
          <div className="xl:col-span-4">
            <Field
              name="district"
              label="District"
              value={existing?.district}
              placeholder="Colombo"
            />
          </div>
          <div className="md:col-span-2 xl:col-span-4">
            <Field
              name="description"
              label="Description (optional)"
              value={existing?.description || ""}
              placeholder="Terminal purpose or connected modes"
              required={false}
            />
          </div>
        </div>
        <div className="flex flex-wrap items-center justify-end gap-2 border-t border-slate-200 bg-slate-50/70 px-5 py-4 sm:px-6">
          {(createMutation.isError || updateMutation.isError) && (
            <span className="mr-auto text-sm text-red-600">
              Failed to save centre. Please try again.
            </span>
          )}
          <Button type="button" variant="outline" onClick={() => navigate(-1)}>
            Cancel
          </Button>
          <Button
            type="submit"
            disabled={createMutation.isPending || updateMutation.isPending}
          >
            {createMutation.isPending || updateMutation.isPending ? (
              <Loader2 className="animate-spin size-4" />
            ) : existing ? (
              "Save changes"
            ) : (
              "Create centre"
            )}
          </Button>
        </div>
      </form>
    </main>
  );
}

function Field({
  name,
  label,
  value,
  placeholder,
  type,
  disabled,
  required = true,
}: {
  name: string;
  label: string;
  value?: string;
  placeholder: string;
  type?: string;
  disabled?: boolean;
  required?: boolean;
}) {
  return (
    <label className="grid gap-1.5 text-sm font-medium">
      {label}
      <Input
        name={name}
        defaultValue={value}
        placeholder={placeholder}
        type={type}
        required={required}
        disabled={disabled}
        className="h-11 px-3"
      />
    </label>
  );
}
function Fact({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <p className="text-xs text-muted-foreground">{label}</p>
      <p className="mt-1 text-sm font-medium">{value}</p>
    </div>
  );
}
function Check({ label, complete }: { label: string; complete: boolean }) {
  return (
    <div className="flex items-center justify-between rounded-md border p-3 text-sm">
      <span>{label}</span>
      <StatusBadge
        label={complete ? "Complete" : "Required"}
        tone={complete ? "good" : "warning"}
      />
    </div>
  );
}
function Summary({
  icon: Icon,
  label,
  value,
  href,
}: {
  icon: typeof TrainFront;
  label: string;
  value: string;
  href: string;
}) {
  return (
    <Link
      to={href}
      className="group rounded-lg border bg-card p-4 transition hover:border-primary/30 hover:shadow-sm"
    >
      <div className="flex items-center justify-between">
        <Icon className="size-4 text-primary" />
        <ChevronRight className="size-4 text-muted-foreground transition group-hover:translate-x-0.5" />
      </div>
      <p className="mt-3 text-sm text-muted-foreground">{label}</p>
      <p className="mt-1 font-semibold">{value}</p>
    </Link>
  );
}
