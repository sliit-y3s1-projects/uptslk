type PriorityAlert = {
  label: string;
  detail: string;
  level: string;
};

type PriorityAlertsPanelProps = {
  items: PriorityAlert[];
};

export function PriorityAlertsPanel({ items }: PriorityAlertsPanelProps) {
  return (
    <aside className="overflow-hidden rounded-lg border bg-card">
      <div className="border-b px-4 py-3">
        <h2 className="text-sm font-semibold">Priority alerts</h2>
        <p className="text-xs text-muted-foreground">
          Items needing operator attention
        </p>
      </div>

      <div className="divide-y">
        {items.map((alert) => (
          <div key={alert.label} className="px-4 py-3">
            <div className="flex items-center justify-between gap-3">
              <p className="text-sm font-medium">{alert.label}</p>
              <span className="rounded-md border px-2 py-0.5 text-xs text-muted-foreground">
                {alert.level}
              </span>
            </div>
            <p className="mt-1 text-sm leading-6 text-muted-foreground">
              {alert.detail}
            </p>
          </div>
        ))}
      </div>
    </aside>
  );
}
