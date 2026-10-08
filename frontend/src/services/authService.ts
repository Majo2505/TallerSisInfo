declare const process: {
  env: {
    NEXT_PUBLIC_API_URL?: string;
  };
};

const API_URL =
  (typeof process !== 'undefined' && process.env?.NEXT_PUBLIC_API_URL) ||
  'http://localhost:5000/api/auth';

const TOKEN_KEY = 'jwtToken';
const USER_KEY = 'userData';

export interface AuthResponse {
  token: string;
  userId: number;
  nombre: string;
  email: string;
}

export const authService = {
  async login(email: string, password: string): Promise<AuthResponse> {
    const response = await fetch(`${API_URL}/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email, password }),
    });

    const data = await response.json();
    if (!response.ok) {
      throw new Error(data.message || 'Credenciales inválidas.');
    }

    if (typeof window !== 'undefined') {
      sessionStorage.setItem(TOKEN_KEY, data.token);
      sessionStorage.setItem(USER_KEY, JSON.stringify(data));
    }

    return data;
  },

  async register(nombre: string, email: string, password: string): Promise<AuthResponse> {
    const response = await fetch(`${API_URL}/register`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ nombre, email, password }),
    });

    const data = await response.json();
    if (!response.ok) {
      throw new Error(data.message || 'Error durante el registro.');
    }

    if (typeof window !== 'undefined') {
      sessionStorage.setItem(TOKEN_KEY, data.token);
      sessionStorage.setItem(USER_KEY, JSON.stringify(data));
    }

    return data;
  },

  logout(): void {
    if (typeof window !== 'undefined') {
      sessionStorage.removeItem(TOKEN_KEY);
      sessionStorage.removeItem(USER_KEY);
    }
  },

  getToken(): string | null {
    if (typeof window === 'undefined') return null;
    return sessionStorage.getItem(TOKEN_KEY);
  },

  getUser(): AuthResponse | null {
    if (typeof window === 'undefined') return null;
    const user = sessionStorage.getItem(USER_KEY);
    try {
      return user ? JSON.parse(user) : null;
    } catch {
      return null;
    }
  },

  isAuthenticated(): boolean {
    return !!this.getToken();
  },
};