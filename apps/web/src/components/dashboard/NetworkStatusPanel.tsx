import { HugeiconsIcon } from "@hugeicons/react";
import { ArrowUpRight01Icon, MapPinIcon } from "@hugeicons/core-free-icons";

import { Button } from "@/components/ui/button";

type NetworkStatusItem = {
  region: string;
  routes: string;
  status: string;
  load: string;
};

type NetworkStatusPanelProps = {
  items: NetworkStatusItem[];
};

export function NetworkStatusPanel({ items }: NetworkStatusPanelProps) {
  return (
    <article className="overflow-hidden rounded-lg border bg-card">
      <div className="flex items-center justify-between gap-3 border-b px-4 py-3">
        <div>
          <h2 className="text-sm font-semibold">Network status</h2>
          <p className="text-xs text-muted-foreground">
            Corridor health by operating region
          </p>
        </div>

        <Button variant="outline" size="sm">
          <HugeiconsIcon icon={MapPinIcon} strokeWidth={2} className="size-4" />
          Map
        </Button>
      </div>

      <div className="divide-y">
        {items.map((item) => (
          <div
            key={item.region}
            className="grid gap-3 px-4 py-3 text-sm sm:grid-cols-[1fr_120px_88px_24px] sm:items-center"
          >
            <div>
              <p className="font-medium">{item.region}</p>
              <p className="text-xs text-muted-foreground">{item.routes}</p>
            </div>
            <p className="text-muted-foreground">{item.status}</p>
            <p className="font-medium tabular-nums">{item.load}</p>
            <HugeiconsIcon
              icon={ArrowUpRight01Icon}
              strokeWidth={2}
              className="hidden size-4 text-muted-foreground sm:block"
            />
          </div>
        ))}
      </div>
    </article>
  );
}
