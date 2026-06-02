import { useEffect, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { joinTab } from '../api';
import { useAuth } from '../auth/useAuth';

export function JoinTabPage() {
  const { token } = useParams<{ token: string }>();
  const { user, loading: authLoading, login } = useAuth();
  const navigate = useNavigate();
  const [status, setStatus] = useState('Preparing invite…');
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (authLoading) {
      return;
    }

    if (!user) {
      setStatus('Please log in to join this tab.');
      return;
    }

    if (!token) {
      setError('Invite token is missing.');
      return;
    }

    setStatus('Joining tab…');
    joinTab(token)
      .then((tab) => navigate(`/tabs/${tab.id}`, { replace: true }))
      .catch(() => {
        setError('Could not join this tab. The invite may have expired.');
      });
  }, [authLoading, login, navigate, token, user]);

  return (
    <section className="stack page-card">
      <span className="status-badge">Invite link</span>
      <h2>Join a tab</h2>
      {error ? <p className="error-text">{error}</p> : <p>{status}</p>}
      {!authLoading && !user ? (
        <button className="primary-action" type="button" onClick={login}>
          Log in to join
        </button>
      ) : null}
      {error ? <Link className="secondary-link" to="/">Back to tabs</Link> : null}
    </section>
  );
}
