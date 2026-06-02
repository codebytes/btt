import { Link } from 'react-router-dom';

export function NotFoundPage() {
  return (
    <section className="stack page-card">
      <span className="status-badge">404</span>
      <h2>Page not found</h2>
      <p>This route is not part of the BarTabTracker shell.</p>
      <Link className="primary-action" to="/">
        Back to tabs
      </Link>
    </section>
  );
}
