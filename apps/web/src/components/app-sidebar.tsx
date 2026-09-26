import * as React from "react";
import { HugeiconsIcon } from "@hugeicons/react";
import { NavLink, useLocation } from "react-router";
import {
  BusFrontIcon,
  ClipboardListIcon,
  Route01Icon,
  Ticket01Icon,
  UserMultipleIcon,
} from "@hugeicons/core-free-icons";

import {
  Sidebar,
  SidebarContent,
  SidebarGroup,
  SidebarGroupLabel,
  SidebarHeader,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
  SidebarRail,
} from "@/components/ui/sidebar";
import { useAuth } from "@/hooks/useAuth";

const data = {
  navMain: [
    {
      title: "Operations",
      icon: ClipboardListIcon,
      items: [
        { title: "Overview", url: "/operations", isActive: true },
        { title: "Dispatch", url: "/operations/dispatch" },
        { title: "Duty roster", url: "/operations/duty-roster" },
        { title: "Trip history", url: "/operations/history" },
        { title: "Bay management", url: "/operations/bays" },
        { title: "Incidents", url: "/operations/incidents" },
        { title: "Recovery agents", url: "/operations/agent-recovery" },
      ],
    },
    {
      title: "Network",
      icon: Route01Icon,
      items: [
        { title: "Routes", url: "/network/routes" },
        { title: "Timetables", url: "/network/timetables" },
      ],
    },
    {
      title: "Fleet",
      icon: BusFrontIcon,
      items: [
        { title: "Vehicles", url: "/fleet/vehicles" },
        { title: "Drivers", url: "/fleet/drivers" },
        { title: "Maintenance", url: "/fleet/maintenance" },
      ],
    },
    {
      title: "Passengers",
      icon: UserMultipleIcon,
      items: [
        { title: "Passenger flow", url: "/passengers/flow" },
      ],
    },
    {
      title: "Fares",
      icon: Ticket01Icon,
      items: [{ title: "Fare rules", url: "/fares/fare-rules" }],
    },
  ],
};

export function AppSidebar({ ...props }: React.ComponentProps<typeof Sidebar>) {
  const { user } = useAuth();
  const { pathname } = useLocation();
  const matchesPath = (url: string) =>
    url === "/operations" ? pathname === url : pathname.startsWith(url);
  const isAdmin = user?.role === "Admin" || user?.role === "SuperAdmin";

  return (
    <Sidebar className="border-r-2 border-slate-300" {...props}>
      <SidebarHeader className="h-14 justify-center border-b border-sidebar-border px-5 py-0">
        <NavLink
          to="/operations"
          aria-label={`${isAdmin ? "Admin" : "Centre"} operations`}
          className="w-fit text-[22px] font-semibold tracking-[-0.045em] text-sidebar-foreground"
        >
          <span className="font-bold text-primary">UPTS</span>{" "}
          {isAdmin ? "Admin" : "Centre"}{" "}
          <span className="font-bold text-primary">Ops</span>
        </NavLink>
      </SidebarHeader>
      <SidebarContent className="gap-0 px-0 py-2">
        {data.navMain.map((item) => (
          <SidebarGroup
            key={item.title}
            className="border-t-2 border-sidebar-border px-0 py-2 first:border-t-0"
          >
            <SidebarGroupLabel
              className="h-8 gap-2.5 px-5 text-base font-semibold tracking-[0.02em] text-primary"
            >
              <HugeiconsIcon
                icon={item.icon}
                strokeWidth={2}
                className="size-4 text-primary"
              />
              <span>{item.title}</span>
            </SidebarGroupLabel>
            <SidebarMenu className="mt-1 gap-0">
              {item.items.map((subItem) => (
                <SidebarMenuItem key={subItem.title}>
                  <SidebarMenuButton
                    isActive={matchesPath(subItem.url)}
                    tooltip={subItem.title}
                    render={<NavLink to={subItem.url} />}
                    className="h-8 rounded-none bg-transparent px-5 text-sm font-medium text-sidebar-foreground hover:bg-transparent hover:text-primary data-active:bg-primary data-active:font-semibold data-active:text-primary-foreground data-active:hover:bg-primary data-active:hover:text-primary-foreground"
                  >
                    <span>{subItem.title}</span>
                  </SidebarMenuButton>
                </SidebarMenuItem>
              ))}
            </SidebarMenu>
          </SidebarGroup>
        ))}
      </SidebarContent>
      <SidebarRail />
    </Sidebar>
  );
}
