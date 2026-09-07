import { ResourcePage } from "@/features/resource/ResourcePage";
export function TicketsPage() { return <ResourcePage title="Bookings & tickets" description="Oversee ticket status, boarding records, cancellations, and refunds." collection="tickets" primaryAction="Create booking" view="inbox" />; }
export function PaymentsPage() { return <ResourcePage title="Payments" description="Review wallet activity, fare payments, refunds, and reconciliation." collection="payments" primaryAction="Record payment" view="ledger" />; }
