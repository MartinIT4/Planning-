import type { BacklogItemDto } from '../types/backlog';

const BASE_URL = import.meta.env.VITE_API_BASE_URL ?? '/api';

async function request<T>(path: string, options?: RequestInit): Promise<T> {
  const res = await fetch(`${BASE_URL}${path}`, {
    headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
    ...options,
  });
  if (!res.ok) {
    const body = await res.text().catch(() => '');
    throw new Error(`${options?.method ?? 'GET'} ${path} → ${res.status}: ${body.slice(0, 200)}`);
  }
  if (res.status === 204) return undefined as T;
  return res.json() as Promise<T>;
}

export interface CreateBacklogItemRequest {
  title: string;
  description?: string;
  projectId?: string;
  projectName?: string;
  estimatedHours: number;
}

export const backlogApi = {
  getAll: () => request<BacklogItemDto[]>('/backlog'),
  seed: () => request<{ message: string }>('/backlog/seed', { method: 'POST' }),
  create: (body: CreateBacklogItemRequest) =>
    request<BacklogItemDto>('/backlog', { method: 'POST', body: JSON.stringify(body) }),
  update: (id: string, body: CreateBacklogItemRequest) =>
    request<BacklogItemDto>(`/backlog/${id}`, { method: 'PUT', body: JSON.stringify(body) }),
  delete: (id: string) =>
    request<void>(`/backlog/${id}`, { method: 'DELETE' }),
};
