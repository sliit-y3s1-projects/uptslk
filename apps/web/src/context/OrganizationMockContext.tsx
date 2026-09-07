import { createContext, useContext, useState, type ReactNode } from "react";
import { centres as initialCentres, type Centre } from "@/mock/centres";
import { employees as initialEmployees, type Employee } from "@/mock/organization";

type OrganizationStore = {
  centres: Centre[];
  employees: Employee[];
  saveCentre: (centre: Centre) => void;
  saveEmployee: (employee: Employee) => void;
  toggleEmployee: (employeeId: string) => void;
};

const OrganizationMockContext = createContext<OrganizationStore | undefined>(undefined);

export function OrganizationMockProvider({ children }: { children: ReactNode }) {
  const [centres, setCentres] = useState(() => structuredClone(initialCentres));
  const [employees, setEmployees] = useState(() => structuredClone(initialEmployees));
  function saveCentre(centre: Centre) { setCentres((current) => current.some((item) => item.id === centre.id) ? current.map((item) => item.id === centre.id ? centre : item) : [centre, ...current]); }
  function saveEmployee(employee: Employee) { setEmployees((current) => current.some((item) => item.id === employee.id) ? current.map((item) => item.id === employee.id ? employee : item) : [employee, ...current]); }
  function toggleEmployee(employeeId: string) { setEmployees((current) => current.map((item) => item.id === employeeId ? { ...item, status: item.status === "Suspended" ? "Active" : "Suspended" } : item)); }
  return <OrganizationMockContext.Provider value={{ centres, employees, saveCentre, saveEmployee, toggleEmployee }}>{children}</OrganizationMockContext.Provider>;
}

export function useOrganizationMock() {
  const context = useContext(OrganizationMockContext);
  if (!context) throw new Error("useOrganizationMock must be used within OrganizationMockProvider");
  return context;
}
