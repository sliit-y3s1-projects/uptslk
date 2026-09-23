import { useMemo, useState } from "react";
import { Check, ChevronDown, Clock3 } from "lucide-react";
import { Button } from "@/components/ui/button";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover";
import { cn } from "@/lib/utils";

type TimePickerProps = {
  name: string;
  defaultValue?: string;
  value?: string;
  onValueChange?: (value: string) => void;
  required?: boolean;
  className?: string;
};

const hours = Array.from({ length: 12 }, (_, index) =>
  String(index + 1).padStart(2, "0"),
);
const minutes = Array.from({ length: 60 }, (_, index) =>
  String(index).padStart(2, "0"),
);

function parseTime(value?: string) {
  if (!value) return { hour: "", minute: "", period: "AM" as const };
  const [hourPart, minute = "00"] = value.split(":");
  const hour24 = Number(hourPart);
  return {
    hour: String(hour24 % 12 || 12).padStart(2, "0"),
    minute,
    period: hour24 >= 12 ? ("PM" as const) : ("AM" as const),
  };
}

function toTimeValue(hour: string, minute: string, period: "AM" | "PM") {
  if (!hour || !minute) return "";
  let hour24 = Number(hour) % 12;
  if (period === "PM") hour24 += 12;
  return `${String(hour24).padStart(2, "0")}:${minute}`;
}

/** Theme-matched time selector that submits a standard HTML HH:mm value. */
export function TimePicker({
  name,
  defaultValue,
  value,
  onValueChange,
  required,
  className,
}: TimePickerProps) {
  const [internalValue, setInternalValue] = useState(defaultValue ?? "");
  const selectedValue = value ?? internalValue;
  const selection = useMemo(() => parseTime(selectedValue), [selectedValue]);

  const update = (next: Partial<typeof selection>) => {
    const nextSelection = { ...selection, ...next };
    const nextValue = toTimeValue(
      nextSelection.hour,
      nextSelection.minute,
      nextSelection.period,
    );
    if (value === undefined) setInternalValue(nextValue);
    onValueChange?.(nextValue);
  };

  const displayValue = selectedValue
    ? `${selection.hour}:${selection.minute} ${selection.period}`
    : "Select departure time";

  return (
    <Popover>
      <input
        name={name}
        type="hidden"
        value={selectedValue}
        required={required}
      />
      <PopoverTrigger
        render={
          <Button
            type="button"
            variant="outline"
            className={cn(
              "h-10 w-full justify-between rounded-lg bg-background px-3 font-normal text-foreground shadow-xs hover:bg-accent/40",
              !selectedValue && "text-muted-foreground",
              className,
            )}
          />
        }
      >
        <span className="flex items-center gap-2">
          <Clock3 className="size-4 text-muted-foreground" />
          {displayValue}
        </span>
        <ChevronDown className="size-4 text-muted-foreground" />
      </PopoverTrigger>
      <PopoverContent
        align="start"
        className="w-[300px] rounded-xl border-border p-2 shadow-xl"
      >
        <p className="px-2 pb-2 text-xs font-medium text-muted-foreground">
          Departure time
        </p>
        <div className="grid grid-cols-3 gap-1 border-t pt-2">
          <TimeColumn
            label="Hour"
            values={hours}
            selected={selection.hour}
            onSelect={(hour) => update({ hour })}
          />
          <TimeColumn
            label="Minute"
            values={minutes}
            selected={selection.minute}
            onSelect={(minute) => update({ minute })}
          />
          <TimeColumn
            label="Period"
            values={["AM", "PM"]}
            selected={selection.period}
            onSelect={(period) => update({ period: period as "AM" | "PM" })}
          />
        </div>
      </PopoverContent>
    </Popover>
  );
}

function TimeColumn({
  label,
  values,
  selected,
  onSelect,
}: {
  label: string;
  values: string[];
  selected: string;
  onSelect: (value: string) => void;
}) {
  return (
    <div className="min-w-0">
      <p className="px-2 pb-1 text-xs text-muted-foreground">{label}</p>
      <div className="h-44 space-y-0.5 overflow-y-auto rounded-md bg-muted/40 p-1 [-ms-overflow-style:none] [scrollbar-width:none] [&::-webkit-scrollbar]:hidden">
        {values.map((option) => (
          <button
            key={option}
            type="button"
            onClick={() => onSelect(option)}
            className={cn(
              "flex h-8 w-full items-center justify-between rounded-md px-2 text-sm transition-colors hover:bg-accent",
              selected === option &&
                "bg-primary text-primary-foreground hover:bg-primary",
            )}
          >
            {option}
            {selected === option && <Check className="size-3.5" />}
          </button>
        ))}
      </div>
    </div>
  );
}
