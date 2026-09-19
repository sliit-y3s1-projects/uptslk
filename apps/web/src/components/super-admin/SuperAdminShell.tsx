import type { ReactNode } from "react";
import { SidebarInset, SidebarProvider } from "@/components/ui/sidebar";
import { SuperAdminSidebar } from "@/components/super-admin/SuperAdminSidebar";
import { SuperAdminHeader } from "@/components/super-admin/SuperAdminHeader";

export function SuperAdminShell({ children }: { children: ReactNode }) {
  return (
    <SidebarProvider
      style={{ "--sidebar-width": "16rem" } as React.CSSProperties}
    >
      <SuperAdminSidebar />
      <SidebarInset>
        <SuperAdminHeader />
        {children}
      </SidebarInset>
    </SidebarProvider>
  );
}
