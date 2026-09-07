import { useState } from "react";
import { Check, Plus, X } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { PageHeading } from "@/components/shared/PageHeading";
import { StatusBadge } from "@/components/shared/StatusBadge";
import { useAuth } from "@/hooks/useAuth";
import { useDispatchMock } from "@/context/DispatchMockContext";
import type { DispatchApproval } from "@/mock/dispatch";

export function ApprovalsPage() {
  const { user } = useAuth();
  const { approvals, addApproval, decideApproval } = useDispatchMock();
  const [showForm, setShowForm] = useState(false);
  const scoped = approvals.filter((item) => item.centreId === user?.centreId);
  return <main className="flex flex-1 flex-col gap-4 bg-muted/20 p-4"><PageHeading title="Operational approvals" description="Review exceptional changes that fall outside normal dispatch rules." action={<Button onClick={() => setShowForm((value) => !value)}><Plus /> Request approval</Button>} />{showForm && <form className="rounded-lg border bg-card p-4" onSubmit={(event) => { event.preventDefault(); const form = new FormData(event.currentTarget); const approval: DispatchApproval = { id: `APR-${Date.now().toString().slice(-4)}`, centreId: user?.centreId ?? "makumbura", tripId: String(form.get("tripId")), request: String(form.get("request")), reason: String(form.get("reason")), status: "Pending", requestedBy: user?.name ?? "Centre manager" }; addApproval(approval); setShowForm(false); }}><div className="grid gap-3 md:grid-cols-3"><Input name="tripId" placeholder="Trip ID" required /><Input name="request" placeholder="Requested exception" required /><Input name="reason" placeholder="Operational reason" required /></div><div className="mt-3 flex justify-end gap-2"><Button type="button" variant="outline" onClick={() => setShowForm(false)}>Cancel</Button><Button type="submit">Submit request</Button></div></form>}<section className="overflow-hidden rounded-lg border bg-card"><div className="hidden grid-cols-[120px_150px_minmax(220px,1fr)_120px_200px] gap-3 border-b px-4 py-3 text-xs font-medium uppercase text-muted-foreground md:grid"><span>Request</span><span>Trip</span><span>Exception</span><span>Status</span><span>Decision</span></div>{scoped.map((request) => <div key={request.id} className="grid gap-3 border-b px-4 py-4 md:grid-cols-[120px_150px_minmax(220px,1fr)_120px_200px] md:items-center"><p className="font-medium">{request.id}</p><p className="text-sm">{request.tripId}</p><div><p className="text-sm font-medium">{request.request}</p><p className="text-xs text-muted-foreground">{request.reason} · {request.requestedBy}</p></div><StatusBadge label={request.status} tone={request.status === "Approved" ? "good" : request.status === "Rejected" ? "danger" : "warning"} />{request.status === "Pending" ? <div className="flex gap-2"><Button size="sm" onClick={() => decideApproval(request.id, "Approved")}><Check /> Approve</Button><Button size="sm" variant="outline" onClick={() => decideApproval(request.id, "Rejected")}><X /> Reject</Button></div> : <span className="text-sm text-muted-foreground">Decision recorded</span>}</div>)}</section></main>;
}
