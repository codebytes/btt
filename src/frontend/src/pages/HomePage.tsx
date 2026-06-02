import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { listTabs, type Tab } from '../api';
import { formatDate } from '../utils/format';

export function HomePage() {
  const [tabs, setTabs] = useState<Tab[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    listTabs()
      .then(setTabs)
      .catch(() => setError('Could not load open tabs yet.'))
      .finally(() => setLoading(false));
  }, []);

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
