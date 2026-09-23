import { useState } from "react";
import { Check, X } from "lucide-react";
import { Button } from "@/components/ui/button";
import { PageHeading } from "@/components/shared/PageHeading";
import { ResourceTable } from "@/components/shared/ResourceTable";
import {
  ResourceHighlights,
  type ResourceView,
} from "@/features/resource/ResourceViews";
import { useMockData } from "@/context/MockDataContext";
import { useOperationalScope } from "@/context/OperationalScopeContext";
import { useAuth } from "@/hooks/useAuth";
import { mockRowCentreId } from "@/mock/centre-scope";

type ResourcePageProps = {
  title: string;
  description: string;
  collection: string;
  primaryAction: string;
  approvalMode?: boolean;
  view?: ResourceView;
};

export function ResourcePage({
  title,
  description,
  collection,
  primaryAction,
  approvalMode = false,
  view = "table",
}: ResourcePageProps) {
  const [notice, setNotice] = useState<string | null>(null);
  const { data } = useMockData();
  const rows = data[collection] ?? [];
  const { dateRange } = useOperationalScope();
  const { user } = useAuth();
  const scopedRows = rows.filter(
    (row, index) =>
      !user?.centreId || mockRowCentreId(row, index) === user.centreId,
  );

  return (
    <main className="flex flex-1 flex-col gap-4 bg-muted/20 p-4">
      <PageHeading
        title={title}
        description={`${description} · ${dateRange}`}
        action={
          approvalMode ? (
            <div className="flex gap-2">
              <Button
                variant="outline"
                onClick={() =>
                  setNotice("Mock request sent back for revision.")
                }
              >
                <X /> Request revision
              </Button>
              <Button onClick={() => setNotice("Mock approval recorded.")}>
                <Check /> Approve selected
              </Button>
            </div>
          ) : undefined
        }
      />
      {notice && (
        <div className="flex items-center justify-between rounded-lg border border-primary/20 bg-primary/5 px-3 py-2 text-sm">
          <span>{notice}</span>
          <Button variant="ghost" size="sm" onClick={() => setNotice(null)}>
            Dismiss
          </Button>
        </div>
      )}
      <ResourceHighlights view={view} rows={scopedRows} />
      {view !== "settings" && (
        <ResourceTable
          rows={scopedRows}
          primaryAction={primaryAction}
          onPrimaryAction={() =>
            setNotice(`${primaryAction} is ready for its future form/API flow.`)
          }
        />
      )}
    </main>
  );
}
