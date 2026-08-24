import { useAuth } from "@/hooks/useAuth";
import { SignInPanel } from "@/components/auth/SignInPanel";
import { DashboardPage } from "@/pages/DashboardPage";
import { RequireRole } from "./components/auth/RequireAuth";

function App() {
  const { user, loading } = useAuth();

  if (loading) return null;

  if (!user) return <SignInPanel />;

  return (
    <RequireRole role="Admin">
      <DashboardPage />
    </RequireRole>
  );
}

export default App;
