import { SignedIn, SignedOut } from "@thunderid/react";

import { SignInPanel } from "@/components/auth/SignInPanel";
import { DashboardPage } from "@/pages/DashboardPage";

function App() {
  return (
    <>
      <SignedIn>
        <DashboardPage />
      </SignedIn>

      <SignedOut>
        <SignInPanel />
      </SignedOut>
    </>
  );
}

export default App;
