import { useEffect } from "react";
import { useAuth } from "@/hooks/useAuth";
import { SignInPanel } from "@/components/auth/SignInPanel";
import { RegisterPage } from "@/components/auth/RegisterPage";
import { OnboardingPage } from "@/components/auth/OnboardingPage";
import { ProfilePage } from "@/features/profile/ProfilePage";
import { PasswordChangePage } from "@/features/profile/PasswordChangePage";
import { BookingPaymentStatusPage, CheckoutPage } from "@/features/bookings/CheckoutPage";
import { MyTicketsPage } from "@/features/bookings/MyTicketsPage";
import { CommuterLayout } from "@/features/bookings/CommuterHeader";
import { NotFoundPage } from "@/components/NotFoundPage";
import { RequireRole } from "./components/auth/RequireAuth";
import { Navigate, Route, Routes, useLocation } from "react-router";
import { PublicBookingPage } from "@/features/bookings/PublicBookingPage";
import { AdminShell } from "@/components/layout/AdminShell";
import { DispatchPage } from "@/features/operations/DispatchPage";
import { DutyRosterPage } from "@/features/operations/DutyRosterPage";
import { AgentRecoveryDetailPage, AgentRecoveryPage } from "@/features/agent-recovery/AgentRecoveryPages";
import { BayManagementPage } from "@/features/operations/BayManagementPage";
import { IncidentsPage } from "@/features/operations/IncidentsPage";
import { ApprovalsPage } from "@/features/operations/ApprovalsPage";
import { IncidentFormPage } from "@/features/operations/IncidentFormPage";
import { TripDetailPage } from "@/features/operations/TripDetailPage";
import { TripFormPage } from "@/features/operations/TripFormPage";
import { TripHistoryPage } from "@/features/operations/TripHistoryPage";
import { StopsPage } from "@/features/network/NetworkPages";
import { TimetablesPage } from "@/features/network/TimetablesPage";
import {
  DriverDetailPage,
  DriverFormPage,
  DriversPage,
} from "@/features/fleet/DriversPage";
import {
  MaintenanceDetailPage,
  MaintenanceFormPage,
  MaintenancePage,
} from "@/features/fleet/MaintenancePage";
import {
  RouteDetailPage,
  RouteFormPage,
  RoutesPage,
} from "@/features/network/RoutesPage";
import {
  VehicleFormPage,
  VehicleProfilePage,
  VehiclesPage,
} from "@/features/fleet/VehiclesPage";
import { RidersPage, SupportPage } from "@/features/riders/RiderPages";
import {
  AssistancePage,
  PassengerFlowPage,
} from "@/features/riders/PassengerOperationsPages";
import { BookingManagementPage, FareRulesManagementPage, PaymentReturnPage, PaymentsPage, TicketsPage } from "@/features/fares/FarePages";
import { ReconciliationPage } from "@/features/fares/ReconciliationPage";
import { RidershipPage, RevenuePage } from "@/features/reports/ReportsPages";
import { IntegrationsPage } from "@/features/settings/SettingsPages";
import { TeamPage } from "@/features/settings/TeamPage";
import { CentreConsolePage } from "@/features/centres/CentreConsolePage";
import {
  CentreFormPage,
  CentreProfilePage,
  CentresPage,
} from "@/features/centres/CentresPage";
import {
  CentreOperationsPreviewPage,
  CentreRouteDetailViewPage,
  CentreRoutesViewPage,
  CentreVehicleDetailViewPage,
  CentreVehiclesViewPage,
} from "@/features/centres/CentreOperationalViews";
import { SuperAdminShell } from "@/components/super-admin/SuperAdminShell";
import { SuperAdminOverviewPage } from "@/features/super-admin/OverviewPage";
import { EmployeesPage } from "@/features/super-admin/EmployeesPage";
import { UsersPage } from "@/features/super-admin/UsersPage";
import {
  AccessRequestsPage,
  RolesPage,
} from "@/features/super-admin/AccessPages";
import {
  AuditLogPage,
  PlatformHealthPage,
  SystemSettingsPage,
} from "@/features/super-admin/GovernancePages";

