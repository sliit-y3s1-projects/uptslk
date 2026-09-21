import { useState } from "react";
import { Link } from "react-router";
import { Button } from "@/components/ui/button";
import { usePassenger, usePassengerMutations } from "../hooks/usePassengers";
import type {
  PassengerDetail,
  PassengerRequest,
  PassengerCategory,
} from "../types/passengers";
import {
  Panel,
  Field,
  SelectField,
  CategoryOptions,
  Feedback,
  QueryState,
  DataTable,
} from "./FeatureUi";
import { money, dateTime } from "./format";

export function PassengerForm({
  passenger,
  onSaved,
  onClose,
}: {
  passenger?: PassengerDetail;
  onSaved: (id: string) => void;
  onClose: () => void;
}) {
  const { create, update } = usePassengerMutations();
  const [fullName, setFullName] = useState(passenger?.fullName ?? "");
  const [phoneNumber, setPhoneNumber] = useState(passenger?.phoneNumber ?? "");
  const [email, setEmail] = useState(passenger?.email ?? "");
  const [password, setPassword] = useState("");
  const [category, setCategory] = useState<PassengerCategory>(
    passenger?.category ?? "Adult",
  );
  const [isActive, setActive] = useState(passenger?.isActive ?? true);
  const mutation = passenger ? update : create;
  return (
    <Panel title={passenger ? "Edit passenger" : "Add passenger"}>
      <form
        className="space-y-4"
        onSubmit={(event) => {
          event.preventDefault();
          const body: PassengerRequest = {
            fullName: fullName.trim(),
            phoneNumber: phoneNumber.trim(),
            email: email.trim() || null,
            ...(passenger || !password ? {} : { password }),
            category,
          };
          if (!body.fullName || !body.phoneNumber) return;
          if (passenger)
            update.mutate(
              { id: passenger.id, body: { ...body, isActive } },
              { onSuccess: () => onSaved(passenger.id) },
            );
          else
            create.mutate(body, { onSuccess: (result) => onSaved(result.id) });
        }}
      >
        <fieldset disabled={mutation.isPending} className="space-y-4">
          <div className="grid gap-4 sm:grid-cols-2">
            <Field
              label="Full name"
              required
              maxLength={160}
              value={fullName}
              onChange={(e) => setFullName(e.target.value)}
            />
            <Field
              label="Phone number"
              type="tel"
              required
              maxLength={32}
              value={phoneNumber}
              onChange={(e) => setPhoneNumber(e.target.value)}
            />
            <Field
              label="Email (optional)"
              type="email"
              maxLength={256}
              value={email}
              onChange={(e) => setEmail(e.target.value)}
            />
            {!passenger && (
              <Field
                label="Portal password (optional)"
                type="password"
                minLength={8}
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder="At least 8 characters"
              />
            )}
            <SelectField
              label="Category"
              value={category}
              onChange={(e) => setCategory(e.target.value as PassengerCategory)}
            >
              <CategoryOptions />
            </SelectField>
          </div>
          {passenger && (
            <label className="flex items-center gap-2 text-sm">
              <input
                type="checkbox"
                checked={isActive}
                onChange={(e) => setActive(e.target.checked)}
              />
              Active account
            </label>
          )}
          <Feedback error={mutation.error} />
          <div className="flex gap-2">
            <Button
              type="submit"
              disabled={!fullName.trim() || !phoneNumber.trim()}
            >
              {mutation.isPending ? "Saving..." : "Save passenger"}
            </Button>
            <Button type="button" variant="outline" onClick={onClose}>
              Cancel
            </Button>
          </div>
        </fieldset>
      </form>
    </Panel>
  );
}

