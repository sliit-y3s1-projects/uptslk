import type { ReactNode } from "react";
import { AppSidebar } from "@/components/app-sidebar";
import { DashboardHeader } from "@/components/dashboard/DashboardHeader";
import { SidebarInset, SidebarProvider } from "@/components/ui/sidebar";
import { OperationalScopeProvider } from "@/context/OperationalScopeContext";
import { MockDataProvider } from "@/context/MockDataContext";
import { useAuth } from "@/hooks/useAuth";
import { centres } from "@/mock/centres";
import { DispatchMockProvider } from "@/context/DispatchMockContext";

export function AdminShell({ children }: { children: ReactNode }) {
  const { user } = useAuth();
  const centre = centres.find((item) => item.id === user?.centreId);
  return (
    <MockDataProvider>
      <DispatchMockProvider>
        <OperationalScopeProvider initialDistrict={centre?.district}>
          <SidebarProvider
            style={{ "--sidebar-width": "14.5rem" } as React.CSSProperties}
          >
            <AppSidebar />
            <SidebarInset>
              <DashboardHeader />
              {children}
            </SidebarInset>
          </SidebarProvider>
        </OperationalScopeProvider>
      </DispatchMockProvider>
    </MockDataProvider>
  );
}
