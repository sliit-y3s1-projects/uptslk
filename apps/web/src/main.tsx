import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import "./index.css";
import { ThunderIDProvider } from "@thunderid/react";
import App from "./App.tsx";

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <ThunderIDProvider
      clientId="u4-adSBdDIiHSb-3__Hb1A"
      baseUrl="https://localhost:8090"
    >
      <App />
    </ThunderIDProvider>
  </StrictMode>,
);
