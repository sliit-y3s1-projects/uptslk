import { useCentreSelection } from "../hooks/useCentreSelection";
import { QueryState } from "@/features/riders/components/FeatureUi";
import { LabeledSelect } from "@/components/shared/LabeledSelect";
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
      <LabeledSelect
        label="Centre"
        value={centreId}
        onChange={onChange}
        placeholder="Select a centre"
        options={(query.data ?? []).map((c) => ({
          value: c.id,
          label: `${c.name} (${c.code})`,
        }))}
      />
      <QueryState query={query} empty={!query.data?.length} />
    </div>
  );
}
