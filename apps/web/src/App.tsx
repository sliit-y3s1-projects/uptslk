import { lazy, Suspense, useEffect } from "react";
import { LoaderCircle } from "lucide-react";
import { useAuth } from "@/hooks/useAuth";
import { SignInPanel } from "@/components/auth/SignInPanel";
import { RegisterPage } from "@/components/auth/RegisterPage";
import { OnboardingPage } from "@/components/auth/OnboardingPage";
import { CommuterLayout } from "@/features/bookings/CommuterHeader";
import { NotFoundPage } from "@/components/NotFoundPage";
import { RequireRole } from "./components/auth/RequireAuth";
import { Navigate, Route, Routes, useLocation } from "react-router";
import { LandingPage } from "@/features/landing/LandingPage";
import { TeamPage as PublicTeamPage } from "@/features/landing/TeamPage";
import { AdminShell } from "@/components/layout/AdminShell";
import { SuperAdminShell } from "@/components/super-admin/SuperAdminShell";

const ProfilePage = lazy(() =>
  import("@/features/profile/ProfilePage").then((module) => ({
    default: module.ProfilePage,
  })),
);

const StaffProfilePage = lazy(() =>
  import("@/features/profile/StaffProfilePage").then((module) => ({
    default: module.StaffProfilePage,
  })),
);

const PasswordChangePage = lazy(() =>
  import("@/features/profile/PasswordChangePage").then((module) => ({
    default: module.PasswordChangePage,
  })),
);

const BookingPaymentStatusPage = lazy(() =>
  import("@/features/bookings/CheckoutPage").then((module) => ({
    default: module.BookingPaymentStatusPage,
  })),
);

const CheckoutPage = lazy(() =>
  import("@/features/bookings/CheckoutPage").then((module) => ({
    default: module.CheckoutPage,
  })),
);

const MyTicketsPage = lazy(() =>
  import("@/features/bookings/MyTicketsPage").then((module) => ({
    default: module.MyTicketsPage,
  })),
);

const PublicBookingPage = lazy(() =>
  import("@/features/bookings/PublicBookingPage").then((module) => ({
    default: module.PublicBookingPage,
  })),
);

const DispatchPage = lazy(() =>
  import("@/features/operations/DispatchPage").then((module) => ({
    default: module.DispatchPage,
  })),
);

const DutyRosterPage = lazy(() =>
  import("@/features/operations/DutyRosterPage").then((module) => ({
    default: module.DutyRosterPage,
  })),
);

const AgentRecoveryDetailPage = lazy(() =>
  import("@/features/agent-recovery/AgentRecoveryPages").then((module) => ({
    default: module.AgentRecoveryDetailPage,
  })),
);

const AgentRecoveryPage = lazy(() =>
  import("@/features/agent-recovery/AgentRecoveryPages").then((module) => ({
    default: module.AgentRecoveryPage,
  })),
);

const BayManagementPage = lazy(() =>
  import("@/features/operations/BayManagementPage").then((module) => ({
    default: module.BayManagementPage,
  })),
);

const IncidentsPage = lazy(() =>
  import("@/features/operations/IncidentsPage").then((module) => ({
    default: module.IncidentsPage,
  })),
);

const IncidentFormPage = lazy(() =>
  import("@/features/operations/IncidentFormPage").then((module) => ({
    default: module.IncidentFormPage,
  })),
);

const TripDetailPage = lazy(() =>
  import("@/features/operations/TripDetailPage").then((module) => ({
    default: module.TripDetailPage,
  })),
);

const TripFormPage = lazy(() =>
  import("@/features/operations/TripFormPage").then((module) => ({
    default: module.TripFormPage,
  })),
);

const TripHistoryPage = lazy(() =>
  import("@/features/operations/TripHistoryPage").then((module) => ({
    default: module.TripHistoryPage,
  })),
);

const TimetablesPage = lazy(() =>
  import("@/features/network/TimetablesPage").then((module) => ({
    default: module.TimetablesPage,
  })),
);

const DriverDetailPage = lazy(() =>
  import("@/features/fleet/DriversPage").then((module) => ({
    default: module.DriverDetailPage,
  })),
);

