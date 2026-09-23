export type PassengerCategory = "Adult" | "Student" | "Senior" | "Child";
export interface PassengerRequest {
  fullName: string;
  phoneNumber: string;
  email: string | null;
  password?: string;
  category: PassengerCategory;
}
export interface UpdatePassengerRequest extends PassengerRequest {
  isActive: boolean;
}
export interface Passenger extends UpdatePassengerRequest {
  id: string;
  balance: number;
  bookingCount: number;
}
export interface WalletTransaction {
  id: string;
  bookingId?: string | null;
  type: "Topup" | "Fare" | "Refund";
  amount: number;
  createdAt: string;
}
export interface PassengerDetail extends UpdatePassengerRequest {
  id: string;
  wallet: {
    id: string;
    balance: number;
    transactions: WalletTransaction[];
  } | null;
  bookings: {
    id: string;
    tripId: string;
    route: string;
    passengerCount: number;
    fare: number;
    status: string;
    createdAt: string;
  }[];
}
export interface PassengerFilters {
  search?: string;
  category?: string;
  active?: string;
}

export interface CreatedPassenger {
  id: string;
  fullName: string;
  phoneNumber: string;
  category: PassengerCategory;
  balance: number;
  portalAccountCreated?: boolean;
}
export interface TopUpWalletRequest {
  amount: number;
}
export interface TopUpWalletResponse {
  id: string;
  balance: number;
  type: "Topup";
  amount: number;
  createdAt: string;
}
