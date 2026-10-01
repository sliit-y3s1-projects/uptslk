import { useId } from "react";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";

export function LabeledSelect({
  label,
  value,
  onChange,
  options,
  placeholder = "Select",
  disabled = false,
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  options: { value: string; label: string }[];
  placeholder?: string;
  disabled?: boolean;
}) {
  const id = useId();
  return (
    <div className="min-w-0 space-y-1.5">
      <Label htmlFor={id}>{label}</Label>
      <Select
        value={value || null}
        disabled={disabled}
        onValueChange={(next) => onChange(next ?? "")}
        itemToStringLabel={(item) =>
          options.find((option) => option.value === item)?.label ?? item
        }
      >
        <SelectTrigger id={id} className="h-10 w-full">
          <SelectValue placeholder={placeholder} />
        </SelectTrigger>
        <SelectContent>
          {options.map((option) => (
            <SelectItem key={option.value} value={option.value}>
              {option.label}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>
    </div>
  );
}
