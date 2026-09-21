import { useState } from "react";
import { Button } from "@/components/ui/button";
import { PageHeading } from "@/components/shared/PageHeading";
import { useSupportRequestMutations, useSupportRequests } from "./hooks/useSupportRequests";
import type { SupportRequestPriority, SupportRequestStatus, SupportRequestType } from "./types/support";
import { Panel, SelectField, Field, QueryState, DataTable, Feedback } from "./components/FeatureUi";
import { usePassengers } from "./hooks/usePassengers";

export function RequestQueue({ type, title, description }: { type: SupportRequestType; title: string; description: string }) {
  const query = useSupportRequests(type);
  const passengers = usePassengers({ active: "true" });
  const mutations = useSupportRequestMutations();
  const [subject, setSubject] = useState("");
  const [details, setDetails] = useState("");
  const [priority, setPriority] = useState<SupportRequestPriority>("Medium");
  const [passengerId, setPassengerId] = useState("");
  const [notice, setNotice] = useState("");
  const create = () => {
    if (!subject.trim() || !details.trim()) return;
    mutations.create.mutate({ type, priority, subject, description: details, ...(passengerId ? { passengerId } : {}) }, { onSuccess: () => { setSubject(""); setDetails(""); setPassengerId(""); setNotice("Request created."); } });
  };
  const updateStatus = (request: (typeof query.data extends (infer T)[] | undefined ? T : never), status: SupportRequestStatus) => {
    mutations.update.mutate({ id: request.id, body: { priority: request.priority, status, subject: request.subject, description: request.description, assignedTo: request.assignedTo ?? undefined, resolution: status === "Resolved" ? "Resolved by operations team." : request.resolution ?? undefined } });
  };
  return <main className="flex flex-1 flex-col gap-4 bg-muted/20 p-4">
    <PageHeading title={title} description={description} />
    <Feedback error={query.error || mutations.create.error || mutations.update.error} success={notice} />
    <Panel title="Create request">
      <div className="grid gap-3 md:grid-cols-[2fr_1fr]">
        <Field label="Subject" value={subject} onChange={(event) => setSubject(event.target.value)} placeholder="What does the passenger need?" />
        <SelectField label="Priority" value={priority} onChange={(event) => setPriority(event.target.value as SupportRequestPriority)}>{(["Low", "Medium", "High", "Urgent"] as SupportRequestPriority[]).map((item) => <option key={item}>{item}</option>)}</SelectField>
      </div>
      <SelectField label="Passenger (optional)" value={passengerId} onChange={(event) => setPassengerId(event.target.value)}>
        <option value="">Unlinked request</option>
        {passengers.data?.map((passenger) => <option key={passenger.id} value={passenger.id}>{passenger.fullName} · {passenger.phoneNumber}</option>)}
      </SelectField>
      <Field label="Details" value={details} onChange={(event) => setDetails(event.target.value)} placeholder="Add context for the operations team" />
      <Button onClick={create} disabled={!subject.trim() || !details.trim() || mutations.create.isPending}>{mutations.create.isPending ? "Creating..." : "Create request"}</Button>
    </Panel>
    <Panel title="Request queue">
      <QueryState query={query} empty={!query.data?.length} />
      {query.data && query.data.length > 0 && <DataTable headings={["Request", "Passenger", "Priority", "Status", "Created", "Action"]}>{query.data.map((request) => <tr key={request.id}>
        <td><p className="font-medium">{request.subject}</p><p className="max-w-sm truncate text-xs text-muted-foreground">{request.description}</p></td>
        <td>{request.passenger ?? "Unlinked"}</td><td>{request.priority}</td><td>{request.status}</td><td>{new Date(request.createdAt).toLocaleString()}</td>
        <td className="space-x-2">{request.status === "Open" && <Button size="sm" variant="outline" onClick={() => updateStatus(request, "InProgress")}>Assign</Button>}{request.status === "InProgress" && <Button size="sm" variant="outline" onClick={() => updateStatus(request, "Resolved")}>Resolve</Button>}{request.status === "Resolved" && <Button size="sm" variant="outline" onClick={() => updateStatus(request, "Closed")}>Close</Button>}</td>
      </tr>)}</DataTable>}
    </Panel>
  </main>;
}
