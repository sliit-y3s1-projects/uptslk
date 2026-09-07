import { createContext, useContext, useState, type ReactNode } from "react";

type OperationalScope = {
  district: string;
  setDistrict: (district: string) => void;
  dateRange: string;
  setDateRange: (dateRange: string) => void;
  resetScope: () => void;
};

const OperationalScopeContext = createContext<OperationalScope | undefined>(undefined);

export function OperationalScopeProvider({ children, initialDistrict = "All" }: { children: ReactNode; initialDistrict?: string }) {
  const [district, setDistrict] = useState(initialDistrict);
  const [dateRange, setDateRange] = useState("Today");
  return <OperationalScopeContext.Provider value={{ district, setDistrict, dateRange, setDateRange, resetScope: () => { setDistrict(initialDistrict); setDateRange("Today"); } }}>{children}</OperationalScopeContext.Provider>;
}

export function useOperationalScope() {
  const context = useContext(OperationalScopeContext);
  if (!context) throw new Error("useOperationalScope must be used within OperationalScopeProvider");
  return context;
}
