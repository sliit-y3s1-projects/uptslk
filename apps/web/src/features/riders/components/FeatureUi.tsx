import { useId } from "react";
import type {
  ReactNode,
  SelectHTMLAttributes,
  InputHTMLAttributes,
} from "react";
import { Button } from "@/components/ui/button";
import { errorMessage } from "../services/request";
export function Panel({
  title,
  children,
}: {
  title: string;
  children: ReactNode;
}) {
  return (
    <section className="space-y-4 rounded-lg border bg-card p-5">
      <h2 className="text-base font-semibold">{title}</h2>
      {children}
    </section>
  );
}
export function Field({
  label,
  ...props
}: InputHTMLAttributes<HTMLInputElement> & { label: string }) {
  const generatedId = useId();
  const id = props.id ?? generatedId;
  return (
    <div className="flex min-w-0 flex-col gap-1.5 text-sm">
      <label htmlFor={id} className="font-medium">
        {label}
      </label>
      <input
        {...props}
        id={id}
        className="h-10 w-full rounded-md border bg-background px-3 disabled:opacity-60"
      />
    </div>
  );
}
export function SelectField({
  label,
  children,
  ...props
}: SelectHTMLAttributes<HTMLSelectElement> & { label: string }) {
  const generatedId = useId();
  const id = props.id ?? generatedId;
  return (
    <div className="flex min-w-0 flex-col gap-1.5 text-sm">
      <label htmlFor={id} className="font-medium">
        {label}
      </label>
      <select
        {...props}
        id={id}
        className="h-10 w-full rounded-md border bg-background px-3"
      >
        {children}
      </select>
    </div>
  );
}
export function CategoryOptions() {
  return (
    <>
      {["Adult", "Student", "Senior", "Child"].map((value) => (
        <option key={value}>{value}</option>
      ))}
    </>
  );
}
export function ActiveOptions() {
  return (
    <>
      <option value="">All statuses</option>
      <option value="true">Active</option>
      <option value="false">Inactive</option>
    </>
  );
}
export function Feedback({
  error,
  success,
}: {
  error?: unknown;
  success?: string | false;
}) {
  if (error)
    return (
      <p
        role="alert"
        className="rounded-md border border-destructive/30 bg-destructive/5 p-3 text-sm text-destructive"
      >
        {errorMessage(error)}
      </p>
    );
  return success ? (
    <p role="status" className="rounded-md border p-3 text-sm">
      {success}
    </p>
  ) : null;
}
export function QueryState({
  query,
  empty,
}: {
  query: { isPending: boolean; error: unknown; refetch: () => unknown };
  empty?: boolean;
}) {
  if (query.isPending)
    return (
      <p role="status" className="py-4 text-sm text-muted-foreground">
        Loading...
      </p>
    );
  if (query.error)
    return (
      <div className="space-y-2">
        <Feedback error={query.error} />
        <Button variant="outline" onClick={() => query.refetch()}>
          Try again
        </Button>
      </div>
    );
  return empty ? (
    <p className="py-4 text-sm text-muted-foreground">
      No records match this selection.
    </p>
  ) : null;
}
export function DataTable({
  headings,
  children,
}: {
  headings: string[];
  children: ReactNode;
}) {
  return (
    <div className="overflow-x-auto">
      <table className="w-full text-left text-sm">
        <thead className="border-b bg-muted/30">
          <tr>
            {headings.map((h) => (
              <th
                key={h}
                scope="col"
                className="whitespace-nowrap p-3 font-medium text-muted-foreground"
              >
                {h}
              </th>
            ))}
          </tr>
        </thead>
        <tbody className="[&_td]:p-3 [&_td]:whitespace-nowrap [&_tr]:border-b [&_tr:last-child]:border-0">
          {children}
        </tbody>
      </table>
    </div>
  );
}
