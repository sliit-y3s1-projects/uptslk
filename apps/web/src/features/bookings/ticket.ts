export type Ticket = {
  id: string;
  status: "Pending" | "Confirmed" | "Completed" | "Cancelled";
  tripStatus: string;
  passengerCount: number;
  fare: number;
  qrCode: string | null;
  canBoard: boolean;
  tripTime: string;
  activeUntil?: string;
  route: string;
  routeName: string;
  passengerName: string;
  origin: string;
  destination: string;
  bay: string;
};

export function ticketGroup(ticket: Ticket, now = Date.now()) {
  if (ticket.status === "Cancelled" || ticket.tripStatus === "Cancelled")
    return "Cancelled";
  if (ticket.status === "Completed" || ticket.tripStatus === "Completed")
    return "Past";
  return new Date(ticket.tripTime).getTime() > now ||
    (["Boarding", "Delayed", "Dispatched"].includes(ticket.tripStatus) &&
      now <
        (ticket.activeUntil
          ? Date.parse(ticket.activeUntil)
          : Date.parse(ticket.tripTime) + 2 * 60 * 60 * 1000))
    ? "Upcoming"
    : "Past";
}

export function canBoard(ticket: Ticket, now = Date.now()) {
  return (
    ticket.canBoard &&
    ticket.status === "Confirmed" &&
    !!ticket.qrCode &&
    ticketGroup(ticket, now) === "Upcoming" &&
    ticket.tripStatus !== "Dispatched"
  );
}

export function ticketLabel(ticket: Ticket) {
  const group = ticketGroup(ticket);
  if (group === "Cancelled") return "Cancelled";
  if (group === "Past") {
    if (ticket.tripStatus === "Dispatched" && ticket.status !== "Completed")
      return "Past trip";
    return ticket.status === "Completed" || ticket.tripStatus === "Completed"
      ? "Completed"
      : "Expired";
  }
  if (ticket.status === "Pending") return "Awaiting payment";
  return ticket.tripStatus === "Dispatched" ? "In progress" : "Confirmed";
}

export function departureLabel(value: string) {
  return new Date(value).toLocaleString("en-GB", {
    timeZone: "Asia/Colombo",
    day: "numeric",
    month: "short",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
    hour12: true,
  });
}
