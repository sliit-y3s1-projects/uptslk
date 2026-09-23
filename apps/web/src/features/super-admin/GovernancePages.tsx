import { useMemo, useState } from "react";
import {
  Activity,
  CheckCircle2,
  Clock3,
  Database,
  Search,
  Settings2,
  ShieldCheck,
  TriangleAlert,
} from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Switch } from "@/components/ui/switch";
import { PageHeading } from "@/components/shared/PageHeading";
import { StatusBadge } from "@/components/shared/StatusBadge";
import { auditEvents, serviceHealth } from "@/mock/organization";

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

export function PlatformHealthPage() {
  const operational = serviceHealth.filter(
    (service) => service.status === "Operational",
  ).length;
  return (
    <main className="flex flex-1 flex-col gap-4 bg-muted/20 p-5">
      <PageHeading
        title="Platform health"
        description="Monitor shared services used by every multimodal centre."
        action={
          <Button variant="outline">
            <Activity /> Refresh checks
          </Button>
        }
      />
      <section className="grid gap-3 sm:grid-cols-3">
        <HealthMetric
          icon={CheckCircle2}
          label="Operational"
          value={`${operational}/${serviceHealth.length}`}
        />
        <HealthMetric
          icon={TriangleAlert}
          label="Degraded"
          value={String(serviceHealth.length - operational)}
        />
        <HealthMetric icon={Clock3} label="Last checked" value="Just now" />
      </section>
      <section className="overflow-hidden rounded-lg border bg-card">
        <div className="grid grid-cols-[minmax(0,1fr)_140px_120px] gap-4 border-b px-4 py-3 text-xs font-medium uppercase text-muted-foreground">
          <span>Service</span>
          <span>Status</span>
          <span>Latency</span>
        </div>
        {serviceHealth.map((service) => (
          <div
            key={service.name}
            className="grid grid-cols-[minmax(0,1fr)_140px_120px] gap-4 border-b px-4 py-4 text-sm"
          >
            <div className="flex items-center gap-3">
              <Database className="size-4 text-muted-foreground" />
              <span className="font-medium">{service.name}</span>
            </div>
            <StatusBadge
              label={service.status}
              tone={service.status === "Operational" ? "good" : "warning"}
            />
            <span className="text-muted-foreground">{service.latency}</span>
          </div>
        ))}
      </section>
    </main>
  );
}

export function SystemSettingsPage() {
  return (
    <main className="flex flex-1 flex-col gap-4 bg-muted/20 p-5">
      <PageHeading
        title="System settings"
        description="Configure organization-wide security and platform defaults."
      />
      <section className="grid gap-4 xl:grid-cols-2">
        <SettingCard
          icon={ShieldCheck}
          title="Identity security"
          description="Require stronger authentication for privileged accounts."
          rows={[
            "Require MFA for Super Admins",
            "Notify on elevated access",
            "Suspend dormant accounts",
          ]}
        />
        <SettingCard
          icon={Settings2}
          title="Organization defaults"
          description="Shared configuration inherited by new centres."
          rows={[
            "Enable audit retention",
            "Allow centre role invitations",
            "Show platform health alerts",
          ]}
        />
      </section>
      <div className="flex justify-end">
        <Button>Save settings</Button>
      </div>
    </main>
  );
}

function HealthMetric({
  icon: Icon,
  label,
  value,
}: {
  icon: typeof Activity;
  label: string;
  value: string;
}) {
  return (
    <article className="rounded-lg border bg-card p-4">
      <Icon className="size-4 text-primary" />
      <p className="mt-3 text-sm text-muted-foreground">{label}</p>
      <p className="mt-1 text-xl font-semibold">{value}</p>
    </article>
  );
}
function SettingCard({
  icon: Icon,
  title,
  description,
  rows,
}: {
  icon: typeof Activity;
  title: string;
  description: string;
  rows: string[];
}) {
  return (
    <article className="rounded-lg border bg-card p-5">
      <div className="flex gap-3">
        <div className="flex size-9 items-center justify-center rounded-md bg-primary/10 text-primary">
          <Icon className="size-4" />
        </div>
        <div>
          <h2 className="font-semibold">{title}</h2>
          <p className="text-sm text-muted-foreground">{description}</p>
        </div>
      </div>
      <div className="mt-5 divide-y">
        {rows.map((row, index) => (
          <label
            key={row}
            className="flex items-center justify-between py-3 text-sm"
          >
            <span>{row}</span>
            <Switch defaultChecked={index !== 2} />
          </label>
        ))}
      </div>
    </article>
  );
}
