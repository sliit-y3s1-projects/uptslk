import { useEffect, useState } from "react";
import { useAuth } from "@/hooks/useAuth";
import { apiClient } from "@/lib/api/api-client";

function genderFromNic(value: string) {
  const normalized = value.trim().toUpperCase();
  const digits =
    normalized.length === 10
      ? normalized.slice(4, 7)
      : normalized.length >= 9
        ? normalized.slice(2, 5)
        : "";
  const day = Number(digits);
  return day >= 1 && day <= 866 ? (day > 500 ? "Female" : "Male") : "";
}

export function useProfileEditor() {
  const { user, setProfilePhotoUrl, setProfile } = useAuth();
  const [editing, setEditing] = useState(false);
  const [saving, setSaving] = useState(false);
  const [name, setName] = useState(user?.name ?? "");
  const [location, setLocation] = useState(user?.homeLocation ?? "");
  const [nic, setNic] = useState(user?.nicNumber ?? "");
  const [photo, setPhoto] = useState<string | null>(
    user?.profilePhotoUrl ?? null,
  );
  const [photoFile, setPhotoFile] = useState<File | null>(null);
  const [saveError, setSaveError] = useState<string | null>(null);
  const [editSnapshot, setEditSnapshot] = useState({
    name: user?.name ?? "",
    location: user?.homeLocation ?? "",
    nic: user?.nicNumber ?? "",
    photo: user?.profilePhotoUrl ?? null,
  });
  const gender = genderFromNic(nic) || user?.gender || "";
  async function handleSave() {
    setSaveError(null);
    setSaving(true);
    try {
      const updated = await apiClient<{
        name: string;
        homeLocation: string | null;
        nicNumber: string | null;
        gender: string | null;
      }>(`/api/v1/auth/me`, {
        method: "PATCH",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          name,
          homeLocation: location,
          nicNumber: nic,
          gender,
        }),
      });
      setProfile(updated);
      let savedPhoto = photo;
      if (photoFile) {
        const form = new FormData();
        form.append("file", photoFile);
        const uploaded = await apiClient<{ profilePhotoUrl: string }>(
          "/api/v1/auth/me/profile-photo",
          { method: "POST", body: form },
        );
        savedPhoto = uploaded.profilePhotoUrl;
        setPhoto(uploaded.profilePhotoUrl);
        setProfilePhotoUrl(uploaded.profilePhotoUrl);
        setPhotoFile(null);
      }
      setEditSnapshot({ name, location, nic, photo: savedPhoto });
      setEditing(false);
    } catch (cause) {
      setSaveError(
        cause instanceof Error
          ? cause.message
          : "Could not update your profile.",
      );
    } finally {
      setSaving(false);
    }
  }

  function handleEdit() {
    setSaveError(null);
    setEditSnapshot({ name, location, nic, photo });
    setEditing(true);
  }

  function handleCancel() {
    setName(editSnapshot.name);
    setLocation(editSnapshot.location);
    setNic(editSnapshot.nic);
    setPhoto(editSnapshot.photo);
    setPhotoFile(null);
    setSaveError(null);
    setEditing(false);
  }

  useEffect(
    () => () => {
      if (photo?.startsWith("blob:")) URL.revokeObjectURL(photo);
    },
    [photo],
  );

  useEffect(() => {
    const value = nic.trim().toUpperCase();
    if (!editing || !/^(\d{9}[VX]|\d{12})$/.test(value)) return;
    void apiClient("/api/v1/auth/me/verify-nic", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ nicNumber: value }),
    });
  }, [editing, nic]);
  const initials = name
    .split(" ")
    .map((part) => part[0])
    .join("")
    .slice(0, 2)
    .toUpperCase();
  function handlePhoto(event: React.ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0];
    if (!file) return;
    if (!["image/jpeg", "image/png", "image/webp"].includes(file.type)) {
      setSaveError("Choose a JPG, PNG, or WebP image.");
      event.target.value = "";
      return;
    }
    if (file.size > 5 * 1024 * 1024) {
      setSaveError("Profile images cannot exceed 5 MB.");
      event.target.value = "";
      return;
    }
    setSaveError(null);
    setPhotoFile(file);
    setPhoto(URL.createObjectURL(file));
  }

  return {
    user,
    editing,
    saving,
    name,
    setName,
    location,
    setLocation,
    nic,
    setNic,
    photo,
    gender,
    initials,
    saveError,
    handleSave,
    handleEdit,
    handleCancel,
    handlePhoto,
  };
}
