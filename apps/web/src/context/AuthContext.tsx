import { useState, useEffect, type ReactNode } from "react";
import { AuthContext } from "./auth-context";
import type { User } from "../types/auth";

const API_BASE = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5000";

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [token, setToken] = useState<string | null>(
    localStorage.getItem("upts_token"),
  );
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    async function loadUser() {
      if (!token) {
        setLoading(false);
        return;
      }

      try {
        const res = await fetch(`${API_BASE}/api/v1/auth/me`, {
          headers: { Authorization: `Bearer ${token}` },
        });

        if (!res.ok) throw new Error();
        const data = await res.json();

        setUser({
          id: data.id,
          name: data.name,
          email: data.email,
          role: data.role,
        });
      } catch {
        localStorage.removeItem("upts_token");
        setToken(null);
        setUser(null);
      } finally {
        setLoading(false);
      }
    }

    loadUser();
  }, [token]);

  async function login(email: string, password: string) {
    const res = await fetch(`${API_BASE}/api/v1/auth/login`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ email, password }),
    });
    if (!res.ok) throw new Error("Invalid credentials");
    const data = await res.json();
    localStorage.setItem("upts_token", data.token);
    setToken(data.token);
    setUser({
      id: data.userId,
      name: data.name,
      email: data.email,
      role: data.role,
    });
  }

  async function register(name: string, email: string, password: string) {
    const res = await fetch(`${API_BASE}/api/v1/auth/register`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ name, email, password }),
    });
    if (!res.ok) throw new Error("Registration failed");
    const data = await res.json();
    localStorage.setItem("upts_token", data.token);
    setToken(data.token);
    setUser({
      id: data.userId,
      name: data.name,
      email: data.email,
      role: data.role,
    });
  }

  function logout() {
    localStorage.removeItem("upts_token");
    setToken(null);
    setUser(null);
  }

  return (
    <AuthContext.Provider
      value={{ user, token, login, register, logout, loading }}
    >
      {children}
    </AuthContext.Provider>
  );
}
