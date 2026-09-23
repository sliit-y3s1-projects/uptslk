import { useMemo, useState } from "react";
import {
  KeyRound,
  Loader2,
  Pencil,
  Search,
  ShieldCheck,
  UserCheck,
  UserRound,
  UserX,
} from "lucide-react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from "@/components/ui/dialog";
import { apiClient } from "@/lib/api/api-client";

type Account = {
  id: string;
  name: string;
  email: string;
  role: string;
  centreId?: string | null;
  isActive: boolean;
};

export function UsersPage() {
  const [query, setQuery] = useState("");
  const [resetUser, setResetUser] = useState<Account | null>(null);
  const [editUser, setEditUser] = useState<Account | null>(null);
  const [password, setPassword] = useState("");
  const [editName, setEditName] = useState("");
  const [editEmail, setEditEmail] = useState("");
  const client = useQueryClient();
  const { data: users = [], isLoading } = useQuery({
    queryKey: ["admin", "users"],
    queryFn: () => apiClient<Account[]>("/api/v1/auth/users"),
  });
  const status = useMutation({
    mutationFn: ({ id, active }: { id: string; active: boolean }) =>
      apiClient(`/api/v1/auth/users/${id}/status`, {
        method: "PATCH",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(active),
      }),
    onSuccess: () => client.invalidateQueries({ queryKey: ["admin", "users"] }),
  });
  const reset = useMutation({
    mutationFn: ({ id, value }: { id: string; value: string }) =>
      apiClient(`/api/v1/auth/users/${id}/reset-password`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(value),
      }),
    onSuccess: () => {
      setResetUser(null);
      setPassword("");
    },
  });
  const updateDetails = useMutation({
    mutationFn: ({
      id,
      name,
      email,
    }: {
      id: string;
      name: string;
      email: string;
    }) =>
      apiClient<Account>(`/api/v1/auth/users/${id}`, {
        method: "PATCH",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ name, email }),
      }),
    onSuccess: () => {
      client.invalidateQueries({ queryKey: ["admin", "users"] });
      setEditUser(null);
      setEditName("");
      setEditEmail("");
    },
  });
  function openEdit(user: Account) {
    setEditUser(user);
    setEditName(user.name);
    setEditEmail(user.email);
  }
  const filtered = useMemo(
    () =>
      users.filter((user) =>
        `${user.name} ${user.email} ${user.role}`
          .toLowerCase()
          .includes(query.toLowerCase()),
      ),
    [query, users],
  );
  return (
    <main className="flex flex-1 flex-col gap-5 bg-muted/20 p-5">
      <div>
        <h1 className="text-2xl font-semibold">User directory</h1>
        <p className="mt-1 text-sm text-muted-foreground">
          Manage every UPTSLK identity, including commuters and staff.
        </p>
      </div>
      <section className="rounded-lg border bg-card p-3">
        <div className="relative max-w-xl">
          <Search className="absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            className="pl-9"
            placeholder="Search name, email, or role"
            value={query}
            onChange={(event) => setQuery(event.target.value)}
          />
        </div>
      </section>
      <section className="overflow-hidden rounded-lg border bg-card">
        {isLoading ? (
          <div className="flex justify-center p-10">
            <Loader2 className="animate-spin" />
          </div>
        ) : (
          filtered.map((user) => (
            <div
              key={user.id}
              className="flex flex-wrap items-center gap-4 border-b p-4 last:border-0"
            >
              <div className="flex min-w-0 flex-1 items-center gap-3">
                <div className="flex size-9 shrink-0 items-center justify-center rounded-full bg-primary/10 text-primary">
                  <UserRound className="size-4" />
                </div>
                <div className="min-w-0">
                  <p className="truncate font-medium">{user.name}</p>
                  <p className="truncate text-sm text-muted-foreground">
                    {user.email}
                  </p>
                </div>
              </div>
              <span className="inline-flex items-center gap-1 text-sm">
                <ShieldCheck className="size-4 text-muted-foreground" />
                {user.role}
              </span>
              <span
                className={`rounded-full px-2.5 py-1 text-xs font-medium ${user.isActive ? "bg-emerald-50 text-emerald-700" : "bg-red-50 text-red-700"}`}
              >
                {user.isActive ? "Active" : "Disabled"}
              </span>
              <Button
                variant="outline"
                size="sm"
                className="rounded-full border-sky-200 bg-sky-50 text-sky-800 hover:bg-sky-100 hover:text-sky-900"
                onClick={() => openEdit(user)}
              >
                <Pencil /> Edit
              </Button>
              <Button
                variant={user.isActive ? "destructive" : "outline"}
                size="sm"
                className={
                  user.isActive
                    ? "rounded-full"
                    : "rounded-full border-emerald-200 bg-emerald-50 text-emerald-700 hover:bg-emerald-100 hover:text-emerald-800"
                }
                disabled={status.isPending}
                onClick={() =>
                  status.mutate({ id: user.id, active: !user.isActive })
                }
              >
                {user.isActive ? <UserX /> : <UserCheck />}
                {user.isActive ? "Disable" : "Enable"}
              </Button>
              <Button
                size="sm"
                className="rounded-full border-amber-200 bg-amber-50 text-amber-800 hover:bg-amber-100 hover:text-amber-900"
                onClick={() => setResetUser(user)}
              >
                <KeyRound />
                Reset password
              </Button>
            </div>
          ))
        )}
        {!isLoading && filtered.length === 0 && (
          <p className="p-8 text-center text-sm text-muted-foreground">
            No accounts match your search.
          </p>
        )}
      </section>
      <Dialog
        open={!!editUser}
        onOpenChange={(open) => {
          if (!open) setEditUser(null);
        }}
      >
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Edit user details</DialogTitle>
            <DialogDescription>
              Update the account name and sign-in email for {editUser?.name}.
            </DialogDescription>
          </DialogHeader>
          <div className="grid gap-4">
            <label className="grid gap-1.5 text-sm font-medium">
              Full name
              <Input
                value={editName}
                onChange={(event) => setEditName(event.target.value)}
                placeholder="Full name"
              />
            </label>
            <label className="grid gap-1.5 text-sm font-medium">
              Email address
              <Input
                type="email"
                value={editEmail}
                onChange={(event) => setEditEmail(event.target.value)}
                placeholder="name@upts.lk"
              />
            </label>
            {updateDetails.error && (
              <p className="text-sm text-destructive">
                Could not save the user details. Make sure the email is not
                already in use.
              </p>
            )}
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setEditUser(null)}>
              Cancel
            </Button>
            <Button
              disabled={
                !editUser ||
                !editName.trim() ||
                !editEmail.trim() ||
                updateDetails.isPending
              }
              onClick={() =>
                editUser &&
                updateDetails.mutate({
                  id: editUser.id,
                  name: editName.trim(),
                  email: editEmail.trim(),
                })
              }
            >
              {updateDetails.isPending ? "Saving..." : "Save changes"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
      <Dialog
        open={!!resetUser}
        onOpenChange={(open) => !open && setResetUser(null)}
      >
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Reset account password</DialogTitle>
            <DialogDescription>
              Set a temporary password for {resetUser?.name}.
            </DialogDescription>
          </DialogHeader>
          <Input
            type="password"
            placeholder="At least 8 characters"
            minLength={8}
            value={password}
            onChange={(event) => setPassword(event.target.value)}
          />
          <DialogFooter>
            <Button variant="outline" onClick={() => setResetUser(null)}>
              Cancel
            </Button>
            <Button
              disabled={password.length < 8 || reset.isPending}
              onClick={() =>
                resetUser && reset.mutate({ id: resetUser.id, value: password })
              }
            >
              {reset.isPending ? "Updating..." : "Reset password"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </main>
  );
}
