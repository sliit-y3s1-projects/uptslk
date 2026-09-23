import { Building2, CalendarDays, RotateCcw } from "lucide-react";
import { Button } from "@/components/ui/button";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { useOperationalScope } from "@/context/OperationalScopeContext";
import { useAuth } from "@/hooks/useAuth";
import { centres } from "@/mock/centres";

export function OperationalScopeBar() {
  const { user } = useAuth();
  const { district, setDistrict, dateRange, setDateRange, resetScope } =
    useOperationalScope();
  const centre = centres.find((item) => item.id === user?.centreId);
  const canChangeCentre = !centre;
  return (
    <div className="flex flex-col gap-2 border-b bg-card px-4 py-2 sm:flex-row sm:items-center">
      <div className="flex items-center gap-2 text-xs font-medium text-muted-foreground">
        <CalendarDays className="size-4" /> Workspace scope
      </div>
      <div className="flex flex-1 flex-wrap gap-2">
        {centre ? (
          <div className="flex h-8 min-w-44 items-center gap-2 rounded-md border bg-muted/60 px-2.5 text-xs font-medium">
            <Building2 className="size-3.5 text-primary" />
            {centre.name}
          </div>
        ) : (
          <Select
            value={district}
            onValueChange={(value) => {
              if (value) setDistrict(value);
            }}
          >
            <SelectTrigger
              size="sm"
              aria-label="Multimodal centre scope"
              className="min-w-48 bg-muted/60 text-xs"
            >
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="All">All centres</SelectItem>
              {centres.map((item) => (
                <SelectItem key={item.id} value={item.district}>
                  {item.name}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        )}
        <Select
          value={dateRange}
          onValueChange={(value) => {
            if (value) setDateRange(value);
          }}
        >
          <SelectTrigger
            size="sm"
            aria-label="Date scope"
            className="min-w-28 bg-muted/60 text-xs"
          >
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="Today">Today</SelectItem>
            <SelectItem value="Week">This week</SelectItem>
            <SelectItem value="Month">This month</SelectItem>
          </SelectContent>
        </Select>
      </div>
      {((canChangeCentre && district !== "All") || dateRange !== "Today") && (
        <Button
          size="sm"
          variant="ghost"
          className="h-8 text-xs"
          onClick={resetScope}
        >
          <RotateCcw /> Reset
        </Button>
      )}
    </div>
  );
}
