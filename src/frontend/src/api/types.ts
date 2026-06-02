export type User = {
  id: string;
  displayName: string;
  avatarUrl: string;
};

export type TabStatus = 'open' | 'closed';

export type BarLocation = {
  name: string;
  lat: number;
  lng: number;
};

export type Tab = {
  id: string;
  name: string;
  ownerId: string;
  status: TabStatus;
  currency: string;
  bar?: BarLocation;
  createdAt: string;
  closedAt?: string;
  inviteToken: string;
  memberIds: string[];
};

export type TabItem = {
  id: string;
  tabId: string;
  name: string;
  price: number;
  quantity: number;
  orderedByUserId?: string | null;
};

export type SplitShares = Record<string, number>;

export type TabSplit = {
  total: number;
  equal: SplitShares;
  itemized: SplitShares;
};

export type TabDetail = Tab & {
  items: TabItem[];
  split: TabSplit;
};

export type CreateTabInput = {
  name: string;
  currency: string;
  bar?: BarLocation;
};

export type AddTabItemInput = {
  name: string;
  price: number;
  quantity: number;
  orderedByUserId?: string | null;
};

export type ReverseGeocodeResult = {
  barName?: string;
  country: string;
  currency: string;
};

export type HistoryTab = Tab & {
  total?: number;
  yourShare?: number;
};
