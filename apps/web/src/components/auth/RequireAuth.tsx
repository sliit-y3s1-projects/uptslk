import type { ReactNode } from "react";
import { useAuth } from "@/hooks/useAuth";

export function RequireRole({
  role,
  children,
}: {
  role: string;
  children: ReactNode;
}) {
  const { user } = useAuth();

  if (!user) return null;

  if (user.role !== role) {
    return (
      <main className="flex min-h-screen items-center justify-center px-6 text-center">
        <div>
          <h1 className="text-xl font-semibold">Access Restricted</h1>
          <p className="mt-2 text-sm text-muted-foreground">
            This area is only available to {role} accounts.
          </p>
        </div>
      </main>
    );
  }

  return <>{children}</>;
}
