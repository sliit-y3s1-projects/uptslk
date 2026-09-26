import { useMemo, useState } from "react";
import {
  Search,
} from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { PageHeading } from "@/components/shared/PageHeading";
import { StatusBadge } from "@/components/shared/StatusBadge";
import { auditEvents } from "@/mock/organization";

export function AuditLogPage() {
  const [query, setQuery] = useState("");
  const events = useMemo(
    () =>
      auditEvents.filter((event) =>
        Object.values(event)
          .join(" ")
          .toLowerCase()
          .includes(query.toLowerCase()),
      ),
    [query],
  );
  return (
    <main className="flex flex-1 flex-col gap-4 bg-muted/20 p-5">
      <PageHeading
        title="Audit log"
        description="Trace organization and centre-level administrative actions."
        action={<Button variant="outline">Export CSV</Button>}
      />
      <div className="relative max-w-xl">
        <Search className="absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
        <Input
          className="pl-9"
          placeholder="Search actor, action, target, or centre"
          value={query}
          onChange={(event) => setQuery(event.target.value)}
        />
      </div>
      <section className="overflow-hidden rounded-lg border bg-card">
        <div className="hidden grid-cols-[120px_minmax(180px,1fr)_minmax(220px,1.3fr)_minmax(180px,1fr)_160px] gap-4 border-b px-4 py-3 text-xs font-medium uppercase text-muted-foreground md:grid">
          <span>Event</span>
          <span>Actor</span>
          <span>Action</span>
          <span>Target</span>
          <span>Source</span>
        </div>
        {events.map((event) => (
          <div
            key={event.id}
            className="grid gap-2 border-b px-4 py-4 text-sm md:grid-cols-[120px_minmax(180px,1fr)_minmax(220px,1.3fr)_minmax(180px,1fr)_160px] md:items-center"
          >
            <div>
              <p className="font-medium">{event.id}</p>
              <p className="text-xs text-muted-foreground">{event.time}</p>
            </div>
            <p>{event.actor}</p>
            <p>{event.action}</p>
            <p className="text-muted-foreground">{event.target}</p>
            <StatusBadge
              label={event.source}
              tone={event.source === "Super Admin" ? "warning" : "neutral"}
            />
          </div>
        ))}
      </section>
    </main>
  );
}

