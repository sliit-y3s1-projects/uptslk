export interface User {
  id: string;
  name: string;
  email: string;
  role: string;
  centreId?: string;
  isActive?: boolean;
  homeLocation?: string | null;
  nicNumber?: string | null;
  gender?: string | null;
  profilePhotoUrl?: string | null;
}

export interface AuthContextType {
  user: User | null;
  token: string | null;
  login: (email: string, password: string) => Promise<string>;
  register: (
    name: string,
    email: string,
    password: string,
    profilePhoto?: File,
  ) => Promise<string | null>;
  setProfilePhotoUrl: (url: string) => void;
  logout: () => void;
  loading: boolean;
}
