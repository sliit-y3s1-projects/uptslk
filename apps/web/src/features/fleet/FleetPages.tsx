import { ResourcePage } from "@/features/resource/ResourcePage";
export function DriversPage() {
  return (
    <ResourcePage
      title="Drivers"
      description="Manage driver records, duty status, licences, and trip assignments."
      collection="drivers"
      primaryAction="Add driver"
      view="cards"
    />
  );
}
export function MaintenancePage() {
  return (
    <ResourcePage
      title="Maintenance"
      description="Plan inspections and prevent unavailable vehicles from entering service."
      collection="maintenance"
      primaryAction="Schedule maintenance"
      view="board"
    />
  );
}
