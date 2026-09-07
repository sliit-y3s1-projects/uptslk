export interface User {
  id: string;
  name: string;
  email: string;
  role: string;
  centreId?: string;
}

export interface AuthContextType {
  user: User | null;
  token: string | null;
  login: (email: string, password: string) => Promise<void>;
  register: (name: string, email: string, password: string) => Promise<void>;
  logout: () => void;
  loginDemo: (role: string, centreId?: string) => void;
  loading: boolean;
}
