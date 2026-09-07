import { useState } from "react";
import { Banknote, CheckCircle2, CreditCard, ReceiptText, TriangleAlert } from "lucide-react";
import { Button } from "@/components/ui/button";
import { PageHeading } from "@/components/shared/PageHeading";
import { useAuth } from "@/hooks/useAuth";
import { centreFinance } from "@/mock/centre-operations";

export function ReconciliationPage() {
  const { user } = useAuth();
  const data = centreFinance[user?.centreId as keyof typeof centreFinance] ?? centreFinance.makumbura;
  const [closed, setClosed] = useState(false);
  const money = (value: number) => `Rs. ${Math.abs(value).toLocaleString("en-LK")}`;
  return <main className="flex flex-1 flex-col gap-4 bg-muted/20 p-4"><PageHeading title="Daily reconciliation" description="Compare cash, digital collections, refunds, and the expected centre settlement." action={<Button disabled={closed} onClick={() => setClosed(true)}><CheckCircle2 /> {closed ? "Day closed" : "Close business day"}</Button>} /><section className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4"><Metric icon={ReceiptText} label="Gross collected" value={money(data.collected)} /><Metric icon={Banknote} label="Cash" value={money(data.cash)} /><Metric icon={CreditCard} label="Digital" value={money(data.digital)} /><Metric icon={TriangleAlert} label="Variance" value={`${data.variance < 0 ? "−" : "+"}${money(data.variance)}`} warning={data.variance !== 0} /></section><section className="grid gap-4 xl:grid-cols-[minmax(0,1fr)_340px]"><article className="rounded-lg border bg-card p-5"><h2 className="font-semibold">Settlement summary</h2><div className="mt-5 space-y-4"><Row label="Gross collections" value={money(data.collected)} /><Row label="Less refunds" value={`− ${money(data.refunds)}`} /><Row label="Recorded variance" value={`${data.variance < 0 ? "−" : "+"} ${money(data.variance)}`} /><Row label="Expected settlement" value={money(data.collected - data.refunds + data.variance)} strong /></div></article><article className="rounded-lg border bg-card p-5"><h2 className="font-semibold">Control status</h2><p className="mt-2 text-sm text-muted-foreground">{data.transactions.toLocaleString()} transactions processed today.</p><div className={`mt-5 rounded-md border p-4 text-sm ${closed ? "border-emerald-200 bg-emerald-50 text-emerald-800" : "border-amber-200 bg-amber-50 text-amber-900"}`}>{closed ? "Reconciliation completed and locked for review." : "Variance requires explanation before the business day is closed."}</div><Button className="mt-4 w-full" variant="outline">Export settlement report</Button></article></section></main>;
}

function Metric({ icon: Icon, label, value, warning }: { icon: typeof Banknote; label: string; value: string; warning?: boolean }) { return <article className="rounded-lg border bg-card p-4"><Icon className={warning ? "size-4 text-amber-600" : "size-4 text-primary"} /><p className="mt-3 text-sm text-muted-foreground">{label}</p><p className="mt-1 text-xl font-semibold">{value}</p></article>; }
function Row({ label, value, strong }: { label: string; value: string; strong?: boolean }) { return <div className={`flex justify-between border-b pb-3 text-sm ${strong ? "text-base font-semibold" : ""}`}><span className="text-muted-foreground">{label}</span><span>{value}</span></div>; }
