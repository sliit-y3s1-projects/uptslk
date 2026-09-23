import { Bell } from "lucide-react";
import { useLocation } from "react-router";
import { Button } from "@/components/ui/button";
import { Separator } from "@/components/ui/separator";
import { SidebarTrigger } from "@/components/ui/sidebar";

const labels: Record<string, string> = {
  admin: "Overview",
  centres: "Multimodal Centres",
  new: "Create",
  employees: "Employees",
  roles: "Roles & Permissions",
  access: "Access Requests",
  audit: "Audit Log",
  health: "Platform Health",
  settings: "System Settings",
  operations: "Centre Operations",
  routes: "Centre Routes",
  vehicles: "Centre Fleet",
};

export function SuperAdminHeader() {
  const segments = useLocation().pathname.split("/").filter(Boolean);
  const last = segments.at(-1) ?? "admin";

  const page =
    labels[last] ??
    (segments.includes("routes")
      ? "Route Details"
      : segments.includes("vehicles")
        ? "Vehicle Details"
        : "Centre Profile");

  return (
    <header className="flex h-14 items-center justify-between border-b bg-card px-3">
      <div className="flex items-center gap-3">
        <SidebarTrigger />
        <Separator orientation="vertical" className="h-4" />
        <div>
          <p className="text-sm font-medium">Super Admin</p>
          <p className="text-xs text-muted-foreground">{page}</p>
        </div>
      </div>
      <Button size="icon" variant="ghost" aria-label="Notifications">
        <Bell />
      </Button>
    </header>
  );
}
