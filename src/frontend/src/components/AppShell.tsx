import { NavLink, Outlet } from 'react-router-dom';
import { useAuth } from '../auth/useAuth';

const navItems = [
  { to: '/', label: 'Tabs', icon: '🍻' },
  { to: '/tabs/new', label: 'New', icon: '➕' },
  { to: '/history', label: 'History', icon: '🧾' },
] as const;

export function AppShell() {
  const { user, logout } = useAuth();

  return (
    <div className="app-frame">
      <header className="top-bar">
        <div>
          <p className="eyebrow">BarTabTracker</p>
          <h1>Split the night, not the math.</h1>
        </div>
        <div className="profile-pill" aria-label={user ? `Signed in as ${user.displayName}` : 'Not signed in'}>
          {user ? <img src={user.avatarUrl} alt="" /> : <span>?</span>}
        </div>
      </header>

      <main className="page-content">
        <Outlet />
      </main>

      <footer className="bottom-nav" aria-label="Primary navigation">
        {navItems.map((item) => (
          <NavLink key={item.to} to={item.to} end={item.to === '/'}>
            <span aria-hidden="true">{item.icon}</span>
            {item.label}
          </NavLink>
        ))}
        {user ? (
          <button type="button" onClick={logout}>
            <span aria-hidden="true">👋</span>
            Log out
          </button>
        ) : (
          <NavLink to="/login">
            <span aria-hidden="true">🔐</span>
            Login
          </NavLink>
        )}
      </footer>
    </div>
  );
}
