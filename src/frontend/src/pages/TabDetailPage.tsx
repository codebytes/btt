import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react';
import { QRCodeSVG } from 'qrcode.react';
import { useParams } from 'react-router-dom';
import { addTabItem, closeTab, deleteTabItem, getTab, type AddTabItemInput, type TabDetail } from '../api';
import { useAuth } from '../auth/useAuth';
import { BarMap } from '../components/BarMap';
import { formatDate, formatMoney } from '../utils/format';

type SplitMode = 'equal' | 'itemized';

type ItemFormState = {
  name: string;
  price: string;
  quantity: string;
  orderedByUserId: string;
};

const initialItemForm: ItemFormState = {
  name: '',
  price: '',
  quantity: '1',
  orderedByUserId: 'shared',
};

function memberLabel(userId: string, currentUserId?: string) {
  return userId === currentUserId ? 'You' : `Member ${userId.slice(0, 6)}`;
}

export function TabDetailPage() {
  const { id } = useParams<{ id: string }>();
  const { user } = useAuth();
  const [detail, setDetail] = useState<TabDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [itemForm, setItemForm] = useState<ItemFormState>(initialItemForm);
  const [splitMode, setSplitMode] = useState<SplitMode>('equal');
  const [saving, setSaving] = useState(false);
  const [copyState, setCopyState] = useState('Copy');

  const loadTab = useCallback(async () => {
    if (!id) {
      return;
    }

    setLoading(true);
    setError(null);

    try {
      setDetail(await getTab(id));
    } catch {
      setError('Could not load this tab.');
    } finally {
      setLoading(false);
    }
  }, [id]);

  useEffect(() => {
    void loadTab();
  }, [loadTab]);

  const tab = detail;
  const total = useMemo(() => detail?.split.total ?? 0, [detail?.split.total]);
  const readOnly = tab?.status === 'closed';
  const isOwner = Boolean(user && tab && tab.ownerId === user.id);
  const inviteUrl = tab ? `${window.location.origin}/join/${tab.inviteToken}` : '';

  const handleAddItem = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    if (!id) {
      return;
    }

    const input: AddTabItemInput = {
      name: itemForm.name,
      price: Number(itemForm.price),
      quantity: Number(itemForm.quantity),
      orderedByUserId: itemForm.orderedByUserId === 'shared' ? null : itemForm.orderedByUserId,
    };

    setSaving(true);
    setError(null);

    try {
      await addTabItem(id, input);
      await loadTab();
      setItemForm(initialItemForm);
    } catch {
      setError('Could not add that item.');
    } finally {
      setSaving(false);
    }
  };

  const handleDeleteItem = async (itemId: string) => {
    if (!id) {
      return;
    }

    setSaving(true);
    setError(null);

    try {
      await deleteTabItem(id, itemId);
      await loadTab();
    } catch {
      setError('Could not remove that item.');
    } finally {
      setSaving(false);
    }
  };

  const handleClose = async () => {
    if (!id) {
      return;
    }

    setSaving(true);
    setError(null);

    try {
      await closeTab(id);
      await loadTab();
    } catch {
      setError('Could not close this tab.');
    } finally {
      setSaving(false);
    }
  };

  const handleCopy = async () => {
    try {
      await navigator.clipboard.writeText(inviteUrl);
      setCopyState('Copied');
      window.setTimeout(() => setCopyState('Copy'), 1800);
    } catch {
      setCopyState('Copy failed');
    }
  };

  if (loading) {
    return <section className="page-card"><p className="muted">Loading tab…</p></section>;
  }

  if (!detail || !tab) {
    return <section className="page-card"><p className="error-text">{error ?? 'Tab not found.'}</p></section>;
  }

  const splitRows = Object.entries(detail.split[splitMode]).map(([userId, amount]) => ({ userId, amount }));

  return (
    <section className="stack detail-screen">
      <div className="page-card stack">
        <span className="status-badge">{tab.status}</span>
        <div className="title-row">
          <div>
            <h2>{tab.name}</h2>
            <p>{tab.bar?.name ?? 'No bar tagged'} · {formatDate(tab.createdAt)}</p>
          </div>
          <strong className="total-pill">{formatMoney(total, tab.currency)}</strong>
        </div>

        <div className="member-chips" aria-label="Tab members">
          {tab.memberIds.map((memberId) => (
            <span key={memberId}>{memberLabel(memberId, user?.id)}</span>
          ))}
        </div>

        {tab.bar ? <BarMap bar={tab.bar} compact /> : null}

        {tab.status === 'open' ? (
          <div className="invite-card">
            <div>
              <strong>Invite friends</strong>
              <p>{inviteUrl}</p>
            </div>
            <QRCodeSVG value={inviteUrl} size={92} />
            <button className="secondary-action" type="button" onClick={handleCopy}>{copyState}</button>
          </div>
        ) : null}

        {isOwner && tab.status === 'open' ? (
          <button className="danger-action" type="button" onClick={handleClose} disabled={saving}>
            Close tab
          </button>
        ) : null}
      </div>

      {!readOnly ? (
        <form className="page-card form-stack" onSubmit={handleAddItem}>
          <span className="status-badge">Add item</span>
          <label>
            Item name
            <input value={itemForm.name} onChange={(event) => setItemForm({ ...itemForm, name: event.target.value })} placeholder="Margarita" required />
          </label>
          <div className="form-grid">
            <label>
              Price
              <input value={itemForm.price} onChange={(event) => setItemForm({ ...itemForm, price: event.target.value })} inputMode="decimal" min="0" step="0.01" type="number" required />
            </label>
            <label>
              Qty
              <input value={itemForm.quantity} onChange={(event) => setItemForm({ ...itemForm, quantity: event.target.value })} inputMode="numeric" min="1" type="number" required />
            </label>
          </div>
          <label>
            Who ordered?
            <select value={itemForm.orderedByUserId} onChange={(event) => setItemForm({ ...itemForm, orderedByUserId: event.target.value })}>
              <option value="shared">Shared</option>
              {tab.memberIds.map((memberId) => (
                <option key={memberId} value={memberId}>{memberLabel(memberId, user?.id)}</option>
              ))}
            </select>
          </label>
          {error ? <p className="error-text">{error}</p> : null}
          <button className="primary-action" type="submit" disabled={saving}>Add item</button>
        </form>
      ) : null}

      <div className="page-card stack">
        <span className="status-badge">Items</span>
        <div className="list-stack">
          {detail.items.length === 0 ? <p className="muted">No items yet.</p> : null}
          {detail.items.map((item) => (
            <div className="list-row" key={item.id}>
              <span>
                <strong>{item.name}</strong>
                <small>
                  {item.quantity} × {formatMoney(item.price, tab.currency)} · {item.orderedByUserId ? memberLabel(item.orderedByUserId, user?.id) : 'Shared'}
                </small>
              </span>
              {!readOnly ? (
                <button className="icon-button" type="button" onClick={() => void handleDeleteItem(item.id)} aria-label={`Remove ${item.name}`}>
                  ×
                </button>
              ) : null}
            </div>
          ))}
        </div>
      </div>

      <div className="page-card stack">
        <span className="status-badge">Split</span>
        <div className="segmented-control" role="tablist" aria-label="Split mode">
          <button className={splitMode === 'equal' ? 'active' : ''} type="button" onClick={() => setSplitMode('equal')}>Equal</button>
          <button className={splitMode === 'itemized' ? 'active' : ''} type="button" onClick={() => setSplitMode('itemized')}>Itemized</button>
        </div>
        <div className="list-stack">
          {splitRows.map((row) => (
            <div className="list-row" key={row.userId}>
              <span><strong>{memberLabel(row.userId, user?.id)}</strong></span>
              <strong>{formatMoney(row.amount, tab.currency)}</strong>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}
