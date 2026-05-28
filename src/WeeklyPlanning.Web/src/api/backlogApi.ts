import type { BacklogItemDto } from '../types/backlog';
import { getToken, handleUnauthorized } from './weeklyPlanApi';

const BASE_URL = import.meta.env.VITE_API_BASE_URL ?? '/api';

async function request<T>(path: string, options?: RequestInit): Promise<T> {
  const headers = new Headers(options?.headers ?? {});
  headers.set('Content-Type', 'application/json');
  headers.set('Accept', 'application/json');

  const token = getToken();
  if (token) {
    headers.set('Authorization', `Bearer ${token}`);
  }

  const res = await fetch(`${BASE_URL}${path}`, {
    ...options,
    headers,
  });

  if (res.status === 401) {
    handleUnauthorized();
    throw new Error('Sesión expirada.');
  }

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
