import { useEffect, useMemo, useState, type ReactNode } from 'react';
import { devLogin, getMe } from '../api';
import { AuthContext, mockUser, type AuthContextValue } from './authContextCore';

type AuthProviderProps = {
  children: ReactNode;
};

export function AuthProvider({ children }: AuthProviderProps) {
  const [user, setUser] = useState<AuthContextValue['user']>(mockUser);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let active = true;

    async function loadCurrentUser() {
      try {
        const currentUser = await getMe();
        if (active) {
          setUser(currentUser);
        }
      } catch {
        if (import.meta.env.DEV) {
          try {
            const devUser = await devLogin();
            if (active) {
              setUser(devUser);
            }
            return;
          } catch {
            // TODO: remove mock fallback once auth is always available in local dev.
          }
        }

        if (active) {
          setUser(import.meta.env.DEV ? mockUser : null);
        }
      } finally {
        if (active) {
          setLoading(false);
        }
      }
    }

    void loadCurrentUser();

    return () => {
      active = false;
    };
  }, []);

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      loading,
      login: async () => {
        if (import.meta.env.DEV) {
          setLoading(true);
          try {
            setUser(await devLogin());
          } finally {
            setLoading(false);
          }
          return;
        }

        window.location.assign('/api/auth/login');
      },
      logout: () => {
        setUser(null);
      },
    }),
    [loading, user],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

