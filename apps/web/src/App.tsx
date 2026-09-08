import { useAuth } from "@/hooks/useAuth";
import { SignInPanel } from "@/components/auth/SignInPanel";
import { Navigate, Route, Routes } from "react-router";
import { AdminShell } from "@/components/layout/AdminShell";
import { MaintenancePage } from "@/features/fleet/FleetPages";
import { VehicleFormPage, VehicleProfilePage, VehiclesPage } from "@/features/fleet/VehiclesPage";

function App() {
  const { user, loading } = useAuth();

  if (loading) return null;

  if (!user) return <SignInPanel />;

  return (
    <AdminShell>
      <Routes>
        <Route path="/" element={<Navigate to="/fleet/vehicles" replace />} />
        <Route path="/fleet/vehicles" element={<VehiclesPage />} />
        <Route path="/fleet/vehicles/new" element={<VehicleFormPage />} />
        <Route path="/fleet/vehicles/:vehicleId" element={<VehicleProfilePage />} />
        <Route path="/fleet/vehicles/:vehicleId/edit" element={<VehicleFormPage />} />
        <Route path="/fleet/maintenance" element={<MaintenancePage />} />
        <Route path="*" element={<Navigate to="/fleet/vehicles" replace />} />
      </Routes>
    </AdminShell>
  );
}

export default App;

