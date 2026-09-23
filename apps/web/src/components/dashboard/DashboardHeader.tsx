import {
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbList,
  BreadcrumbPage,
  BreadcrumbSeparator,
} from "@/components/ui/breadcrumb";
import { Separator } from "@/components/ui/separator";
import { SidebarTrigger } from "@/components/ui/sidebar";
import { Button } from "@/components/ui/button";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { useOperationalScope } from "@/context/OperationalScopeContext";
import { useAuth } from "@/hooks/useAuth";
import { useLocation } from "react-router";

const labels: Record<string, string> = {
  operations: "Operations",
  dispatch: "Dispatch",
  history: "Trip History",
  bays: "Bay Management",
  incidents: "Incidents",
  approvals: "Approvals",
  new: "Create",
  network: "Network",
  routes: "Routes",
  timetables: "Timetables",
  stops: "Stops",
  centres: "Centres",
  fleet: "Fleet",
  vehicles: "Vehicles",
  drivers: "Drivers",
  maintenance: "Maintenance",
  passengers: "Passengers",
  flow: "Passenger Flow",
  assistance: "Assistance",
  riders: "Riders",
  accounts: "Accounts",
  support: "Support",
  fares: "Fares & Finance",
  tickets: "Tickets",
  payments: "Payments",
  reconciliation: "Reconciliation",
  reports: "Reports",
  ridership: "Ridership",
  revenue: "Revenue",
  settings: "Settings",
  team: "Team",
  integrations: "Integrations",
};

export function DashboardHeader() {
  const { pathname } = useLocation();
  const { logout } = useAuth();
  const { dateRange, setDateRange } = useOperationalScope();
  const segments = pathname.split("/").filter(Boolean);
  const section = labels[segments[0]] ?? "Operations";
  const last = segments.at(-1) ?? "operations";
  const page =
    labels[last] ??
    (segments.includes("dispatch")
      ? "Trip Details"
      : segments.includes("incidents")
        ? "Incident Details"
        : "Overview");
  return (
    <header className="flex h-14 shrink-0 items-center justify-between gap-3 border-b bg-background px-3">
      <div className="flex min-w-0 items-center gap-2">
        <SidebarTrigger />
        <Separator orientation="vertical" className="mr-2 h-4" />

        <Breadcrumb>
          <BreadcrumbList>
            <BreadcrumbItem className="hidden md:block">
              <BreadcrumbLink href="/operations">Dashboard</BreadcrumbLink>
            </BreadcrumbItem>
            <BreadcrumbSeparator className="hidden md:block" />
            <BreadcrumbItem>
              <BreadcrumbPage>
                {segments.length > 1 ? `${section} / ${page}` : section}
              </BreadcrumbPage>
            </BreadcrumbItem>
          </BreadcrumbList>
        </Breadcrumb>
      </div>
      <div className="flex shrink-0 items-center gap-2">
        <Select
          value={dateRange}
          onValueChange={(value) => value && setDateRange(value)}
        >
          <SelectTrigger
            size="default"
            aria-label="Date range"
            className="h-10 w-36 rounded-full bg-muted/40 px-4 text-sm"
          >
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="Today">Today</SelectItem>
            <SelectItem value="Week">This week</SelectItem>
            <SelectItem value="Month">This month</SelectItem>
          </SelectContent>
        </Select>
        <Button
          type="button"
          className="h-10 rounded-full bg-red-500 px-5 text-sm text-white hover:bg-red-600"
          onClick={logout}
        >
          Log out
        </Button>
      </div>
    </header>
  );
}
