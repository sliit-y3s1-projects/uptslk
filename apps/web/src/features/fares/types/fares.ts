import type {
  PassengerCategory,
  WalletTransaction,
} from "@/features/riders/types/passengers";
export type BookingStatus = "Pending" | "Confirmed" | "Completed" | "Cancelled";
export interface FareRule {
  id: string;
  routeId: string;
  route: { routeNumber: string; name: string; centreId: string };
  passengerCategory: PassengerCategory;
  amount: number;
  isActive: boolean;
  updatedAt: string;
}
export interface CreateFareRuleRequest {
  routeId: string;
  passengerCategory: PassengerCategory;
  amount: number;
}
export interface UpdateFareRuleRequest {
  passengerCategory: PassengerCategory;
  amount: number;
  isActive: boolean;
}
export interface FareFilters {
  routeId?: string;
  centreId?: string;
  passengerCategory?: string;
  active?: string;
}
export interface BookingFilters {
  passengerId?: string;
  tripId?: string;
  status?: string;
}
export interface CreateBookingRequest {
  tripId: string;
  passengerId: string;
  seatNumber: string;
}
export interface CheckoutSession {
  url: string;
}
export interface PaymentOrderStatus {
  bookingId: string;
  status: "Initiated" | "Pending" | "Succeeded" | "Failed" | "Cancelled" | "Chargebacked" | "RefundPending" | "Refunded";
  amount: number;
  currency: string;
  provider: "Mock";
  updatedAt: string;
}
export interface Booking {
  id: string;
  tripId: string;
  route: string;
  tripTime: string;
  passengerId: string;
  passenger: string;
  seatNumber: string;
  fare: number;
  status: BookingStatus;
  createdAt: string;
}
export interface TripSummary {
  id: string;
  scheduledTime: string;
  route: string;
  name: string;
  vehicle: string;
}
export interface BookingDetail {
  id: string;
  trip: TripSummary;
  passenger: {
    id: string;
    fullName: string;
    phoneNumber: string;
    category: PassengerCategory;
    balance: number | null;
  };
  seatNumber: string;
  fare: number;
  passengerCategory: PassengerCategory;
  qrCode: string;
  status: BookingStatus;
  cancelledAt: string | null;
  cancellationReason: string | null;
  refundAmount: number;
  transactions: WalletTransaction[];
  createdAt: string;
  updatedAt: string;
}
export interface Seat {
  seatNumber: string;
  isAvailable: boolean;
}
export interface Manifest {
  trip: TripSummary;
  bookings: {
    id: string;
    seatNumber: string;
    passenger: string;
    phoneNumber: string;
    category: PassengerCategory;
    status: BookingStatus;
    qrCode: string;
  }[];
  passengerCount: number;
}
export interface FareQuote {
  trip: Omit<TripSummary, "vehicle">;
  passenger: { id: string; category: PassengerCategory };
  fareRuleId: string;
  fare: number;
}
export interface TripOption {
  id: string;
  centreId: string;
  routeId: string;
  routeNumber: string;
  routeName: string;
  vehicle: string;
  bay: string;
  scheduledTime: string;
  status: string;
}
export interface TripDetail {
  id: string;
  status: string;
  bay: { code: string };
  vehicle: { isAccessible: boolean; capacity: number };
}
export interface RouteOption {
  id: string;
  centreId: string;
  routeNumber: string;
  name: string;
  isActive: boolean;
}
export interface CentreOption {
  id: string;
  code: string;
  name: string;
}

export interface CreatedFareRule extends CreateFareRuleRequest {
  id: string;
  isActive: boolean;
}
export interface CreatedBooking extends CreateBookingRequest {
  id: string;
  fare: number;
  status: BookingStatus;
  qrCode: string;
}
export interface ChangeSeatRequest {
  seatNumber: string;
}
export interface CompleteBookingRequest {
  status: "Completed";
}
export interface CancelBookingRequest {
  reason: string;
}
