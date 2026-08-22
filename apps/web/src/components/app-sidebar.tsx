import * as React from "react";
import { HugeiconsIcon } from "@hugeicons/react";
import { ChevronRight } from "lucide-react";
import {
  Analytics01Icon,
  BusFrontIcon,
  ClipboardListIcon,
  DashboardSquare03Icon,
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
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
  SidebarMenuSub,
  SidebarMenuSubButton,
  SidebarMenuSubItem,
  SidebarRail,
} from "@/components/ui/sidebar";

const data = {
  navMain: [
    {
      title: "Operations",
      url: "#",
      icon: ClipboardListIcon,
      items: [
        { title: "Overview", url: "#", isActive: true },
        { title: "Dispatch", url: "#" },
        { title: "Incidents", url: "#" },
      ],
    },
    {
      title: "Network",
      url: "#",
      icon: Route01Icon,
      items: [
        { title: "Routes", url: "#" },
        { title: "Stops", url: "#" },
        { title: "Service Areas", url: "#" },
      ],
    },
    {
      title: "Fleet",
      url: "#",
      icon: BusFrontIcon,
      items: [
        { title: "Vehicles", url: "#" },
        { title: "Drivers", url: "#" },
        { title: "Maintenance", url: "#" },
      ],
    },
    {
      title: "Riders",
      url: "#",
      icon: UserMultipleIcon,
      items: [
        { title: "Accounts", url: "#" },
        { title: "Support", url: "#" },
      ],
    },
    {
      title: "Fares",
      url: "#",
      icon: Ticket01Icon,
      items: [
        { title: "Tickets", url: "#" },
        { title: "Payments", url: "#" },
      ],
    },
    {
      title: "Reports",
      url: "#",
      icon: Analytics01Icon,
      items: [
        { title: "Ridership", url: "#" },
        { title: "Revenue", url: "#" },
      ],
    },
    {
      title: "Settings",
      url: "#",
      icon: Settings01Icon,
      items: [
        { title: "Team", url: "#" },
        { title: "Integrations", url: "#" },
      ],
    },
  ],
};

const defaultOpenItems = data.navMain
  .filter((item) => item.items.some((subItem) => subItem.isActive))
  .map((item) => item.title);

export function AppSidebar({ ...props }: React.ComponentProps<typeof Sidebar>) {
  const [openItems, setOpenItems] = React.useState<string[]>(defaultOpenItems);

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
              render={
                <a href="#" aria-label="UPTS dashboard">
                  <div className="flex aspect-square size-8 items-center justify-center rounded-lg bg-sidebar-primary text-sidebar-primary-foreground">
                    <HugeiconsIcon
                      icon={DashboardSquare03Icon}
                      strokeWidth={2}
                      className="size-4"
                    />
                  </div>
                  <div className="flex flex-col gap-0.5 leading-none">
                    <span className="font-medium">UPTS</span>
                    <span className="text-xs text-sidebar-foreground/70">
                      Transport Operations
                    </span>
                  </div>
                </a>
              }
            />
          </SidebarMenuItem>
        </SidebarMenu>
      </SidebarHeader>
      <SidebarContent>
        <SidebarGroup>
          <SidebarMenu>
            {data.navMain.map((item) => {
              const isOpen = openItems.includes(item.title);
              const isActive = item.items.some((subItem) => subItem.isActive);

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
                              isActive={subItem.isActive}
                              className="data-active:bg-sidebar-accent data-active:text-sidebar-accent-foreground data-active:font-medium"
                              render={
                                <a href={subItem.url}>
                                  <span>{subItem.title}</span>
                                </a>
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
      <SidebarRail />
    </Sidebar>
  );
}
