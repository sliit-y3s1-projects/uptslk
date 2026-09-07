import { createContext, useContext, useState, type ReactNode } from "react";
import { collections, type TableRow } from "@/mock/mock-data";

type MockDataStore = {
  data: Record<string, TableRow[]>;
  saveRecord: (collection: string, record: TableRow) => void;
  deactivateRecord: (collection: string, id: string) => void;
};

const MockDataContext = createContext<MockDataStore | undefined>(undefined);

export function MockDataProvider({ children }: { children: ReactNode }) {
  const [data, setData] = useState(() => structuredClone(collections));
  function saveRecord(collection: string, record: TableRow) {
    setData((current) => {
      const records = current[collection] ?? [];
      const exists = records.some((item) => item.id === record.id);
      return { ...current, [collection]: exists ? records.map((item) => item.id === record.id ? record : item) : [record, ...records] };
    });
  }
  function deactivateRecord(collection: string, id: string) {
    setData((current) => ({ ...current, [collection]: (current[collection] ?? []).map((item) => item.id === id ? { ...item, status: "Inactive", tone: "danger" as const, updated: "Deactivated just now" } : item) }));
  }
  return <MockDataContext.Provider value={{ data, saveRecord, deactivateRecord }}>{children}</MockDataContext.Provider>;
}

export function useMockData() {
  const context = useContext(MockDataContext);
  if (!context) throw new Error("useMockData must be used within MockDataProvider");
  return context;
}