const DriverFormPage = lazy(() =>
  import("@/features/fleet/DriversPage").then((module) => ({
    default: module.DriverFormPage,
  })),
);

const DriversPage = lazy(() =>
  import("@/features/fleet/DriversPage").then((module) => ({
    default: module.DriversPage,
  })),
);

const MaintenanceDetailPage = lazy(() =>
  import("@/features/fleet/MaintenancePage").then((module) => ({
    default: module.MaintenanceDetailPage,
  })),
);

const MaintenanceFormPage = lazy(() =>
  import("@/features/fleet/MaintenancePage").then((module) => ({
    default: module.MaintenanceFormPage,
  })),
);

const MaintenancePage = lazy(() =>
  import("@/features/fleet/MaintenancePage").then((module) => ({
    default: module.MaintenancePage,
  })),
);

const RouteDetailPage = lazy(() =>
  import("@/features/network/RoutesPage").then((module) => ({
    default: module.RouteDetailPage,
  })),
);

const RouteFormPage = lazy(() =>
  import("@/features/network/RoutesPage").then((module) => ({
    default: module.RouteFormPage,
  })),
);

const RoutesPage = lazy(() =>
  import("@/features/network/RoutesPage").then((module) => ({
    default: module.RoutesPage,
  })),
);

const VehicleFormPage = lazy(() =>
  import("@/features/fleet/VehiclesPage").then((module) => ({
    default: module.VehicleFormPage,
  })),
);

const VehicleProfilePage = lazy(() =>
  import("@/features/fleet/VehiclesPage").then((module) => ({
    default: module.VehicleProfilePage,
  })),
);

const VehiclesPage = lazy(() =>
  import("@/features/fleet/VehiclesPage").then((module) => ({
    default: module.VehiclesPage,
  })),
);

const RidersPage = lazy(() =>
  import("@/features/riders/RiderPages").then((module) => ({
    default: module.RidersPage,
  })),
);

const PassengerFlowPage = lazy(() =>
  import("@/features/riders/PassengerOperationsPages").then((module) => ({
    default: module.PassengerFlowPage,
  })),
);

const BookingManagementPage = lazy(() =>
  import("@/features/fares/FarePages").then((module) => ({
    default: module.BookingManagementPage,
  })),
);

const FareRulesManagementPage = lazy(() =>
  import("@/features/fares/FarePages").then((module) => ({
    default: module.FareRulesManagementPage,
  })),
);

const PaymentReturnPage = lazy(() =>
  import("@/features/fares/FarePages").then((module) => ({
    default: module.PaymentReturnPage,
  })),
);

const PaymentsPage = lazy(() =>
  import("@/features/fares/FarePages").then((module) => ({
    default: module.PaymentsPage,
  })),
);

const TicketsPage = lazy(() =>
  import("@/features/fares/FarePages").then((module) => ({
    default: module.TicketsPage,
  })),
);

const ReconciliationPage = lazy(() =>
  import("@/features/fares/ReconciliationPage").then((module) => ({
    default: module.ReconciliationPage,
  })),
);

const RidershipPage = lazy(() =>
  import("@/features/reports/ReportsPages").then((module) => ({
    default: module.RidershipPage,
  })),
);

const RevenuePage = lazy(() =>
  import("@/features/reports/ReportsPages").then((module) => ({
    default: module.RevenuePage,
  })),
);

const IntegrationsPage = lazy(() =>
  import("@/features/settings/SettingsPages").then((module) => ({
    default: module.IntegrationsPage,
  })),
);

const TeamPage = lazy(() =>
  import("@/features/settings/TeamPage").then((module) => ({
    default: module.TeamPage,
  })),
);

const CentreConsolePage = lazy(() =>
  import("@/features/centres/CentreConsolePage").then((module) => ({
    default: module.CentreConsolePage,
  })),
);

const CentreFormPage = lazy(() =>
  import("@/features/centres/CentresPage").then((module) => ({
    default: module.CentreFormPage,
  })),
);

const CentreProfilePage = lazy(() =>
  import("@/features/centres/CentresPage").then((module) => ({
    default: module.CentreProfilePage,
  })),
);

