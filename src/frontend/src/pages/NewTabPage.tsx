import { useEffect, useState, type FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { ApiError, createTab, reverseGeocode, type BarLocation } from '../api';
import { BarMap } from '../components/BarMap';

type LocationStatus = 'idle' | 'loading' | 'ready' | 'error';

type NewTabDraft = {
  name: string;
  currency: string;
  bar?: BarLocation;
  locationStatus: LocationStatus;
  locationMessage: string;
};

const draftStorageKey = 'btt:new-tab-draft';
const defaultDraft: NewTabDraft = {
  name: '',
  currency: 'USD',
  bar: undefined,
  locationStatus: 'idle',
  locationMessage: '',
};

function roundCoordinate(value: number) {
  return Number(value.toFixed(4));
}

function loadDraft(): NewTabDraft {
  const storedDraft = window.sessionStorage.getItem(draftStorageKey);

  if (!storedDraft) {
    return defaultDraft;
  }

  try {
    return { ...defaultDraft, ...(JSON.parse(storedDraft) as Partial<NewTabDraft>) };
  } catch {
    return defaultDraft;
  }
}

function createTabErrorMessage(error: unknown) {
  if (error instanceof ApiError) {
    if (error.status === 401) {
      return 'You are not signed in. Use Log in first, then try creating the tab again.';
    }

    return error.message;
  }

  return 'Could not create the tab. Please try again.';
}

export function NewTabPage() {
  const navigate = useNavigate();
  const [draft, setDraft] = useState<NewTabDraft>(loadDraft);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    window.sessionStorage.setItem(draftStorageKey, JSON.stringify(draft));
  }, [draft]);

  const requestLocation = () => {
    setDraft((current) => ({ ...current, locationMessage: '' }));
    setError(null);

    if (!navigator.geolocation) {
      setDraft((current) => ({
        ...current,
        locationStatus: 'error',
        locationMessage: 'Geolocation is not available in this browser. You can still create a tab.',
      }));
      return;
    }

    setDraft((current) => ({ ...current, locationStatus: 'loading' }));
    navigator.geolocation.getCurrentPosition(
      async (position) => {
        const lat = roundCoordinate(position.coords.latitude);
        const lng = roundCoordinate(position.coords.longitude);
        let nextBar: BarLocation = { name: 'Bar location', lat, lng };
        let nextMessage = 'One-time bar location captured. Bar lookup is not available yet, so defaults were kept.';
        let nextCurrency = draft.currency;

        try {
          const result = await reverseGeocode(lat, lng);
          nextBar = {
            name: result.barName ?? `${result.country} bar`,
            lat,
            lng,
          };
          nextCurrency = result.currency;
          nextMessage = 'One-time bar location captured for this tab. No continuous tracking is used.';
        } catch {
          // Keep the captured map pin even if reverse geocoding is not available yet.
        }

        setDraft((current) => ({
          ...current,
          bar: nextBar,
          currency: nextCurrency,
          locationStatus: 'ready',
          locationMessage: nextMessage,
        }));
      },
      () => {
        setDraft((current) => ({
          ...current,
          locationStatus: 'error',
          locationMessage: 'Location permission was denied or unavailable. You can continue without a map pin.',
        }));
      },
      { enableHighAccuracy: false, timeout: 10000 },
    );
  };

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setSubmitting(true);
    setError(null);

    const trimmedName = draft.name.trim();
    const trimmedCurrency = draft.currency.trim().toUpperCase();
    const bar = draft.bar && draft.bar.name.trim().length > 0 ? { ...draft.bar, name: draft.bar.name.trim() } : undefined;

    try {
      const tab = await createTab({ name: trimmedName, currency: trimmedCurrency, bar });
      window.sessionStorage.removeItem(draftStorageKey);
      setDraft(defaultDraft);
      navigate(`/tabs/${tab.id}`);
    } catch (submitError) {
      setError(createTabErrorMessage(submitError));
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <section className="stack page-card">
      <span className="status-badge">Create flow</span>
      <h2>Start a new tab</h2>
      <p>Set the basics now. Location is optional and can be skipped if friends are already ordering.</p>

      <form className="form-stack" onSubmit={handleSubmit}>
        <label>
          Tab name
          <input
            value={draft.name}
            onChange={(event) => setDraft((current) => ({ ...current, name: event.target.value }))}
            placeholder="Friday at The Corner Bar"
            required
          />
        </label>

        <label>
          Currency
          <input
            value={draft.currency}
            onChange={(event) => setDraft((current) => ({ ...current, currency: event.target.value.toUpperCase() }))}
            maxLength={3}
            required
          />
        </label>

        <div className="location-panel">
          <div>
            <strong>Optional bar map pin</strong>
            <p>
              If you choose <strong>Use my location</strong>, the browser asks once for GPS so we can tag the bar's
              location on this tab and prefill local currency. This is optional and is not continuous tracking.
            </p>
          </div>
          <button
            className="secondary-action"
            type="button"
            onClick={requestLocation}
            disabled={draft.locationStatus === 'loading'}
            aria-describedby="location-consent-note"
          >
            {draft.locationStatus === 'loading' ? 'Locating…' : 'Use my location'}
          </button>
        </div>

        {draft.bar ? (
          <label>
            Bar name
            <input
              value={draft.bar.name}
              onChange={(event) => setDraft((current) => ({
                ...current,
                bar: current.bar ? { ...current.bar, name: event.target.value } : current.bar,
              }))}
              required
            />
          </label>
        ) : null}

        {draft.bar ? (
          <div className="stack location-summary">
            <BarMap bar={draft.bar} compact />
            <p className="muted" id="location-consent-note">
              Map pin for the bar only: {draft.bar.lat.toFixed(4)}, {draft.bar.lng.toFixed(4)}. You can still create the tab
              without saving a location.
            </p>
          </div>
        ) : (
          <p className="muted" id="location-consent-note">
            No location will be requested or saved unless you tap Use my location.
          </p>
        )}
        {draft.locationMessage ? <p className={draft.locationStatus === 'error' ? 'error-text' : 'muted'}>{draft.locationMessage}</p> : null}
        {error ? <p className="error-text" role="alert">{error}</p> : null}

        <button className="primary-action" type="submit" disabled={submitting || draft.name.trim().length === 0}>
          {submitting ? 'Creating…' : 'Create tab'}
        </button>
      </form>
    </section>
  );
}
