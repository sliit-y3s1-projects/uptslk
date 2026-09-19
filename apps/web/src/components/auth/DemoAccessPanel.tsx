import { Building2, ShieldCheck } from "lucide-react";
import { Button } from "@/components/ui/button";
import { demoAccounts } from "@/mock/centres";

export function DemoAccessPanel({
  onLogin,
}: {
  onLogin: (role: string, centreId?: string) => void;
}) {
  return (
    <div className="w-full border-t pt-5 text-left">
      <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
        Demo workspace access
      </p>
      <p className="mt-1 text-sm text-muted-foreground">
        Choose a mock centre or the read-only national overview.
      </p>
      <div className="mt-3 grid gap-2">
        {demoAccounts.map((account) => (
          <Button
            key={account.label}
            type="button"
            variant="outline"
            className="justify-start"
            onClick={() => onLogin(account.role, account.centreId)}
          >
            {account.role === "SuperAdmin" ? <ShieldCheck /> : <Building2 />}{" "}
            {account.label}
          </Button>
        ))}
      </div>
    </div>
  );
}