export function PassengerProfile({ id }: { id: string }) {
  const query = usePassenger(id);
  const { deactivate, restore, resetPassword, topUp } = usePassengerMutations();
  const [editing, setEditing] = useState(false);
  const [confirmDeactivate, setConfirmDeactivate] = useState(false);
  const [amount, setAmount] = useState("");
  const [notice, setNotice] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const passenger = query.data;
  return (
    <div className="space-y-4">
      <QueryState query={query} />
      {passenger && (
        <>
          {editing ? (
            <PassengerForm
              passenger={passenger}
              onSaved={() => {
                setEditing(false);
                setNotice("Passenger updated.");
              }}
              onClose={() => setEditing(false)}
            />
          ) : (
            <Panel title={passenger.fullName}>
              <div className="flex flex-wrap gap-x-6 gap-y-2 text-sm">
                <span>{passenger.phoneNumber}</span>
                <span>{passenger.email || "No email"}</span>
                <span>{passenger.category}</span>
                <span>{passenger.isActive ? "Active" : "Inactive"}</span>
              </div>
              <div className="flex flex-wrap gap-2">
                <Button variant="outline" onClick={() => setEditing(true)}>
                  Edit profile
                </Button>
                {passenger.isActive && (
                  <Button
                    variant="outline"
                    onClick={() => setConfirmDeactivate(true)}
                  >
                    Deactivate account
                  </Button>
                )}
                {!passenger.isActive && (
                  <Button variant="outline" disabled={restore.isPending} onClick={() => restore.mutate(id, { onSuccess: () => setNotice("Passenger account restored.") })}>
                    {restore.isPending ? "Restoring..." : "Restore account"}
                  </Button>
                )}
              </div>
              {confirmDeactivate && (
                <div className="space-y-3 rounded-md border p-3">
                  <p className="text-sm">
                    Deactivate {passenger.fullName}? New bookings will be
                    blocked. Existing bookings and financial history remain
                    available.
                  </p>
                  <div className="flex gap-2">
                    <Button
                      disabled={deactivate.isPending}
                      onClick={() =>
                        deactivate.mutate(id, {
                          onSuccess: () => {
                            setConfirmDeactivate(false);
                            setNotice("Passenger deactivated.");
                          },
                        })
                      }
                    >
                      Confirm deactivation
                    </Button>
                    <Button
                      variant="outline"
                      disabled={deactivate.isPending}
                      onClick={() => setConfirmDeactivate(false)}
                    >
                      Keep active
                    </Button>
                  </div>
                </div>
              )}
              <Feedback error={deactivate.error} success={notice} />
            </Panel>
          )}
          <Panel title="Portal access">
            <p className="text-sm text-muted-foreground">Set a new password for this passenger’s linked commuter account.</p>
            <form className="flex flex-wrap items-end gap-3" onSubmit={(event) => { event.preventDefault(); resetPassword.mutate({ id, password: newPassword }, { onSuccess: () => { setNewPassword(""); setNotice("Portal password reset successfully."); } }); }}>
              <Field label="New password" type="password" minLength={8} required value={newPassword} onChange={(event) => setNewPassword(event.target.value)} placeholder="At least 8 characters" />
              <Button type="submit" disabled={newPassword.length < 8 || resetPassword.isPending}>{resetPassword.isPending ? "Resetting..." : "Reset password"}</Button>
            </form>
            <Feedback error={resetPassword.error} success={resetPassword.isSuccess && "Password reset. Share the new credentials securely."} />
          </Panel>
          <Panel title="Wallet">
            <p className="text-2xl font-semibold tabular-nums">
              {passenger.wallet
                ? money(passenger.wallet.balance)
                : "Wallet unavailable"}
            </p>
            {passenger.wallet && (
              <>
                <form
                  className="flex flex-wrap items-end gap-3"
                  onSubmit={(e) => {
                    e.preventDefault();
                    topUp.mutate(
                      { id, amount: Number(amount) },
                      { onSuccess: () => setAmount("") },
                    );
                  }}
                >
                  <Field
                    label="Top-up amount (LKR)"
                    type="number"
                    min="0.01"
                    max="1000000"
                    step="0.01"
                    required
                    value={amount}
                    disabled={topUp.isPending}
                    onChange={(e) => {
                      setAmount(e.target.value);
                      topUp.reset();
                    }}
                  />
                  <Button type="submit" disabled={topUp.isPending}>
                    {topUp.isPending ? "Adding funds..." : "Top up wallet"}
                  </Button>
                </form>
                <Feedback
                  error={topUp.error}
                  success={
                    topUp.isSuccess &&
                    "Top-up recorded. Balance and transactions refreshed."
                  }
                />
                <DataTable
                  headings={["Date", "Transaction", "Amount", "Booking"]}
                >
                  {passenger.wallet.transactions.map((t) => (
                    <tr key={t.id}>
                      <td>{dateTime(t.createdAt)}</td>
                      <td>{t.type === "Topup" ? "Top-up" : t.type}</td>
                      <td className="tabular-nums">
                        {t.type === "Fare" ? "-" : "+"}
                        {money(t.amount)}
                      </td>
                      <td>
                        {t.bookingId ? (
                          <Link
                            className="underline"
                            to={`/fares/tickets?bookingId=${t.bookingId}`}
                          >
                            View ticket
                          </Link>
                        ) : (
                          "-"
                        )}
                      </td>
                    </tr>
                  ))}
                </DataTable>
                {!passenger.wallet.transactions.length && (
                  <p className="text-sm text-muted-foreground">
                    No wallet transactions yet.
                  </p>
                )}
              </>
            )}
          </Panel>
          <Panel title="Booking history">
            <DataTable headings={["Route", "Seat", "Fare", "Status", "Ticket"]}>
              {passenger.bookings.map((b) => (
                <tr key={b.id}>
                  <td>{b.route}</td>
                  <td>{b.seatNumber}</td>
                  <td>{money(b.fare)}</td>
                  <td>{b.status}</td>
                  <td>
                    <Link
                      className="underline"
                      to={`/fares/tickets?bookingId=${b.id}`}
                    >
                      View ticket
                    </Link>
                  </td>
                </tr>
              ))}
            </DataTable>
            {!passenger.bookings.length && (
              <p className="text-sm text-muted-foreground">No bookings yet.</p>
            )}
          </Panel>
        </>
      )}
    </div>
  );
}
