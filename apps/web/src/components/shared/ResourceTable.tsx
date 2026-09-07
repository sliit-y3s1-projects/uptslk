import { useMemo, useState } from "react";
import { ArrowUpDown, ChevronLeft, ChevronRight, Search } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { StatusBadge } from "@/components/shared/StatusBadge";
import type { TableRow } from "@/mock/mock-data";

export function ResourceTable({ rows, primaryAction = "Add record", onPrimaryAction, detailPath }: { rows: TableRow[]; primaryAction?: string; onPrimaryAction?: () => void; detailPath?: (row: TableRow) => string }) {
  const [query, setQuery] = useState("");
  const [status, setStatus] = useState("All");
  const [sort, setSort] = useState<"name" | "status">("name");
  const [page, setPage] = useState(1);
  const pageSize = 10;
  const statuses = [...new Set(rows.map((row) => row.status))];
  const filteredRows = useMemo(() => rows.filter((row) => `${row.title} ${row.subtitle} ${row.id}`.toLowerCase().includes(query.toLowerCase()) && (status === "All" || row.status === status)).sort((a, b) => sort === "name" ? a.title.localeCompare(b.title) : a.status.localeCompare(b.status)), [query, rows, sort, status]);
  const totalPages = Math.max(1, Math.ceil(filteredRows.length / pageSize));
  const currentPage = Math.min(page, totalPages);
  const visibleRows = filteredRows.slice((currentPage - 1) * pageSize, currentPage * pageSize);
  function updateFilter(callback: () => void) { callback(); setPage(1); }
  return <section className="overflow-hidden rounded-lg border bg-card"><div className="border-b p-3"><div className="flex flex-col gap-3 xl:flex-row xl:items-center xl:justify-between"><div className="flex flex-1 flex-col gap-2 sm:flex-row"><div className="relative min-w-0 flex-1 sm:max-w-sm"><Search className="pointer-events-none absolute left-3 top-2.5 size-4 text-muted-foreground" /><Input value={query} onChange={(event) => updateFilter(() => setQuery(event.target.value))} placeholder="Search name, ID, route, or vehicle" className="pl-9" /></div><div className="flex gap-2"><Select value={status} onValueChange={(value) => { if (value) updateFilter(() => setStatus(value)); }}><SelectTrigger className="min-w-36 bg-muted/60"><SelectValue /></SelectTrigger><SelectContent><SelectItem value="All">All statuses</SelectItem>{statuses.map((option) => <SelectItem key={option} value={option}>{option}</SelectItem>)}</SelectContent></Select><button type="button" aria-label="Change sort order" onClick={() => setSort((value) => value === "name" ? "status" : "name")} className="inline-flex size-9 items-center justify-center rounded-md border bg-muted/60 hover:bg-muted"><ArrowUpDown className="size-4" /></button></div></div><Button onClick={onPrimaryAction}>{primaryAction}</Button></div><p className="mt-3 text-xs text-muted-foreground">{filteredRows.length} matching records in this centre workspace.</p></div><div className="divide-y">{visibleRows.map((row) => <article key={row.id} className="grid gap-2 p-4 md:grid-cols-[minmax(0,1.5fr)_130px_150px_120px] md:items-center"><div className="min-w-0">{detailPath ? <a href={detailPath(row)} className="font-medium hover:underline">{row.title}</a> : <p className="font-medium">{row.title}</p>}<p className="mt-1 truncate text-sm text-muted-foreground">{row.id} · {row.subtitle}</p></div><StatusBadge label={row.status} tone={row.tone} /><p className="text-sm text-muted-foreground">{row.updated}</p><p className="text-sm font-medium md:text-right">{row.meta}</p></article>)}{visibleRows.length === 0 && <p className="p-8 text-center text-sm text-muted-foreground">No mock records match these filters.</p>}</div><footer className="flex items-center justify-between border-t px-4 py-3 text-sm"><span className="text-muted-foreground">Page {currentPage} of {totalPages}</span><div className="flex gap-2"><Button size="icon" variant="outline" aria-label="Previous page" disabled={currentPage === 1} onClick={() => setPage((value) => Math.max(1, value - 1))}><ChevronLeft /></Button><Button size="icon" variant="outline" aria-label="Next page" disabled={currentPage === totalPages} onClick={() => setPage((value) => Math.min(totalPages, value + 1))}><ChevronRight /></Button></div></footer></section>;
}
