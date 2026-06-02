import { createContext } from 'react';
import type { User } from '../api';

export type AuthContextValue = {
  user: User | null;
  loading: boolean;
  login: () => Promise<void>;
  logout: () => void;
};

export const mockUser: User = {
  id: 'user-linus',
  displayName: 'Linus',
  avatarUrl: 'https://api.dicebear.com/9.x/thumbs/svg?seed=Linus',
};

export const AuthContext = createContext<AuthContextValue | undefined>(undefined);
