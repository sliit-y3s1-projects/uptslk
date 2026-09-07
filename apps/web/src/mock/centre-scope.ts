import type { TableRow } from "@/mock/mock-data";

export function mockRowCentreId(row: TableRow, index: number) {
  if (row.centreId) return row.centreId;
  if (row.district === "Gampaha") return "kadawatha";
  if (row.district === "Colombo") return "makumbura";
  return index % 2 === 0 ? "makumbura" : "kadawatha";
}
