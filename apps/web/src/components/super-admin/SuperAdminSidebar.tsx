import { Activity, Building2, FileClock, Gauge, KeyRound, LogOut, Settings, ShieldCheck, UserPlus, UsersRound } from "lucide-react";
import { NavLink, useLocation } from "react-router";
import { Button } from "@/components/ui/button";
import { Sidebar, SidebarContent, SidebarFooter, SidebarGroup, SidebarGroupLabel, SidebarHeader, SidebarMenu, SidebarMenuButton, SidebarMenuItem, SidebarRail } from "@/components/ui/sidebar";
import { useAuth } from "@/hooks/useAuth";

const groups = [
  { label: "Organization", items: [{ title: "Overview", url: "/admin", icon: Gauge }, { title: "Multimodal centres", url: "/admin/centres", icon: Building2 }] },
  { label: "People & access", items: [{ title: "Employees", url: "/admin/employees", icon: UsersRound }, { title: "Roles & permissions", url: "/admin/roles", icon: KeyRound }, { title: "Access requests", url: "/admin/access", icon: UserPlus }] },
  { label: "Governance", items: [{ title: "Audit log", url: "/admin/audit", icon: FileClock }, { title: "Platform health", url: "/admin/health", icon: Activity }] },
  { label: "System", items: [{ title: "Settings", url: "/admin/settings", icon: Settings }] },
];

export function SuperAdminSidebar() {
  const { logout } = useAuth();
  const { pathname } = useLocation();
  return <Sidebar><SidebarHeader><SidebarMenu><SidebarMenuItem><SidebarMenuButton size="lg" render={<NavLink to="/admin" />}><span className="text-2xl font-bold tracking-tight">UPTSLK Console</span></SidebarMenuButton></SidebarMenuItem></SidebarMenu></SidebarHeader><SidebarContent>{groups.map((group) => <SidebarGroup key={group.label}><SidebarGroupLabel>{group.label}</SidebarGroupLabel><SidebarMenu>{group.items.map((item) => { const isActive = item.url === "/admin" ? pathname === "/admin" : pathname.startsWith(item.url); return <SidebarMenuItem key={item.url}><SidebarMenuButton isActive={isActive} tooltip={item.title} className="data-active:bg-sidebar-primary data-active:font-medium data-active:text-sidebar-primary-foreground data-active:hover:bg-sidebar-primary/90 data-active:hover:text-sidebar-primary-foreground" render={<NavLink to={item.url} />}><item.icon className="size-4" /><span>{item.title}</span></SidebarMenuButton></SidebarMenuItem>; })}</SidebarMenu></SidebarGroup>)}</SidebarContent><SidebarFooter><div className="mb-2 flex items-center gap-2 rounded-md border bg-muted/40 p-2"><div className="flex size-8 items-center justify-center rounded-md bg-primary/10 text-primary"><ShieldCheck className="size-4" /></div><div className="min-w-0"><p className="truncate text-xs font-medium">Super Admin</p><p className="truncate text-xs text-muted-foreground">Organization-wide</p></div></div><Button variant="ghost" className="w-full justify-start text-muted-foreground" onClick={logout}><LogOut /> Log out</Button></SidebarFooter><SidebarRail /></Sidebar>;
}
