import {
  Activity,
  ArrowUpRight,
  CheckCircle2,
  Clock3,
  MapPinned,
  MoreHorizontal,
  WalletCards,
} from "lucide-react";
import { Button } from "@/components/ui/button";
import { StatusBadge } from "@/components/shared/StatusBadge";
import type { TableRow } from "@/mock/mock-data";

export type ResourceView =
  "table" | "board" | "cards" | "inbox" | "ledger" | "settings";

export function ResourceHighlights({
  view,
  rows,
}: {
  view: ResourceView;
  rows: TableRow[];
}) {
  if (view === "table") return null;
  if (view === "board")
    return (
      <section className="grid gap-3 xl:grid-cols-3">
        {["Needs action", "In progress", "Completed"].map((column, index) => (
          <article key={column} className="rounded-lg border bg-card">
            <div className="flex items-center justify-between border-b px-4 py-3">
              <h2 className="text-sm font-semibold">{column}</h2>
              <span className="text-xs text-muted-foreground">
                {index === 0 ? 2 : 1}
              </span>
            </div>
            <div className="space-y-2 p-3">
              {rows
                .filter((_, rowIndex) => rowIndex % 3 === index)
                .map((row) => (
                  <div
                    key={row.id}
                    className="rounded-md border bg-background p-3"
                  >
                    <div className="flex justify-between gap-2">
                      <p className="text-sm font-medium">{row.title}</p>
                      <MoreHorizontal className="size-4 text-muted-foreground" />
                    </div>
                    <p className="mt-1 text-xs text-muted-foreground">
                      {row.subtitle}
                    </p>
                    <div className="mt-3 flex items-center justify-between">
                      <StatusBadge label={row.status} tone={row.tone} />
                      <span className="text-xs text-muted-foreground">
                        {row.updated}
                      </span>
                    </div>
                  </div>
                ))}
            </div>
          </article>
        ))}
      </section>
    );
  if (view === "cards")
    return (
      <section className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">
        {rows.map((row, index) => (
          <article key={row.id} className="rounded-lg border bg-card p-4">
            <div className="flex items-start justify-between gap-3">
              <div className="flex size-9 items-center justify-center rounded-md bg-primary/10 text-primary">
                {index % 2 ? (
                  <Activity className="size-4" />
                ) : (
                  <MapPinned className="size-4" />
                )}
              </div>
              <StatusBadge label={row.status} tone={row.tone} />
            </div>
            <h2 className="mt-5 font-semibold">{row.title}</h2>
            <p className="mt-1 text-sm text-muted-foreground">{row.subtitle}</p>
            <div className="mt-5 flex items-center justify-between border-t pt-3 text-xs">
              <span className="text-muted-foreground">{row.updated}</span>
              <span className="font-medium">{row.meta}</span>
            </div>
          </article>
        ))}
      </section>
    );
  if (view === "inbox")
    return (
      <section className="grid overflow-hidden rounded-lg border bg-card lg:grid-cols-[minmax(0,0.9fr)_minmax(280px,1.1fr)]">
        <div className="border-b lg:border-b-0 lg:border-r">
          <div className="border-b p-4">
            <h2 className="font-semibold">Open queue</h2>
            <p className="text-sm text-muted-foreground">
              Cases requiring attention
            </p>
          </div>
          {rows.map((row) => (
            <button
              key={row.id}
              type="button"
              className="block w-full border-b px-4 py-3 text-left hover:bg-muted/40"
            >
              <div className="flex items-center justify-between gap-2">
                <p className="font-medium">{row.title}</p>
                <StatusBadge label={row.status} tone={row.tone} />
              </div>
              <p className="mt-1 text-sm text-muted-foreground">
                {row.subtitle}
              </p>
            </button>
          ))}
        </div>
        <div className="p-5">
          <span className="text-xs font-medium uppercase text-muted-foreground">
            Selected case
          </span>
          <h2 className="mt-2 text-lg font-semibold">{rows[0]?.title}</h2>
          <p className="mt-2 text-sm leading-6 text-muted-foreground">
            Review the customer history, add an internal note, then resolve,
            escalate, or issue the appropriate mock action.
          </p>
          <div className="mt-6 space-y-4 border-l pl-4 text-sm">
            <p>
              <span className="font-medium">09:12</span> · Case created by rider
            </p>
            <p>
              <span className="font-medium">09:20</span> · Assigned to support
              queue
            </p>
            <p>
              <span className="font-medium">Now</span> · Awaiting operator
              response
            </p>
          </div>
          <div className="mt-6 flex gap-2">
            <Button>Resolve case</Button>
            <Button variant="outline">Escalate</Button>
          </div>
        </div>
      </section>
    );
  if (view === "ledger")
    return (
      <section className="grid gap-4 xl:grid-cols-[minmax(0,1fr)_280px]">
        <article className="rounded-lg border bg-card p-5">
          <div className="flex items-center gap-2">
            <WalletCards className="size-4 text-primary" />
            <h2 className="font-semibold">Today’s settlement</h2>
          </div>
          <p className="mt-4 text-3xl font-semibold">Rs. 284,600</p>
          <p className="mt-1 text-sm text-muted-foreground">
            Completed fares, top-ups, and refunds
          </p>
          <div className="mt-6 grid grid-cols-3 gap-3 border-t pt-4 text-sm">
            <div>
              <p className="text-muted-foreground">Collected</p>
              <p className="mt-1 font-semibold">Rs. 312k</p>
            </div>
            <div>
              <p className="text-muted-foreground">Refunds</p>
              <p className="mt-1 font-semibold">Rs. 18k</p>
            </div>
            <div>
              <p className="text-muted-foreground">Pending</p>
              <p className="mt-1 font-semibold">Rs. 6k</p>
            </div>
          </div>
        </article>
        <article className="rounded-lg border bg-card p-5">
          <h2 className="font-semibold">Reconciliation</h2>
          <p className="mt-2 text-sm text-muted-foreground">
            2 exceptions require review before payout.
          </p>
          <Button className="mt-5 w-full" variant="outline">
            Review payouts <ArrowUpRight />
          </Button>
        </article>
      </section>
    );
  return (
    <section className="grid gap-4 lg:grid-cols-2">
      {[
        "Access & roles",
        "Connected services",
        "Operational preferences",
        "Audit activity",
      ].map((item, index) => (
        <article key={item} className="rounded-lg border bg-card p-5">
          <div className="flex items-center justify-between">
            <h2 className="font-semibold">{item}</h2>
            {index === 3 ? (
              <Clock3 className="size-4 text-muted-foreground" />
            ) : (
              <CheckCircle2 className="size-4 text-emerald-600" />
            )}
          </div>
          <p className="mt-2 text-sm leading-6 text-muted-foreground">
            Configure how this administrative workspace behaves. The current
            values are mock-only and ready for API integration.
          </p>
          <Button className="mt-4" size="sm" variant="outline">
            Manage
          </Button>
        </article>
      ))}
    </section>
  );
}
