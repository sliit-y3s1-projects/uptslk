import { useAuth } from "@/hooks/useAuth";
import { SignInPanel } from "@/components/auth/SignInPanel";
import { RequireRole } from "./components/auth/RequireAuth";
import { Navigate, Route, Routes } from "react-router";
import { AdminShell } from "@/components/layout/AdminShell";
import { DispatchPage } from "@/features/operations/DispatchPage";
import { BayManagementPage } from "@/features/operations/BayManagementPage";
import { IncidentsPage } from "@/features/operations/IncidentsPage";
import { ApprovalsPage } from "@/features/operations/ApprovalsPage";
import { IncidentFormPage } from "@/features/operations/IncidentFormPage";
import { TripDetailPage } from "@/features/operations/TripDetailPage";
import { TripFormPage } from "@/features/operations/TripFormPage";
import { TripHistoryPage } from "@/features/operations/TripHistoryPage";
import { StopsPage } from "@/features/network/NetworkPages";
import { TimetablesPage } from "@/features/network/TimetablesPage";
import { DriversPage, MaintenancePage } from "@/features/fleet/FleetPages";
import { RouteDetailPage, RouteFormPage, RoutesPage } from "@/features/network/RoutesPage";
import { VehicleFormPage, VehicleProfilePage, VehiclesPage } from "@/features/fleet/VehiclesPage";
import { RidersPage, SupportPage } from "@/features/riders/RiderPages";
import { AssistancePage, PassengerFlowPage } from "@/features/riders/PassengerOperationsPages";
import { PaymentsPage, TicketsPage } from "@/features/fares/FarePages";
import { ReconciliationPage } from "@/features/fares/ReconciliationPage";
import { RidershipPage, RevenuePage } from "@/features/reports/ReportsPages";
import { IntegrationsPage } from "@/features/settings/SettingsPages";
import { TeamPage } from "@/features/settings/TeamPage";
import { CentreConsolePage } from "@/features/centres/CentreConsolePage";
import { CentreFormPage, CentreProfilePage, CentresPage } from "@/features/centres/CentresPage";
import { CentreOperationsPreviewPage, CentreRouteDetailViewPage, CentreRoutesViewPage, CentreVehicleDetailViewPage, CentreVehiclesViewPage } from "@/features/centres/CentreOperationalViews";
import { SuperAdminShell } from "@/components/super-admin/SuperAdminShell";
import { SuperAdminOverviewPage } from "@/features/super-admin/OverviewPage";
import { EmployeesPage } from "@/features/super-admin/EmployeesPage";
import { AccessRequestsPage, RolesPage } from "@/features/super-admin/AccessPages";
import { AuditLogPage, PlatformHealthPage, SystemSettingsPage } from "@/features/super-admin/GovernancePages";

function App() {
  const { user, loading } = useAuth();

  if (loading) return null;

  if (!user) return <SignInPanel />;

  const isSuperAdmin = user.role === "SuperAdmin" || user.role === "Admin";

  if (isSuperAdmin) return <RequireRole role={user.role}><SuperAdminShell><Routes>
    <Route path="/admin" element={<SuperAdminOverviewPage />} />
    <Route path="/admin/centres" element={<CentresPage />} />
    <Route path="/admin/centres/new" element={<CentreFormPage />} />
    <Route path="/admin/centres/:centreId" element={<CentreProfilePage />} />
    <Route path="/admin/centres/:centreId/edit" element={<CentreFormPage />} />
    <Route path="/admin/centres/:centreId/operations" element={<CentreOperationsPreviewPage />} />
    <Route path="/admin/centres/:centreId/routes" element={<CentreRoutesViewPage />} />
    <Route path="/admin/centres/:centreId/routes/:routeId" element={<CentreRouteDetailViewPage />} />
    <Route path="/admin/centres/:centreId/vehicles" element={<CentreVehiclesViewPage />} />
    <Route path="/admin/centres/:centreId/vehicles/:vehicleId" element={<CentreVehicleDetailViewPage />} />
    <Route path="/admin/employees" element={<EmployeesPage />} />
    <Route path="/admin/roles" element={<RolesPage />} />
    <Route path="/admin/access" element={<AccessRequestsPage />} />
    <Route path="/admin/audit" element={<AuditLogPage />} />
    <Route path="/admin/health" element={<PlatformHealthPage />} />
    <Route path="/admin/settings" element={<SystemSettingsPage />} />
    <Route path="*" element={<Navigate to="/admin" replace />} />
  </Routes></SuperAdminShell></RequireRole>;

  return <RequireRole role="CentreManager"><AdminShell><Routes>
    <Route path="/operations" element={<CentreConsolePage />} />
    <Route path="/operations/dispatch" element={<DispatchPage />} />
    <Route path="/operations/dispatch/new" element={<TripFormPage />} />
    <Route path="/operations/dispatch/:tripId" element={<TripDetailPage />} />
    <Route path="/operations/dispatch/:tripId/edit" element={<TripFormPage />} />
    <Route path="/operations/history" element={<TripHistoryPage />} />
    <Route path="/operations/bays" element={<BayManagementPage />} />
    <Route path="/operations/incidents" element={<IncidentsPage />} />
    <Route path="/operations/incidents/new" element={<IncidentFormPage />} />
    <Route path="/operations/approvals" element={<ApprovalsPage />} />
    <Route path="/network/routes" element={<RoutesPage />} /><Route path="/network/routes/new" element={<RouteFormPage />} /><Route path="/network/routes/:routeId" element={<RouteDetailPage />} /><Route path="/network/routes/:routeId/edit" element={<RouteFormPage />} /><Route path="/network/timetables" element={<TimetablesPage />} /><Route path="/network/stops" element={<StopsPage />} />
    <Route path="/fleet/vehicles" element={<VehiclesPage />} /><Route path="/fleet/vehicles/new" element={<VehicleFormPage />} /><Route path="/fleet/vehicles/:vehicleId" element={<VehicleProfilePage />} /><Route path="/fleet/vehicles/:vehicleId/edit" element={<VehicleFormPage />} /><Route path="/fleet/drivers" element={<DriversPage />} /><Route path="/fleet/maintenance" element={<MaintenancePage />} />
    <Route path="/passengers/flow" element={<PassengerFlowPage />} /><Route path="/passengers/assistance" element={<AssistancePage />} /><Route path="/riders/accounts" element={<RidersPage />} /><Route path="/riders/support" element={<SupportPage />} />
    <Route path="/fares/tickets" element={<TicketsPage />} /><Route path="/fares/payments" element={<PaymentsPage />} /><Route path="/fares/reconciliation" element={<ReconciliationPage />} />
    <Route path="/reports/ridership" element={<RidershipPage />} /><Route path="/reports/revenue" element={<RevenuePage />} />
    <Route path="/settings/team" element={<TeamPage />} /><Route path="/settings/integrations" element={<IntegrationsPage />} />
    <Route path="*" element={<Navigate to="/operations" replace />} />
  </Routes></AdminShell></RequireRole>;
}

export default App;
