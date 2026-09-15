import { useCentreSelection } from "../hooks/useCentreSelection";
import {
  SelectField,
  QueryState,
} from "@/features/riders/components/FeatureUi";
export function CentrePicker({
  selected,
  onChange,
}: {
  selected: string;
  onChange: (id: string) => void;
}) {
  const { query, centreId } = useCentreSelection(selected);
  return (
    <div className="max-w-lg space-y-2">
      <SelectField
        label="Centre"
        value={centreId}
        onChange={(e) => onChange(e.target.value)}
      >
        <option value="">Select a centre</option>
        {query.data?.map((c) => (
          <option key={c.id} value={c.id}>
            {c.name} ({c.code})
          </option>
        ))}
      </SelectField>
      <QueryState query={query} empty={!query.data?.length} />
    </div>
  );
}
