import type { ReactNode } from "react";
import { useAuth } from "@/hooks/useAuth";
import { useLocation } from "react-router";
import { NotFoundPage } from "@/components/NotFoundPage";

export function RequireRole({
  role,
  children,
}: {
  role: string | string[];
  children: ReactNode;
}) {
  const { user } = useAuth();
  const location = useLocation();

  if (!user) return null;

  const allowedRoles = Array.isArray(role) ? role : [role];
  const knownArea = ["/admin", "/operations", "/network", "/fleet", "/passengers", "/riders", "/fares", "/reports", "/settings"].some((prefix) => location.pathname === prefix || location.pathname.startsWith(`${prefix}/`));
  if (!knownArea) return <NotFoundPage />;
  if (!allowedRoles.includes(user.role)) {
    return (
      <main className="flex min-h-screen items-center justify-center px-6 text-center">
        <div>
          <h1 className="text-xl font-semibold">Access Restricted</h1>
          <p className="mt-2 text-sm text-muted-foreground">
            This area is only available to {allowedRoles.join(", ")} accounts.
          </p>
        </div>
      </main>
    );
  }

  return <>{children}</>;
}
