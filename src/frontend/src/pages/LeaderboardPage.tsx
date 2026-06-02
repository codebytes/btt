import { useEffect, useState } from 'react';
import { ApiError, getLeaderboard, type LeaderboardEntry } from '../api';

function leaderboardErrorMessage(error: unknown) {
  if (error instanceof ApiError) {
    if (error.status === 401) {
      return 'You are not signed in. Use Login first to see the leaderboard.';
    }

    return error.message;
  }

  return 'Could not load the leaderboard. Please try again.';
}

function initials(displayName: string) {
  const parts = displayName.trim().split(/\s+/).filter(Boolean);

  if (parts.length === 0) {
    return '?';
  }

  const first = parts[0]?.[0] ?? '';
  const last = parts.length > 1 ? parts[parts.length - 1]?.[0] ?? '' : '';

  return (first + last).toUpperCase() || '?';
}

export function LeaderboardPage() {
  const [entries, setEntries] = useState<LeaderboardEntry[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    getLeaderboard()
      .then(setEntries)
      .catch((err) => setError(leaderboardErrorMessage(err)))
      .finally(() => setLoading(false));
  }, []);

  return (
    <section className="stack page-card">
      <span className="status-badge">Top tab keepers</span>
      <h2>Leaderboard</h2>
      <p>See who has opened the most tabs across BarTabTracker.</p>

      <ol className="list-stack leaderboard-list">
        {loading ? <p className="muted">Loading leaderboard…</p> : null}
        {error ? <p className="error-text">{error}</p> : null}
        {!loading && !error && entries.length === 0 ? (
          <p className="muted">No tabs yet — be the first to start one!</p>
        ) : null}
        {!loading && !error
          ? entries.map((entry, index) => (
              <li className="list-row leaderboard-row" key={entry.userId}>
                <span className="leaderboard-rank" aria-label={`Rank ${index + 1}`}>
                  {index + 1}
                </span>
                {entry.avatarUrl ? (
                  <img className="leaderboard-avatar" src={entry.avatarUrl} alt="" />
                ) : (
                  <span className="leaderboard-avatar leaderboard-avatar--fallback" aria-hidden="true">
                    {initials(entry.displayName)}
                  </span>
                )}
                <span className="leaderboard-name">
                  <strong>{entry.displayName}</strong>
                </span>
                <strong className="leaderboard-count">
                  {entry.tabCount}
                  <small>{entry.tabCount === 1 ? 'tab' : 'tabs'}</small>
                </strong>
              </li>
            ))
          : null}
      </ol>
    </section>
  );
}
