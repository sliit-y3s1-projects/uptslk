import { Ticket, UserRound } from "lucide-react";
import { Link, useNavigate } from "react-router";
import { Button } from "@/components/ui/button";
import { useAuth } from "@/hooks/useAuth";
import type { ReactNode } from "react";

export function CommuterLayout({ children }: { children: ReactNode }) {
  return <div className="min-h-screen bg-slate-50"><CommuterHeader />{children}</div>;
}

export function CommuterHeader() {
  const navigate = useNavigate();
  const { user, logout } = useAuth();
  const portal = user?.role === "Admin" || user?.role === "SuperAdmin"
    ? { label: "Admin portal", path: "/admin" }
    : user && ["CentreManager", "Dispatcher", "FleetOfficer", "Driver"].includes(user.role)
      ? { label: "Operations", path: "/operations" }
      : null;

  return (
    <header className="sticky top-0 z-30 border-b border-slate-200 bg-white/95 backdrop-blur">
      <div className="flex min-h-20 w-full items-center justify-between gap-3 px-4 py-3 sm:px-10 lg:px-12 xl:px-24">
        <Link to="/" className="min-w-0 text-xl font-bold tracking-tight text-indigo-950 sm:text-2xl">
          UPTSLK{" "}
          <span className="hidden font-normal text-slate-500 sm:inline">Bus booking</span>
        </Link>
        <nav className="flex items-center gap-1 sm:gap-2">
          {user ? (
            <>
              <Link to="/my-tickets"><Button className="gap-2 rounded-full bg-indigo-950 px-3 text-white hover:bg-indigo-900 sm:h-12 sm:px-5"><Ticket className="size-4" /><span className="hidden min-[480px]:inline">My tickets</span></Button></Link>
              <Link to="/profile"><Button className="gap-2 rounded-full bg-indigo-100 px-3 text-indigo-950 hover:bg-indigo-200 sm:h-12 sm:px-5"><UserRound className="size-4" /><span className="hidden min-[480px]:inline">My profile</span></Button></Link>
              {portal && <Button variant="outline" className="hidden rounded-full border-primary/30 bg-primary/5 px-5 text-primary hover:bg-primary/10 sm:h-12 lg:inline-flex" onClick={() => navigate(portal.path)}>{portal.label}</Button>}
              <Button variant="outline" className="hidden rounded-full border-slate-300 bg-white px-5 sm:h-12 min-[480px]:inline-flex" onClick={() => { logout(); navigate("/"); }}>Sign out</Button>
            </>
          ) : <Link to="/login"><Button className="rounded-full bg-indigo-950 px-5 sm:h-12 sm:px-6">Sign in</Button></Link>}
        </nav>
      </div>
    </header>
  );
}
