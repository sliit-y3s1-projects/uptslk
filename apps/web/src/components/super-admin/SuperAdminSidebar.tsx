import { Landmark, Settings, ShieldCheck, UsersRound } from "lucide-react";
import { NavLink, useLocation } from "react-router";
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

const groups = [
  {
    label: "Organization",
    icon: Landmark,
    items: [
      { title: "Overview", url: "/admin" },
      { title: "Multimodal centres", url: "/admin/centres" },
      { title: "Routes", url: "/network/routes" },
    ],
  },
  {
    label: "People & access",
    icon: UsersRound,
    items: [
      { title: "Employees", url: "/admin/employees" },
      { title: "Users", url: "/admin/users" },
      { title: "Roles & permissions", url: "/admin/roles" },
      { title: "Access requests", url: "/admin/access" },
    ],
  },
  {
    label: "Governance",
    icon: ShieldCheck,
    items: [
      { title: "Audit log", url: "/admin/audit" },
      { title: "Platform health", url: "/admin/health" },
    ],
  },
  {
    label: "System",
    icon: Settings,
    items: [{ title: "Settings", url: "/admin/settings" }],
  },
];

export function SuperAdminSidebar() {
  const { pathname } = useLocation();
  return (
    <Sidebar className="border-r-2 border-slate-300">
      <SidebarHeader className="border-b border-sidebar-border px-2 py-3">
        <SidebarMenu>
          <SidebarMenuItem>
            <SidebarMenuButton size="lg" render={<NavLink to="/admin" />}>
              <span
                className="text-xl font-bold tracking-[0.06em] text-sidebar-foreground"
                style={{ fontFamily: "'Geist Variable', sans-serif" }}
              >
                ADMIN <span className="text-primary">OPS</span>
              </span>
            </SidebarMenuButton>
          </SidebarMenuItem>
        </SidebarMenu>
      </SidebarHeader>
      <SidebarContent className="gap-0 px-0 py-2">
        {groups.map((group) => (
          <SidebarGroup
            key={group.label}
            className="border-t-2 border-sidebar-border px-0 py-2 first:border-t-0"
          >
            <SidebarGroupLabel
              className="h-8 gap-2.5 px-5 text-[15px] font-semibold tracking-[0.02em] text-primary"
            >
              <group.icon className="size-4" />
              <span>{group.label}</span>
            </SidebarGroupLabel>
            <SidebarMenu className="mt-1 gap-0">
              {group.items.map((item) => {
                const isActive =
                  item.url === "/admin"
                    ? pathname === "/admin"
                    : pathname.startsWith(item.url);
                return (
                  <SidebarMenuItem key={item.url}>
                    <SidebarMenuButton
                      isActive={isActive}
                      tooltip={item.title}
                      className="h-8 rounded-none bg-transparent px-5 text-[13px] font-medium text-sidebar-foreground hover:bg-transparent hover:text-primary data-active:bg-primary data-active:font-semibold data-active:text-primary-foreground data-active:hover:bg-primary data-active:hover:text-primary-foreground"
                      render={<NavLink to={item.url} />}
                    >
                      <span>{item.title}</span>
                    </SidebarMenuButton>
                  </SidebarMenuItem>
                );
              })}
            </SidebarMenu>
          </SidebarGroup>
        ))}
      </SidebarContent>
      <SidebarRail />
    </Sidebar>
  );
}
