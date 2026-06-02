import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { getHistory, type HistoryTab } from '../api';
import { formatDate, formatMoney } from '../utils/format';

function tabTotal(tab: HistoryTab) {
  return tab.total ?? 0;
}

export function HistoryPage() {
  const [tabs, setTabs] = useState<HistoryTab[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    getHistory()
      .then(setTabs)
      .catch(() => setError('Could not load closed tabs.'))
      .finally(() => setLoading(false));
  }, []);

  return (
    <section className="stack page-card">
      <span className="status-badge">Past tabs</span>
      <h2>History</h2>
      <p>Review closed tabs, receipts, your share, and the bar location.</p>

      <div className="list-stack">
        {loading ? <p className="muted">Loading history…</p> : null}
        {error ? <p className="error-text">{error}</p> : null}
        {!loading && !error && tabs.length === 0 ? <p className="muted">No closed tabs yet.</p> : null}
        {tabs.map((tab) => (
          <Link className="list-row history-row" key={tab.id} to={`/tabs/${tab.id}`}>
            <span>
              <strong>{tab.name}</strong>
              <small>{formatDate(tab.closedAt ?? tab.createdAt)} · {tab.bar?.name ?? 'No bar tagged'}</small>
              <small>Your share: {formatMoney(tab.yourShare ?? 0, tab.currency)}</small>
            </span>
            <strong>{formatMoney(tabTotal(tab), tab.currency)}</strong>
          </Link>
        ))}
      </div>
    </section>
  );
}
