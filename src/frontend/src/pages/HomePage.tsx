import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { listTabs, type Tab } from '../api';
import { useAuth } from '../auth/useAuth';
import { formatDate } from '../utils/format';

export function HomePage() {
  const { user, loading: authLoading } = useAuth();
  const [tabs, setTabs] = useState<Tab[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    // Wait for auth to resolve before fetching so the request isn't sent
    // before the dev-login fallback (DEV) or Entra cookie (prod) is ready.
    // The effect re-runs when auth state changes, so a fresh session refetches
    // once authenticated instead of staying on a pre-auth 401.
    if (authLoading) {
      return;
    }

    if (!user) {
      setLoading(false);
      return;
    }

    let active = true;
    setLoading(true);
    setError(null);

    listTabs()
      .then((result) => {
        if (active) {
          setTabs(result);
        }
      })
      .catch(() => {
        if (active) {
          setError('Could not load open tabs yet.');
        }
      })
      .finally(() => {
        if (active) {
          setLoading(false);
        }
      });

    return () => {
      active = false;
    };
  }, [authLoading, user]);

  return (
    <section className="stack page-card hero-card">
      <span className="status-badge">Open tabs</span>
      <h2>Tonight's tabs</h2>
      <p>Start a shared bar tab or jump back into one already in progress.</p>
      <Link className="primary-action" to="/tabs/new">
        Start a tab
      </Link>

      <div className="list-stack">
        {loading ? <p className="muted">Loading tabs…</p> : null}
        {error ? <p className="error-text">{error}</p> : null}
        {!loading && !error && tabs.length === 0 ? <p className="muted">No open tabs yet.</p> : null}
        {tabs.map((tab) => (
          <Link className="list-row" key={tab.id} to={`/tabs/${tab.id}`}>
            <span>
              <strong>{tab.name}</strong>
              <small>{tab.bar?.name ?? 'No bar tagged'} · {formatDate(tab.createdAt)}</small>
            </span>
            <span aria-hidden="true">›</span>
          </Link>
        ))}
      </div>
    </section>
  );
}
