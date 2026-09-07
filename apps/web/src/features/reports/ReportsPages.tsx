import { PageHeading } from "@/components/shared/PageHeading";
import { Button } from "@/components/ui/button";
import { Download } from "lucide-react";
import { useAuth } from "@/hooks/useAuth";
import { useOperationalScope } from "@/context/OperationalScopeContext";
import { centreFinance, passengerZones } from "@/mock/centre-operations";
import { centres } from "@/mock/centres";

function ReportPage({ title, description, focus }: { title: string; description: string; focus: string }) {
  const { user } = useAuth();
  const { dateRange } = useOperationalScope();
  const centre = centres.find((item) => item.id === user?.centreId) ?? centres[0];
  const finance = centreFinance[centre.id as keyof typeof centreFinance] ?? centreFinance.makumbura;
  const passengers = passengerZones.filter((item) => item.centreId === centre.id).reduce((sum, item) => sum + item.passengers, 0);
  const metrics = focus === "Daily boardings" ? [{ label: "Passengers now", value: passengers.toLocaleString(), change: "+8.4% vs prior period" }, { label: "Boardings", value: (passengers * 18).toLocaleString(), change: "+5.1%" }, { label: "Average occupancy", value: "68%", change: "+2.6 pts" }, { label: "Longest wait", value: "19 min", change: "Requires attention" }] : [{ label: "Gross revenue", value: `Rs. ${finance.collected.toLocaleString()}`, change: "+5.1% vs prior period" }, { label: "Digital share", value: `${Math.round(finance.digital / finance.collected * 100)}%`, change: "+3.2 pts" }, { label: "Refunds", value: `Rs. ${finance.refunds.toLocaleString()}`, change: "2.3% of revenue" }, { label: "Variance", value: `Rs. ${Math.abs(finance.variance).toLocaleString()}`, change: "Requires reconciliation" }];
  return <main className="flex flex-1 flex-col gap-4 bg-muted/20 p-4"><PageHeading title={title} description={`${description} ${centre.name} · ${dateRange}.`} action={<Button variant="outline"><Download /> Export CSV</Button>} /><section className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">{metrics.map((metric) => <article key={metric.label} className="rounded-lg border bg-card p-4"><p className="text-sm text-muted-foreground">{metric.label}</p><p className="mt-2 text-2xl font-semibold">{metric.value}</p><p className="mt-1 text-xs text-muted-foreground">{metric.change}</p></article>)}</section><section className="rounded-lg border bg-card p-5"><h2 className="font-semibold">{focus}</h2><div className="mt-5 flex h-48 items-end gap-3 border-b px-2">{[44, 61, 52, 73, 67, 84, 78, 92, 88, 96, 81, 90].map((height, index) => <div key={index} className="flex-1 rounded-t bg-primary/80" style={{ height: `${height}%` }} />)}</div><div className="mt-3 flex justify-between text-xs text-muted-foreground"><span>Period start</span><span>Today</span></div></section></main>;
}
export function RidershipPage() { return <ReportPage title="Ridership" description="Compare boardings, demand, occupancy, and route usage over time." focus="Daily boardings" />; }
export function RevenuePage() { return <ReportPage title="Revenue" description="Track collected fares, refunds, wallet top-ups, and payout exposure." focus="Daily fare revenue" />; }
