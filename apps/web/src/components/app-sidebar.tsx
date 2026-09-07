import * as React from "react";
import { HugeiconsIcon } from "@hugeicons/react";
import { ChevronRight, LogOut } from "lucide-react";
import { NavLink, useLocation } from "react-router";
import {
  Analytics01Icon,
  BusFrontIcon,
  ClipboardListIcon,
  Route01Icon,
  Settings01Icon,
  Ticket01Icon,
  UserMultipleIcon,
} from "@hugeicons/core-free-icons";

import {
  Sidebar,
  SidebarContent,
  SidebarGroup,
  SidebarHeader,
  SidebarFooter,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
  SidebarMenuSub,
  SidebarMenuSubButton,
  SidebarMenuSubItem,
  SidebarRail,
} from "@/components/ui/sidebar";
import { Button } from "@/components/ui/button";
import { useAuth } from "@/hooks/useAuth";

const data = {
  navMain: [
    {
      title: "Operations",
      url: "/operations",
      icon: ClipboardListIcon,
      items: [
        { title: "Overview", url: "/operations", isActive: true },
        { title: "Dispatch", url: "/operations/dispatch" },
        { title: "Trip history", url: "/operations/history" },
        { title: "Bay management", url: "/operations/bays" },
        { title: "Incidents", url: "/operations/incidents" },
        { title: "Approvals", url: "/operations/approvals" },
      ],
    },
    {
      title: "Network",
      url: "/network/routes",
      icon: Route01Icon,
      items: [
        { title: "Routes", url: "/network/routes" },
        { title: "Timetables", url: "/network/timetables" },
        { title: "Stops", url: "/network/stops" },
      ],
    },
    {
      title: "Fleet",
      url: "/fleet/vehicles",
      icon: BusFrontIcon,
      items: [
        { title: "Vehicles", url: "/fleet/vehicles" },
        { title: "Drivers", url: "/fleet/drivers" },
        { title: "Maintenance", url: "/fleet/maintenance" },
      ],
    },
    {
      title: "Passengers",
      url: "/passengers/flow",
      icon: UserMultipleIcon,
      items: [
        { title: "Passenger flow", url: "/passengers/flow" },
        { title: "Assistance", url: "/passengers/assistance" },
        { title: "Accounts", url: "/riders/accounts" },
        { title: "Support", url: "/riders/support" },
      ],
    },
    {
      title: "Fares & finance",
      url: "/fares/tickets",
      icon: Ticket01Icon,
      items: [
        { title: "Tickets", url: "/fares/tickets" },
        { title: "Payments", url: "/fares/payments" },
        { title: "Reconciliation", url: "/fares/reconciliation" },
      ],
    },
    {
      title: "Reports",
      url: "/reports/ridership",
      icon: Analytics01Icon,
      items: [
        { title: "Ridership", url: "/reports/ridership" },
        { title: "Revenue", url: "/reports/revenue" },
      ],
    },
    {
      title: "Settings",
      url: "/settings/team",
      icon: Settings01Icon,
      items: [
        { title: "Team", url: "/settings/team" },
        { title: "Integrations", url: "/settings/integrations" },
      ],
    },
  ],
};

const defaultOpenItems = data.navMain
  .filter((item) => item.items.some((subItem) => subItem.isActive))
  .map((item) => item.title);

export function AppSidebar({ ...props }: React.ComponentProps<typeof Sidebar>) {
  const { logout } = useAuth();
  const { pathname } = useLocation();
  const [openItems, setOpenItems] = React.useState<string[]>(defaultOpenItems);
  const matchesPath = (url: string) => url === "/operations" ? pathname === url : pathname.startsWith(url);

  function toggleItem(title: string) {
    setOpenItems((current) =>
      current.includes(title)
        ? current.filter((item) => item !== title)
        : [...current, title],
    );
  }

  return (
    <Sidebar {...props}>
      <SidebarHeader>
        <SidebarMenu>
          <SidebarMenuItem>
            <SidebarMenuButton
              size="lg"
              render={<a href="/operations" aria-label="UPTSLK Console"><span className="text-2xl font-bold tracking-tight">UPTSLK Console</span></a>}
            />
          </SidebarMenuItem>
        </SidebarMenu>
      </SidebarHeader>
      <SidebarContent>
        <SidebarGroup>
          <SidebarMenu>
            {data.navMain.map((item) => {
              const isOpen = openItems.includes(item.title);
              const isActive = item.items.some((subItem) => matchesPath(subItem.url));

              return (
                <SidebarMenuItem key={item.title}>
                  <SidebarMenuButton
                    type="button"
                    isActive={isActive}
                    tooltip={item.title}
                    onClick={() => toggleItem(item.title)}
                    className="cursor-pointer data-active:bg-sidebar-primary data-active:text-sidebar-primary-foreground data-active:hover:bg-sidebar-primary/90 data-active:hover:text-sidebar-primary-foreground"
                  >
                    <HugeiconsIcon
                      icon={item.icon}
                      strokeWidth={2}
                      className="size-4"
                    />
                    <span>{item.title}</span>
                    <ChevronRight
                      className={
                        isOpen
                          ? "ml-auto size-4 rotate-90 transition-transform duration-300 ease-out"
                          : "ml-auto size-4 transition-transform duration-300 ease-out"
                      }
                    />
                  </SidebarMenuButton>
                  <div
                    className={
                      isOpen
                        ? "max-h-72 translate-y-0 opacity-100 transition-all duration-300 ease-out"
                        : "max-h-0 -translate-y-1 opacity-0 transition-all duration-300 ease-out"
                    }
                  >
                    <div className="overflow-hidden">
                      <SidebarMenuSub className="mt-1.5">
                        {item.items.map((subItem) => (
                          <SidebarMenuSubItem key={subItem.title}>
                            <SidebarMenuSubButton
                              isActive={matchesPath(subItem.url)}
                              className="data-active:bg-sidebar-accent data-active:text-sidebar-accent-foreground data-active:font-medium"
                              render={
                                <NavLink to={subItem.url}>
                                  <span>{subItem.title}</span>
                                </NavLink>
                              }
                            />
                          </SidebarMenuSubItem>
                        ))}
                      </SidebarMenuSub>
                    </div>
                  </div>
                </SidebarMenuItem>
              );
            })}
          </SidebarMenu>
        </SidebarGroup>
      </SidebarContent>
      <SidebarFooter>
        <Button
          type="button"
          variant="ghost"
          className="w-full justify-start gap-2 text-muted-foreground hover:text-foreground"
          onClick={logout}
        >
          <LogOut className="size-4" />
          <span>Log out</span>
        </Button>
      </SidebarFooter>
      <SidebarRail />
    </Sidebar>
  );
}
