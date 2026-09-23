import { useMemo, useState, type FormEvent } from "react";
import {
  Building2,
  Copy,
  Eye,
  EyeOff,
  KeyRound,
  Loader2,
  Plus,
  Search,
  UserCheck,
  UserRound,
} from "lucide-react";
import { useSearchParams } from "react-router";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  Dialog as Sheet,
  DialogContent as SheetContent,
  DialogHeader as SheetHeader,
  DialogTitle as SheetTitle,
  DialogDescription as SheetDescription,
  DialogFooter as SheetFooter,
} from "@/components/ui/dialog";
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
  isActive: boolean;
};
const roles = ["CentreManager", "Dispatcher", "FleetOfficer", "Driver"];

export function EmployeesPage() {
  const { data: centres = [] } = useCentres();
  const {
    data: employees = [],
    isLoading,
    error,
  } = useQuery({
    queryKey: ["admin", "users"],
    queryFn: () => apiClient<Employee[]>("/api/v1/auth/users?staffOnly=true"),
  });
  const queryClient = useQueryClient();
  const createUser = useMutation({
    mutationFn: (payload: Record<string, string>) =>
      apiClient<Employee>("/api/v1/auth/create-user", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload),
      }),
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: ["admin", "users"] }),
  });
  const assignCentre = useMutation({
    mutationFn: ({ userId, centreId }: { userId: string; centreId: string }) =>
      apiClient<Employee>(`/api/v1/auth/users/${userId}/centre`, {
        method: "PATCH",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ centreId }),
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["admin", "users"] });
      setAssignment(null);
      setAssignmentCentreId("");
    },
  });
  const [params, setParams] = useSearchParams();
  const [query, setQuery] = useState("");
  const [centre, setCentre] = useState(params.get("centre") ?? "all");
  const [role, setRole] = useState("all");
  const [credentials, setCredentials] = useState<{
    email: string;
    password: string;
  } | null>(null);
  const [assignment, setAssignment] = useState<Employee | null>(null);
  const [assignmentCentreId, setAssignmentCentreId] = useState("");
  const showForm = params.get("create") === "true";
  const filtered = useMemo(
    () =>
      employees.filter(
        (item) =>
          roles.includes(item.role) &&
          `${item.name} ${item.email} ${item.id}`
            .toLowerCase()
            .includes(query.toLowerCase()) &&
          (centre === "all" || item.centreId === centre) &&
          (role === "all" || item.role === role),
      ),
    [centre, employees, query, role],
  );
  function closeForm() {
    const next = new URLSearchParams(params);
    next.delete("create");
    setParams(next);
    setCredentials(null);
  }
  function openAssignment(employee?: Employee) {
    const defaultCentreId =
      employee?.centreId ?? (centre === "all" ? centres[0]?.id : centre) ?? "";
    setAssignment(employee ?? null);
    setAssignmentCentreId(defaultCentreId);
  }
  function employeeLabel(employeeId: string) {
    const employee = employees.find((item) => item.id === employeeId);
    return employee ? `${employee.name} · ${employee.role}` : employeeId;
  }
  async function addEmployee(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    const password = String(form.get("password"));
    const email = String(form.get("email"));
    await createUser.mutateAsync({
      name: String(form.get("name")),
      email,
      password,
      role: String(form.get("role")),
      centreId: String(form.get("centreId")),
    });
    setCredentials({ email, password });
  }
  return (
    <main className="flex flex-1 flex-col gap-4 bg-muted/20 p-5">
      <PageHeading
        title="UPTSLK employees"
        description="Create and manage centre-assigned platform accounts."
        action={
          <div className="flex flex-wrap gap-2">
            <Button variant="outline" onClick={() => openAssignment()}>
              <UserCheck /> Assign existing employee
            </Button>
            <Button
              onClick={() =>
                setParams((current) => {
                  const next = new URLSearchParams(current);
                  next.set("create", "true");
                  return next;
                })
              }
            >
              <Plus /> Add employee
            </Button>
          </div>
        }
      />
      <Sheet open={showForm} onOpenChange={(open) => !open && closeForm()}>
        <SheetContent className="w-full sm:max-w-xl">
          <SheetHeader>
            <SheetTitle>Create employee account</SheetTitle>
            <SheetDescription>
              Create credentials and assign the employee to a centre.
            </SheetDescription>
          </SheetHeader>
          {credentials ? (
            <div className="mt-6 space-y-4">
              <div className="rounded-md border bg-muted/30 p-4 text-sm">
                <p className="font-medium">Account created successfully</p>
                <p className="mt-1 text-muted-foreground">
                  Share these temporary credentials securely with the employee.
                </p>
                <dl className="mt-4 grid gap-2">
                  <div>
                    <dt className="text-muted-foreground">Email</dt>
                    <dd className="font-medium">{credentials.email}</dd>
                  </div>
                  <div>
                    <dt className="text-muted-foreground">
                      Temporary password
                    </dt>
                    <dd className="font-medium">{credentials.password}</dd>
                  </div>
                </dl>
              </div>
              <Button
                variant="outline"
                className="w-full"
                onClick={() =>
                  navigator.clipboard?.writeText(
                    `UPTS login\nEmail: ${credentials.email}\nTemporary password: ${credentials.password}`,
                  )
                }
              >
                <Copy /> Copy credentials
              </Button>
              <Button
                className="w-full"
                render={
                  <a
                    href={`mailto:${credentials.email}?subject=UPTSLK account access&body=Your UPTSLK account is ready. Email: ${credentials.email}\nTemporary password: ${credentials.password}`}
                  />
                }
              >
                Share by email
              </Button>
              <Button variant="outline" className="w-full" onClick={closeForm}>
                Done
              </Button>
            </div>
          ) : (
            <form onSubmit={addEmployee} className="mt-6 grid gap-4">
              <Field
                name="name"
                label="Full name"
                placeholder="Employee name"
              />
              <Field
                name="email"
                label="Work email"
                placeholder="name@upts.lk"
                type="email"
              />
              <Field
                name="password"
                label="Temporary password"
                placeholder="At least 8 characters"
                type="password"
              />
              <p className="-mt-2 text-xs text-muted-foreground">
                Generate a strong temporary password and share it securely after
                creation.
              </p>
              <label className="grid gap-1.5 text-sm font-medium">
                Role
                <Select name="role" defaultValue={roles[0]}>
                  <SelectTrigger className="w-full">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {roles.map((item) => (
                      <SelectItem key={item} value={item}>
                        {item}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </label>
              <label className="grid gap-1.5 text-sm font-medium">
                Assigned centre
                <Select
                  name="centreId"
                  defaultValue={centres[0]?.id}
                  required
                  itemToStringLabel={(value) =>
                    centres.find((item) => item.id === value)?.name ?? value
                  }
                >
                  <SelectTrigger className="w-full">
                    <SelectValue placeholder="Select centre" />
                  </SelectTrigger>
                  <SelectContent>
                    {centres.map((item) => (
                      <SelectItem key={item.id} value={item.id}>
                        {item.name}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </label>
              {createUser.error && (
                <p className="text-sm text-destructive">
                  Could not create this account.
                </p>
              )}
              <SheetFooter>
                <Button type="button" variant="outline" onClick={closeForm}>
                  Cancel
                </Button>
                <Button type="submit" disabled={createUser.isPending}>
                  {createUser.isPending && <Loader2 className="animate-spin" />}{" "}
                  Create account
                </Button>
              </SheetFooter>
            </form>
          )}
        </SheetContent>
      </Sheet>
      <Sheet
        open={assignment !== null || assignmentCentreId !== ""}
        onOpenChange={(open) => {
          if (!open) {
            setAssignment(null);
            setAssignmentCentreId("");
          }
        }}
      >
        <SheetContent className="w-full sm:max-w-lg">
          <SheetHeader>
            <SheetTitle>Assign existing employee</SheetTitle>
            <SheetDescription>
              Select an existing staff account and assign it to a centre. This
              changes the employee’s workspace; it does not create an account.
            </SheetDescription>
          </SheetHeader>
          <div className="mt-6 grid gap-4">
            <label className="grid gap-1.5 text-sm font-medium">
              Employee
              <Select
                value={assignment?.id ?? ""}
                itemToStringLabel={employeeLabel}
                onValueChange={(value) =>
                  setAssignment(
                    employees.find((employee) => employee.id === value) ?? null,
                  )
                }
              >
                <SelectTrigger className="w-full">
                  <SelectValue placeholder="Select an existing employee" />
                </SelectTrigger>
                <SelectContent>
                  {employees.filter((employee) => employee.isActive).map((employee) => (
                    <SelectItem key={employee.id} value={employee.id}>
                      {employee.name} · {employee.role}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </label>
            <label className="grid gap-1.5 text-sm font-medium">
              Centre
              <Select
                value={assignmentCentreId}
                itemToStringLabel={(value) =>
                  centres.find((item) => item.id === value)?.name ?? value
                }
                onValueChange={(value) => setAssignmentCentreId(value ?? "")}
              >
                <SelectTrigger className="w-full">
                  <SelectValue placeholder="Select centre" />
                </SelectTrigger>
                <SelectContent>
                  {centres.map((item) => (
                    <SelectItem key={item.id} value={item.id}>
                      {item.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </label>
            {assignment && (
              <p className="rounded-md border border-primary/20 bg-primary/5 px-3 py-2 text-sm text-muted-foreground">
                {assignment.name} is currently assigned to{" "}
                {centres.find((item) => item.id === assignment.centreId)?.name ??
                  "no centre"}.
              </p>
            )}
            {assignCentre.error && (
              <p className="text-sm text-destructive">
                Could not assign this employee. Please try again.
              </p>
            )}
          </div>
          <SheetFooter className="mt-6">
            <Button
              type="button"
              variant="outline"
              onClick={() => {
                setAssignment(null);
                setAssignmentCentreId("");
              }}
            >
              Cancel
            </Button>
            <Button
              disabled={!assignment || !assignmentCentreId || assignCentre.isPending}
              onClick={() =>
                assignment &&
                assignCentre.mutate({
                  userId: assignment.id,
                  centreId: assignmentCentreId,
                })
              }
            >
              {assignCentre.isPending && <Loader2 className="animate-spin" />} Assign to centre
            </Button>
          </SheetFooter>
        </SheetContent>
      </Sheet>
      <section className="rounded-lg border bg-card p-3">
        <div className="grid gap-3 md:grid-cols-[minmax(240px,1fr)_220px_220px]">
          <div className="relative">
            <Search className="absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
            <Input
              className="pl-9"
              placeholder="Search name, email, or ID"
              value={query}
              onChange={(event) => setQuery(event.target.value)}
            />
          </div>
          <Select
            value={centre}
            itemToStringLabel={(value) =>
              value === "all"
                ? "All centres"
                : centres.find((item) => item.id === value)?.name ?? value
            }
            onValueChange={(value) => setCentre(value ?? "all")}
          >
            <SelectTrigger className="w-full bg-muted/60">
              <Building2 />
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All centres</SelectItem>
              {centres.map((item) => (
                <SelectItem key={item.id} value={item.id}>
                  {item.name}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
          <Select
            value={role}
            itemToStringLabel={(value) =>
              value === "all" ? "All roles" : value
            }
            onValueChange={(value) => setRole(value ?? "all")}
          >
            <SelectTrigger className="w-full bg-muted/60">
              <SelectValue placeholder="All roles" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All roles</SelectItem>
              {roles.map((item) => (
                <SelectItem key={item} value={item}>
                  {item}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      </section>
      <section className="overflow-hidden rounded-lg border bg-card">
        {isLoading ? (
          <div className="flex justify-center p-10">
            <Loader2 className="animate-spin" />
          </div>
        ) : error ? (
          <p className="p-8 text-center text-sm text-destructive">
            Unable to load employees. Sign in as an Admin account.
          </p>
        ) : (
          <>
            {filtered.map((item) => (
              <div
                key={item.id}
                className="grid gap-3 border-b px-4 py-4 md:grid-cols-[minmax(280px,1fr)_160px_200px_230px] md:items-center"
              >
                <div className="flex min-w-0 items-center gap-3">
                  <div className="flex size-9 shrink-0 items-center justify-center rounded-full bg-primary/10 text-primary">
                    <UserRound className="size-4" />
                  </div>
                  <div className="min-w-0">
                    <p className="font-medium">{item.name}</p>
                    <p className="truncate text-sm text-muted-foreground">
                      {item.email} · {item.id}
                    </p>
                  </div>
                </div>
                <p className="text-sm">{item.role}</p>
                <p className="text-sm text-muted-foreground">
                  {centres.find((c) => c.id === item.centreId)?.name ??
                    "Organization-wide"}
                </p>
                <div className="flex items-center gap-2">
                  <StatusBadge
                    label={item.isActive ? "Active" : "Disabled"}
                    tone={item.isActive ? "good" : "neutral"}
                  />
                  <Button
                    size="sm"
                    variant="outline"
                    onClick={() => openAssignment(item)}
                  >
                    Assign centre
                  </Button>
                </div>
              </div>
            ))}
            {filtered.length === 0 && (
              <p className="p-8 text-center text-sm text-muted-foreground">
                No employees match these filters.
              </p>
            )}
          </>
        )}
      </section>
    </main>
  );
}

function Field({
  name,
  label,
  placeholder,
  type = "text",
}: {
  name: string;
  label: string;
  placeholder: string;
  type?: string;
}) {
  const [value, setValue] = useState("");
  const [visible, setVisible] = useState(false);
  if (name === "password")
    return (
      <div className="grid gap-1.5 text-sm font-medium">
        <span>{label}</span>
        <div className="flex gap-2">
          <div className="relative flex-1">
            <Input
              name={name}
              type={visible ? "text" : "password"}
              placeholder={placeholder}
              value={value}
              onChange={(event) => setValue(event.target.value)}
              minLength={8}
              required
            />
            <button
              type="button"
              className="absolute right-2 top-1/2 -translate-y-1/2 text-muted-foreground"
              onClick={() => setVisible((current) => !current)}
              aria-label={visible ? "Hide password" : "Show password"}
            >
              {visible ? (
                <EyeOff className="size-4" />
              ) : (
                <Eye className="size-4" />
              )}
            </button>
          </div>
          <Button
            type="button"
            variant="outline"
            className="shrink-0 text-cyan-700"
            onClick={() =>
              setValue(`Upts-${Math.floor(1000 + Math.random() * 9000)}!`)
            }
          >
            <KeyRound className="size-4" /> Generate
          </Button>
        </div>
      </div>
    );
  return (
    <label className="grid gap-1.5 text-sm font-medium">
      {label}
      <Input name={name} type={type} placeholder={placeholder} required />
    </label>
  );
}
