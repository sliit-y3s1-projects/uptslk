import { useLocation } from "react-router";
import { Button } from "@/components/ui/button";
import { Separator } from "@/components/ui/separator";
import { SidebarTrigger } from "@/components/ui/sidebar";
import { useAuth } from "@/hooks/useAuth";

const labels: Record<string, string> = {
  admin: "Overview",
  centres: "Multimodal Centres",
  new: "Create",
  employees: "Employees",
  roles: "Roles & Permissions",
  access: "Access Requests",
  audit: "Audit Log",
  operations: "Centre Operations",
  routes: "Centre Routes",
  vehicles: "Centre Fleet",
};

export function SuperAdminHeader() {
  const { logout } = useAuth();
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
      <Button
        type="button"
        className="h-10 rounded-full bg-red-500 px-5 text-sm text-white hover:bg-red-600"
        onClick={logout}
      >
        Log out
      </Button>
    </header>
  );
}
