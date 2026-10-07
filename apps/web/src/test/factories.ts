import type { User } from "@/types/auth";

/** Roles the API issues (UserRole enum in apps/api). */
export const ROLES = ["Admin", "CentreManager", "Dispatcher", "FleetOfficer", "Driver", "Commuter"] as const;
export type Role = (typeof ROLES)[number];

/** A valid user. Override only the fields a test cares about. */
export function buildUser(overrides: Partial<User> = {}): User {
  return {
    id: "00000000-0000-4000-8000-000000000001",
    name: "Test User",
    email: "test.user@upts.test",
    role: "Admin",
    isActive: true,
    ...overrides,
  };
}

/** A staff user assigned to a centre, like a real Centre Manager login. */
export const buildStaff = (role: Role = "CentreManager", centreId = "00000000-0000-4000-8000-0000000000c1") =>
  buildUser({ role, centreId, name: `${role} User`, email: `${role.toLowerCase()}@upts.test` });
