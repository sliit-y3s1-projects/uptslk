import { useState, useEffect, type ReactNode } from "react";
import { AuthContext } from "./auth-context";
import type { User } from "../types/auth";
import { API_BASE_URL } from "@/lib/api/api-client";

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [token, setToken] = useState<string | null>("cookie-session");
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    async function loadUser() {
      if (!token) {
        setLoading(false);
        return;
      }

      try {
        let res = await fetch(`${API_BASE_URL}/api/v1/auth/me`, {
          credentials: "include",
        });
        if (res.status === 401) {
          const refresh = await fetch(`${API_BASE_URL}/api/v1/auth/refresh`, {
            method: "POST",
            credentials: "include",
          });
          if (refresh.ok) {
            res = await fetch(`${API_BASE_URL}/api/v1/auth/me`, {
              credentials: "include",
            });
          }
        }

        if (!res.ok) throw new Error();
        const data = await res.json();

        setUser({
          id: data.id,
          name: data.name,
          email: data.email,
          role: data.role,
          centreId: data.centreId,
          isActive: data.isActive,
          homeLocation: data.homeLocation,
          nicNumber: data.nicNumber,
          gender: data.gender,
          profilePhotoUrl: data.profilePhotoUrl,
        });
      } catch {
        setToken(null);
        setUser(null);
      } finally {
        setLoading(false);
      }
    }

    loadUser();
  }, [token]);

  async function login(email: string, password: string) {
    const res = await fetch(`${API_BASE_URL}/api/v1/auth/login`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ email, password }),
      credentials: "include",
    });
    if (!res.ok) {
      const body = (await res.json().catch(() => null)) as {
        error?: string;
      } | null;
      throw new Error(body?.error ?? "Invalid credentials");
    }
    const data = await res.json();
    setToken("cookie-session");
    setUser({
      id: data.userId,
      name: data.name,
      email: data.email,
      role: data.role,
      centreId: data.centreId,
      isActive: data.isActive,
      homeLocation: data.homeLocation,
      nicNumber: data.nicNumber,
      gender: data.gender,
      profilePhotoUrl: data.profilePhotoUrl,
    });
    return data.role as string;
  }

  async function register(
    name: string,
    email: string,
    password: string,
    profilePhoto?: File,
  ) {
    const res = await fetch(`${API_BASE_URL}/api/v1/auth/register`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ name, email, password }),
      credentials: "include",
    });
    if (!res.ok) throw new Error("Registration failed");
    const data = await res.json();
    let profilePhotoUrl = data.profilePhotoUrl as string | null | undefined;
    let profilePhotoWarning: string | null = null;
    if (profilePhoto) {
      const form = new FormData();
      form.append("file", profilePhoto);
      const upload = await fetch(
        `${API_BASE_URL}/api/v1/auth/me/profile-photo`,
        {
          method: "POST",
          body: form,
          credentials: "include",
        },
      );
      if (upload.ok) {
        const uploaded = (await upload.json()) as { profilePhotoUrl: string };
        profilePhotoUrl = uploaded.profilePhotoUrl;
      } else {
        const body = (await upload.json().catch(() => null)) as {
          error?: string;
          detail?: string;
        } | null;
        profilePhotoWarning =
          body?.error ??
          body?.detail ??
          "Your account was created, but the profile image could not be uploaded.";
      }
    }

    setToken("cookie-session");
    setUser({
      id: data.userId,
      name: data.name,
      email: data.email,
      role: data.role,
      centreId: data.centreId,
      isActive: data.isActive,
      homeLocation: data.homeLocation,
      nicNumber: data.nicNumber,
      gender: data.gender,
      profilePhotoUrl,
    });
    return profilePhotoWarning;
  }

  function logout() {
    void fetch(`${API_BASE_URL}/api/v1/auth/logout`, {
      method: "POST",
      credentials: "include",
    });
    setToken(null);
    setUser(null);
    window.setTimeout(() => {
      window.history.pushState({}, "", "/login");
      window.dispatchEvent(new PopStateEvent("popstate"));
    }, 0);
  }

  function setProfilePhotoUrl(profilePhotoUrl: string) {
    setUser((current) =>
      current ? { ...current, profilePhotoUrl } : current,
    );
  }

  return (
    <AuthContext.Provider
      value={{
        user,
        token,
        login,
        register,
        setProfilePhotoUrl,
        logout,
        loading,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}
