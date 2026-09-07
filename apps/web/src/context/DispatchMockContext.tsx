import { createContext, useContext, useState, type ReactNode } from "react";
import { initialDispatchApprovals, initialDispatchIncidents, initialTrips, type DispatchApproval, type DispatchIncident, type Trip, type TripStatus } from "@/mock/dispatch";

type Store = {
  trips: Trip[];
  incidents: DispatchIncident[];
  approvals: DispatchApproval[];
  saveTrip: (trip: Trip) => void;
  transitionTrip: (id: string, status: TripStatus) => void;
  cancelTrip: (id: string, reason: string) => void;
  addIncident: (incident: DispatchIncident) => void;
  updateIncident: (id: string, status: DispatchIncident["status"]) => void;
  addApproval: (approval: DispatchApproval) => void;
  decideApproval: (id: string, status: "Approved" | "Rejected") => void;
};

const DispatchMockContext = createContext<Store | undefined>(undefined);

export function DispatchMockProvider({ children }: { children: ReactNode }) {
  const [trips, setTrips] = useState(() => structuredClone(initialTrips));
  const [incidents, setIncidents] = useState(() => structuredClone(initialDispatchIncidents));
  const [approvals, setApprovals] = useState(() => structuredClone(initialDispatchApprovals));
  function saveTrip(trip: Trip) {
    const conflict = trips.find((item) => item.id !== trip.id && item.centreId === trip.centreId && item.serviceDate === trip.serviceDate && item.scheduledTime === trip.scheduledTime && item.status !== "Cancelled" && (item.bay === trip.bay || item.vehicleId === trip.vehicleId || item.driverId === trip.driverId));
    if (conflict) throw new Error(`Conflict with ${conflict.id}: the bay, vehicle, or driver is already assigned at ${trip.scheduledTime}.`);
    setTrips((current) => current.some((item) => item.id === trip.id) ? current.map((item) => item.id === trip.id ? trip : item) : [trip, ...current]);
  }
  function transitionTrip(id: string, status: TripStatus) { setTrips((current) => current.map((item) => item.id === id ? { ...item, status, updatedAt: "Updated just now" } : item)); }
  function cancelTrip(id: string, reason: string) { setTrips((current) => current.map((item) => item.id === id ? { ...item, status: "Cancelled", notes: reason, updatedAt: "Cancelled just now" } : item)); }
  function addIncident(incident: DispatchIncident) { setIncidents((current) => [incident, ...current]); }
  function updateIncident(id: string, status: DispatchIncident["status"]) { setIncidents((current) => current.map((item) => item.id === id ? { ...item, status } : item)); }
  function addApproval(approval: DispatchApproval) { setApprovals((current) => [approval, ...current]); }
  function decideApproval(id: string, status: "Approved" | "Rejected") { setApprovals((current) => current.map((item) => item.id === id ? { ...item, status } : item)); }
  return <DispatchMockContext.Provider value={{ trips, incidents, approvals, saveTrip, transitionTrip, cancelTrip, addIncident, updateIncident, addApproval, decideApproval }}>{children}</DispatchMockContext.Provider>;
}

export function useDispatchMock() {
  const context = useContext(DispatchMockContext);
  if (!context) throw new Error("useDispatchMock must be used within DispatchMockProvider");
  return context;
}