function App() {
  const { user, loading } = useAuth();
  const location = useLocation();

  useEffect(() => {
    const migrateLegacyBookingLink = (event: MouseEvent) => {
      const anchor = (event.target as HTMLElement).closest<HTMLAnchorElement>(
        'a[href="/book"]',
      );
      if (anchor) {
        event.preventDefault();
        window.history.pushState({}, "", "/");
        window.dispatchEvent(new PopStateEvent("popstate"));
      }
    };
    document.addEventListener("click", migrateLegacyBookingLink);
    return () =>
      document.removeEventListener("click", migrateLegacyBookingLink);
  }, []);

  if (loading) return null;

  if (!user) {
    if (location.pathname === "/") return <CommuterLayout><PublicBookingPage /></CommuterLayout>;
    if (location.pathname === "/signup") return <RegisterPage />;
    if (location.pathname === "/login") return <SignInPanel />;
    return <NotFoundPage />;
  }

  if (location.pathname === "/onboarding") return <OnboardingPage />;
  if (location.pathname === "/book") return <NotFoundPage />;
  if (location.pathname === "/") return <CommuterLayout><PublicBookingPage /></CommuterLayout>;
  if (location.pathname === "/profile") return <CommuterLayout><ProfilePage /></CommuterLayout>;
  if (location.pathname === "/profile/password") return <CommuterLayout><PasswordChangePage /></CommuterLayout>;
  if (location.pathname === "/booking/checkout") return <CommuterLayout><CheckoutPage /></CommuterLayout>;
  if (location.pathname === "/booking/payment-return") return <CommuterLayout><BookingPaymentStatusPage /></CommuterLayout>;
  if (location.pathname === "/booking/payment-cancel") return <CommuterLayout><BookingPaymentStatusPage cancelled /></CommuterLayout>;
  if (location.pathname === "/my-tickets") return <CommuterLayout><MyTicketsPage /></CommuterLayout>;

  const isSuperAdmin = user.role === "SuperAdmin" || user.role === "Admin";

  if (isSuperAdmin)
    return (
      <RequireRole role={user.role}>
        <SuperAdminShell>
          <Routes>
            <Route path="/admin" element={<SuperAdminOverviewPage />} />
            <Route path="/admin/centres" element={<CentresPage />} />
            <Route path="/admin/centres/new" element={<CentreFormPage />} />
            <Route
              path="/admin/centres/:centreId"
              element={<CentreProfilePage />}
            />
            <Route
              path="/admin/centres/:centreId/edit"
              element={<CentreFormPage />}
            />
            <Route
              path="/admin/centres/:centreId/operations"
              element={<CentreOperationsPreviewPage />}
            />
            <Route
              path="/admin/centres/:centreId/routes"
              element={<CentreRoutesViewPage />}
            />
            <Route
              path="/admin/centres/:centreId/routes/:routeId"
              element={<CentreRouteDetailViewPage />}
            />
            <Route
              path="/admin/centres/:centreId/vehicles"
              element={<CentreVehiclesViewPage />}
            />
            <Route
              path="/admin/centres/:centreId/vehicles/:vehicleId"
              element={<CentreVehicleDetailViewPage />}
            />
            <Route path="/network/routes" element={<RoutesPage />} />
            <Route path="/network/routes/new" element={<RouteFormPage />} />
            <Route path="/network/routes/:routeId" element={<RouteDetailPage />} />
            <Route path="/network/routes/:routeId/edit" element={<RouteFormPage />} />
            <Route path="/admin/employees" element={<EmployeesPage />} />
            <Route path="/admin/users" element={<UsersPage />} />
            <Route path="/admin/roles" element={<RolesPage />} />
            <Route path="/admin/access" element={<AccessRequestsPage />} />
            <Route path="/admin/audit" element={<AuditLogPage />} />
            <Route path="/admin/health" element={<PlatformHealthPage />} />
            <Route path="/admin/settings" element={<SystemSettingsPage />} />
            <Route path="*" element={<Navigate to="/admin" replace />} />
          </Routes>
        </SuperAdminShell>
      </RequireRole>
    );

  return (
    <RequireRole
      role={["CentreManager", "Dispatcher", "FleetOfficer", "Driver"]}
    >
      <AdminShell>
        <Routes>
          <Route path="/operations" element={<CentreConsolePage />} />
          <Route path="/operations/dispatch" element={<DispatchPage />} />
          <Route path="/operations/duty-roster" element={<DutyRosterPage />} />
          <Route path="/operations/agent-recovery" element={<AgentRecoveryPage />} />
          <Route path="/operations/agent-recovery/:workflowId" element={<AgentRecoveryDetailPage />} />
          <Route path="/operations/dispatch/new" element={<TripFormPage />} />
          <Route
            path="/operations/dispatch/:tripId"
            element={<TripDetailPage />}
          />
          <Route
            path="/operations/dispatch/:tripId/edit"
            element={<TripFormPage />}
          />
          <Route path="/operations/history" element={<TripHistoryPage />} />
          <Route path="/operations/bays" element={<BayManagementPage />} />
          <Route path="/operations/incidents" element={<IncidentsPage />} />
          <Route
            path="/operations/incidents/new"
            element={<IncidentFormPage />}
          />
          <Route path="/operations/approvals" element={<ApprovalsPage />} />
          <Route path="/network/routes" element={<RoutesPage />} />
          <Route path="/network/routes/new" element={<RouteFormPage />} />
          <Route
            path="/network/routes/:routeId"
            element={<RouteDetailPage />}
          />
          <Route
            path="/network/routes/:routeId/edit"
            element={<RouteFormPage />}
          />
          <Route path="/network/timetables" element={<TimetablesPage />} />
          <Route path="/network/stops" element={<StopsPage />} />
          <Route path="/fleet/vehicles" element={<VehiclesPage />} />
          <Route path="/fleet/vehicles/new" element={<VehicleFormPage />} />
          <Route
            path="/fleet/vehicles/:vehicleId"
            element={<VehicleProfilePage />}
          />
          <Route
            path="/fleet/vehicles/:vehicleId/edit"
            element={<VehicleFormPage />}
          />
          <Route path="/fleet/drivers" element={<DriversPage />} />
          <Route path="/fleet/drivers/new" element={<DriverFormPage />} />
          <Route
            path="/fleet/drivers/:driverId"
            element={<DriverDetailPage />}
          />
          <Route
            path="/fleet/drivers/:driverId/edit"
            element={<DriverFormPage />}
          />
          <Route path="/fleet/maintenance" element={<MaintenancePage />} />
          <Route
            path="/fleet/maintenance/new"
            element={<MaintenanceFormPage />}
          />
          <Route
            path="/fleet/maintenance/:recordId"
            element={<MaintenanceDetailPage />}
          />
          <Route
            path="/fleet/maintenance/:recordId/edit"
            element={<MaintenanceFormPage />}
          />
          <Route path="/passengers/flow" element={<PassengerFlowPage />} />
          <Route path="/passengers/assistance" element={<AssistancePage />} />
          <Route path="/riders/accounts" element={<RidersPage />} />
          <Route path="/riders/support" element={<SupportPage />} />
          <Route path="/fares/tickets" element={<TicketsPage />} />
          <Route path="/fares/bookings" element={<BookingManagementPage />} />
          <Route path="/fares/fare-rules" element={<FareRulesManagementPage />} />
          <Route path="/fares/bookings/payment-return" element={<PaymentReturnPage />} />
          <Route path="/fares/bookings/payment-cancel" element={<PaymentReturnPage cancelled />} />
          <Route path="/fares/payments" element={<PaymentsPage />} />
          <Route
            path="/fares/reconciliation"
            element={<ReconciliationPage />}
          />
          <Route path="/reports/ridership" element={<RidershipPage />} />
          <Route path="/reports/revenue" element={<RevenuePage />} />
          <Route path="/settings/team" element={<TeamPage />} />
          <Route path="/settings/integrations" element={<IntegrationsPage />} />
          <Route path="*" element={<Navigate to="/operations" replace />} />
        </Routes>
      </AdminShell>
    </RequireRole>
  );
}

export default App;
