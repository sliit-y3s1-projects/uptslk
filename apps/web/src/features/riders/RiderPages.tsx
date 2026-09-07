import { ResourcePage } from "@/features/resource/ResourcePage";
export function RidersPage() { return <ResourcePage title="Rider accounts" description="View customer accounts, balances, bookings, and account status." collection="riders" primaryAction="Add rider" view="cards" />; }
export function SupportPage() { return <ResourcePage title="Support" description="Resolve rider questions about bookings, payments, and service information." collection="support" primaryAction="Create case" view="inbox" />; }
