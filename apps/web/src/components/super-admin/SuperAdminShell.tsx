import type { ReactNode } from "react";
import { SidebarInset, SidebarProvider } from "@/components/ui/sidebar";
import { SuperAdminSidebar } from "@/components/super-admin/SuperAdminSidebar";
import { SuperAdminHeader } from "@/components/super-admin/SuperAdminHeader";
import { OrganizationMockProvider } from "@/context/OrganizationMockContext";
import { MockDataProvider } from "@/context/MockDataContext";

export function SuperAdminShell({ children }: { children: ReactNode }) {
  return <OrganizationMockProvider><MockDataProvider><SidebarProvider style={{ "--sidebar-width": "16rem" } as React.CSSProperties}><SuperAdminSidebar /><SidebarInset><SuperAdminHeader />{children}</SidebarInset></SidebarProvider></MockDataProvider></OrganizationMockProvider>;
}
