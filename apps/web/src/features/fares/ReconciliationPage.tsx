import { PassengerWorkspace } from "@/features/riders/RiderPages";
export function ReconciliationPage() {
  return (
    <PassengerWorkspace
      title="Financial transaction review"
      description="Review each passenger's wallet ledger, fare charges and refund records. Centre settlement and day-closing are not available from the current API."
    />
  );
}
