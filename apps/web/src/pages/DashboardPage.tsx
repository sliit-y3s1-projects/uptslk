import { AppSidebar } from "@/components/app-sidebar";
import { DashboardHeader } from "@/components/dashboard/DashboardHeader";
import { MetricCard } from "@/components/dashboard/MetricCard";
import { NetworkStatusPanel } from "@/components/dashboard/NetworkStatusPanel";
import { PriorityAlertsPanel } from "@/components/dashboard/PriorityAlertsPanel";
import { SidebarInset, SidebarProvider } from "@/components/ui/sidebar";

const metrics = [
  { label: "Active routes", value: "42" },
  { label: "Vehicles online", value: "118" },
  { label: "Open incidents", value: "7" },
  { label: "Avg headway", value: "12m" },
];

const networkStatus = [
  { region: "Western", routes: "18 routes", status: "Stable", load: "76%" },
  { region: "Central", routes: "11 routes", status: "Watch", load: "64%" },
  { region: "Southern", routes: "13 routes", status: "Stable", load: "71%" },
  { region: "Northern", routes: "7 routes", status: "Stable", load: "58%" },
  { region: "Eastern", routes: "9 routes", status: "Watch", load: "67%" },
  {
    region: "North Western",
    routes: "8 routes",
    status: "Limited",
    load: "49%",
  },
];

const priorityAlerts = [
  {
    label: "Route 138",
    detail: "14 minute delay near Nugegoda",
    level: "High",
  },
  {
    label: "Galle Road",
    detail: "Two replacement buses assigned",
    level: "Medium",
  },
  {
    label: "Kandy depot",
    detail: "Three vehicles pending inspection",
    level: "Low",
  },
  {
    label: "Route 100",
    detail: "Driver handover missed at Pettah stand",
    level: "Medium",
  },
  {
    label: "Matara terminal",
    detail: "Passenger queue above normal threshold",
    level: "High",
  },
];

export function DashboardPage() {
  return (
    <SidebarProvider
      style={
        {
          "--sidebar-width": "14.5rem",
        } as React.CSSProperties
      }
    >
      <AppSidebar />

      <SidebarInset>
        <DashboardHeader />

        <main className="flex flex-1 flex-col gap-4 bg-muted/20 p-4">
          <div className="flex flex-col gap-1">
            <h1 className="text-xl font-semibold tracking-tight">
              Operations command
            </h1>
            <p className="text-sm text-muted-foreground">
              Live service health, fleet readiness, and issue response.
            </p>
          </div>

          <section className="grid overflow-hidden rounded-lg border bg-card sm:grid-cols-2 xl:grid-cols-4">
            {metrics.map((metric) => (
              <MetricCard
                key={metric.label}
                label={metric.label}
                value={metric.value}
              />
            ))}
          </section>

          <section className="grid flex-1 gap-4 lg:grid-cols-[1fr_340px]">
            <NetworkStatusPanel items={networkStatus} />
            <PriorityAlertsPanel items={priorityAlerts} />
          </section>
        </main>
      </SidebarInset>
    </SidebarProvider>
  );
}