const CentresPage = lazy(() =>
  import("@/features/centres/CentresPage").then((module) => ({
    default: module.CentresPage,
  })),
);

const CentreOperationsPreviewPage = lazy(() =>
  import("@/features/centres/CentreOperationalViews").then((module) => ({
    default: module.CentreOperationsPreviewPage,
  })),
);

const CentreRouteDetailViewPage = lazy(() =>
  import("@/features/centres/CentreOperationalViews").then((module) => ({
    default: module.CentreRouteDetailViewPage,
  })),
);

const CentreRoutesViewPage = lazy(() =>
  import("@/features/centres/CentreOperationalViews").then((module) => ({
    default: module.CentreRoutesViewPage,
  })),
);

const CentreVehicleDetailViewPage = lazy(() =>
  import("@/features/centres/CentreOperationalViews").then((module) => ({
    default: module.CentreVehicleDetailViewPage,
  })),
);

const CentreVehiclesViewPage = lazy(() =>
  import("@/features/centres/CentreOperationalViews").then((module) => ({
    default: module.CentreVehiclesViewPage,
  })),
);

const SuperAdminOverviewPage = lazy(() =>
  import("@/features/super-admin/OverviewPage").then((module) => ({
    default: module.SuperAdminOverviewPage,
  })),
);

const EmployeesPage = lazy(() =>
  import("@/features/super-admin/EmployeesPage").then((module) => ({
    default: module.EmployeesPage,
  })),
);

const UsersPage = lazy(() =>
  import("@/features/super-admin/UsersPage").then((module) => ({
    default: module.UsersPage,
  })),
);

const AccessRequestsPage = lazy(() =>
  import("@/features/super-admin/AccessPages").then((module) => ({
    default: module.AccessRequestsPage,
  })),
);

const RolesPage = lazy(() =>
  import("@/features/super-admin/AccessPages").then((module) => ({
    default: module.RolesPage,
  })),
);

const AuditLogPage = lazy(() =>
  import("@/features/super-admin/GovernancePages").then((module) => ({
    default: module.AuditLogPage,
  })),
);

function RouteFallback() {
  return (
    <div
      role="status"
      aria-label="Loading page"
      className="flex flex-1 items-center justify-center p-10 text-muted-foreground"
    >
      <LoaderCircle className="size-5 animate-spin" />
    </div>
  );
}

