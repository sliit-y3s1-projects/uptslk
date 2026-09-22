import { useEffect, useState } from "react";
import { CalendarDays } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Calendar } from "@/components/ui/calendar";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import { cn } from "@/lib/utils";

type DatePickerProps = {
  name: string;
  defaultValue?: string;
  value?: string;
  onValueChange?: (value: string) => void;
  required?: boolean;
  className?: string;
};

const toDateValue = (day: Date) =>
  `${day.getFullYear()}-${String(day.getMonth() + 1).padStart(2, "0")}-${String(day.getDate()).padStart(2, "0")}`;

export function DatePicker({ name, defaultValue, value, onValueChange, required, className }: DatePickerProps) {
  const [internalValue, setInternalValue] = useState(defaultValue ?? "");
  const selectedValue = value ?? internalValue;
  const selected = selectedValue ? new Date(`${selectedValue}T00:00:00`) : undefined;

  useEffect(() => {
    if (value === undefined) setInternalValue(defaultValue ?? "");
  }, [defaultValue, value]);

  return (
    <Popover>
      <input name={name} type="hidden" value={selectedValue} required={required} />
      <PopoverTrigger render={<Button type="button" variant="outline" className={cn("h-10 w-full justify-start rounded-lg font-normal", !selected && "text-muted-foreground", className)} />}>
        <CalendarDays className="mr-2 size-4" />
        {selected ? selected.toLocaleDateString(undefined, { weekday: "short", day: "numeric", month: "short", year: "numeric" }) : "Select date"}
      </PopoverTrigger>
      <PopoverContent align="start" className="w-auto p-0">
        <Calendar
          mode="single"
          selected={selected}
          onSelect={(day) => {
            if (!day) return;
            const next = toDateValue(day);
            if (value === undefined) setInternalValue(next);
            onValueChange?.(next);
          }}
        />
      </PopoverContent>
    </Popover>
  );
}
