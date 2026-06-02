import { useAuth } from '../auth/useAuth';

export function LoginPage() {
  const { user, login, loading } = useAuth();

  return (
    <section className="stack page-card">
      <span className="status-badge">Auth stub</span>
      <h2>Log in</h2>
      <p>OAuth will land here later. The shell currently uses a mock user so every route stays navigable.</p>
      <button className="primary-action" type="button" onClick={login} disabled={loading}>
        Continue with provider
      </button>
      {user ? <p className="muted">Mock session active for {user.displayName}.</p> : null}
    </section>
  );
}
