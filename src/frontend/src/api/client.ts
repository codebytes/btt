import type {
  AddTabItemInput,
  CreateTabInput,
  HistoryTab,
  LeaderboardEntry,
  ReverseGeocodeResult,
  Tab,
  TabDetail,
  User,
} from './types';

const API_BASE_URL = '/api';

type ApiClientOptions = Omit<RequestInit, 'body' | 'credentials'> & {
  body?: unknown;
};

type ProblemDetails = {
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
};

export class ApiError extends Error {
  status: number;
  statusText: string;
  details?: ProblemDetails;

  constructor(message: string, status: number, statusText: string, details?: ProblemDetails) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.statusText = statusText;
    this.details = details;
  }
}

async function readProblemDetails(response: Response): Promise<ProblemDetails | undefined> {
  const text = await response.text();

  if (!text) {
    return undefined;
  }

  try {
    return JSON.parse(text) as ProblemDetails;
  } catch {
    return { detail: text };
  }
}

function formatApiError(response: Response, details?: ProblemDetails) {
  const validationMessages = details?.errors ? Object.values(details.errors).flat() : [];
  return validationMessages[0] ?? details?.detail ?? details?.title ?? `API request failed: ${response.status} ${response.statusText}`;
}

export async function apiClient<T>(path: string, options: ApiClientOptions = {}): Promise<T> {
  const { body, headers, ...requestOptions } = options;
  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...requestOptions,
    credentials: 'include',
    headers: {
      Accept: 'application/json',
      ...(body ? { 'Content-Type': 'application/json' } : {}),
      ...headers,
    },
    body: body ? JSON.stringify(body) : undefined,
  });

  if (!response.ok) {
    const details = await readProblemDetails(response);
    throw new ApiError(formatApiError(response, details), response.status, response.statusText, details);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

export function getMe(): Promise<User> {
  return apiClient<User>('/me');
}

export function devLogin(): Promise<User> {
  return apiClient<User>('/auth/dev-login', {
    method: 'POST',
    body: { displayName: 'Development User' },
  });
}

export function listTabs(): Promise<Tab[]> {
  return apiClient<Tab[]>('/tabs');
}

export function getTab(id: string): Promise<TabDetail> {
  return apiClient<TabDetail>(`/tabs/${encodeURIComponent(id)}`);
}

export function createTab(input: CreateTabInput): Promise<Tab> {
  return apiClient<Tab>('/tabs', {
    method: 'POST',
    body: input,
  });
}

export function closeTab(id: string): Promise<void> {
  return apiClient<void>(`/tabs/${encodeURIComponent(id)}/close`, {
    method: 'POST',
  });
}

export function addTabItem(id: string, input: AddTabItemInput): Promise<void> {
  return apiClient<void>(`/tabs/${encodeURIComponent(id)}/items`, {
    method: 'POST',
    body: input,
  });
}

export function deleteTabItem(id: string, itemId: string): Promise<void> {
  return apiClient<void>(`/tabs/${encodeURIComponent(id)}/items/${encodeURIComponent(itemId)}`, {
    method: 'DELETE',
  });
}

export function joinTab(token: string): Promise<TabDetail> {
  return apiClient<TabDetail>(`/tabs/join/${encodeURIComponent(token)}`, {
    method: 'POST',
  });
}

export function getHistory(): Promise<HistoryTab[]> {
  return apiClient<HistoryTab[]>('/history');
}

export function getLeaderboard(limit = 10): Promise<LeaderboardEntry[]> {
  return apiClient<LeaderboardEntry[]>(`/leaderboard?limit=${encodeURIComponent(limit)}`);
}

export function reverseGeocode(lat: number, lng: number): Promise<ReverseGeocodeResult> {
  return apiClient<ReverseGeocodeResult>(
    `/geocode/reverse?lat=${encodeURIComponent(lat)}&lng=${encodeURIComponent(lng)}`,
  );
}