function AppRoutes() {
  const { user, loading } = useAuth();
  const location = useLocation();

  useEffect(() => {
    const migrateLegacyBookingLink = (event: MouseEvent) => {
      const anchor = (event.target as HTMLElement).closest<HTMLAnchorElement>(
        'a[href="/book"]',
      );
      if (anchor) {
        event.preventDefault();
        window.history.pushState({}, "", "/reservation");
        window.dispatchEvent(new PopStateEvent("popstate"));
      }
    };
    document.addEventListener("click", migrateLegacyBookingLink);
    return () =>
      document.removeEventListener("click", migrateLegacyBookingLink);
  }, []);

  if (loading) return null;

  if (!user) {
    if (location.pathname === "/") return <LandingPage />;
    if (location.pathname === "/team") return <PublicTeamPage />;
    if (location.pathname === "/book")
      return <Navigate to="/reservation" replace />;
    if (location.pathname === "/reservation")
      return (
        <CommuterLayout>
          <PublicBookingPage />
        </CommuterLayout>
      );
    if (location.pathname === "/signup") return <RegisterPage />;
    if (location.pathname === "/login") return <SignInPanel />;
    return <NotFoundPage />;
  }

  if (location.pathname === "/onboarding") return <OnboardingPage />;
  if (location.pathname === "/book")
    return <Navigate to="/reservation" replace />;
  if (location.pathname === "/") return <LandingPage />;
  if (location.pathname === "/team") return <PublicTeamPage />;
  if (location.pathname === "/reservation")
    return (
      <CommuterLayout>
        <PublicBookingPage />
      </CommuterLayout>
    );
  if (location.pathname === "/profile")
    return user.role === "Admin" || user.role === "SuperAdmin" ? (
      <SuperAdminShell>
        <StaffProfilePage />
      </SuperAdminShell>
    ) : user.role === "Commuter" ? (
      <CommuterLayout>
        <ProfilePage />
      </CommuterLayout>
    ) : (
      <AdminShell>
        <StaffProfilePage />
      </AdminShell>
    );
  if (location.pathname === "/profile/password")
    return user.role === "Admin" || user.role === "SuperAdmin" ? (
      <SuperAdminShell>
        <PasswordChangePage embedded />
      </SuperAdminShell>
    ) : user.role === "Commuter" ? (
      <CommuterLayout>
        <PasswordChangePage />
      </CommuterLayout>
    ) : (
      <AdminShell>
        <PasswordChangePage embedded />
      </AdminShell>
    );
  if (location.pathname === "/booking/checkout")
    return (
      <CommuterLayout>
        <CheckoutPage />
      </CommuterLayout>
    );
  if (location.pathname === "/booking/payment-return")
    return (
      <CommuterLayout>
        <BookingPaymentStatusPage />
      </CommuterLayout>
    );
  if (location.pathname === "/booking/payment-cancel")
    return (
      <CommuterLayout>
        <BookingPaymentStatusPage cancelled />
      </CommuterLayout>
    );
  if (location.pathname === "/my-tickets")
    return (
      <CommuterLayout>
        <MyTicketsPage />
      </CommuterLayout>
    );

  const isSuperAdmin = user.role === "SuperAdmin" || user.role === "Admin";

  if (isSuperAdmin)
    return (
      <RequireRole role={user.role}>
        <SuperAdminShell>
          <Suspense fallback={<RouteFallback />}>
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
              <Route
                path="/network/routes/:routeId"
                element={<RouteDetailPage />}
              />
              <Route
                path="/network/routes/:routeId/edit"
                element={<RouteFormPage />}
              />
              <Route path="/admin/employees" element={<EmployeesPage />} />
              <Route path="/admin/users" element={<UsersPage />} />
              <Route path="/admin/roles" element={<RolesPage />} />
              <Route path="/admin/access" element={<AccessRequestsPage />} />
              <Route path="/admin/audit" element={<AuditLogPage />} />
              <Route path="*" element={<Navigate to="/admin" replace />} />
            </Routes>
          </Suspense>
        </SuperAdminShell>
      </RequireRole>
    );

  return (
    <RequireRole
      role={["CentreManager", "Dispatcher", "FleetOfficer", "Driver"]}
    >
      <AdminShell>
        <Suspense fallback={<RouteFallback />}>
          <Routes>
            <Route path="/operations" element={<CentreConsolePage />} />
            <Route path="/operations/dispatch" element={<DispatchPage />} />
            <Route
              path="/operations/duty-roster"
              element={<DutyRosterPage />}
            />
            <Route
              path="/operations/agent-recovery"
              element={<AgentRecoveryPage />}
            />
            <Route
              path="/operations/agent-recovery/:workflowId"
              element={<AgentRecoveryDetailPage />}
            />
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
            <Route path="/riders/accounts" element={<RidersPage />} />
            <Route path="/fares/tickets" element={<TicketsPage />} />
            <Route path="/fares/bookings" element={<BookingManagementPage />} />
            <Route
              path="/fares/fare-rules"
              element={<FareRulesManagementPage />}
            />
            <Route
              path="/fares/bookings/payment-return"
              element={<PaymentReturnPage />}
            />
            <Route
              path="/fares/bookings/payment-cancel"
              element={<PaymentReturnPage cancelled />}
            />
            <Route path="/fares/payments" element={<PaymentsPage />} />
            <Route
              path="/fares/reconciliation"
              element={<ReconciliationPage />}
            />
            <Route path="/reports/ridership" element={<RidershipPage />} />
            <Route path="/reports/revenue" element={<RevenuePage />} />
            <Route path="/settings/team" element={<TeamPage />} />
            <Route
              path="/settings/integrations"
              element={<IntegrationsPage />}
            />
            <Route path="*" element={<Navigate to="/operations" replace />} />
          </Routes>
        </Suspense>
      </AdminShell>
    </RequireRole>
  );
}

function App() {
  return (
    <Suspense fallback={<RouteFallback />}>
      <AppRoutes />
    </Suspense>
  );
}

export default App;
