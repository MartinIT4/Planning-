import type { ChobiProjectDto, ChobiSyncResultDto, ChobiUserDto } from '../types/chrobi';
import type { PersonDto } from '../types/weeklyPlan';
import { getToken, handleUnauthorized } from './weeklyPlanApi';
import type { ProjectDto } from './weeklyPlanApi';

const BASE_URL = import.meta.env.VITE_API_BASE_URL ?? '/api';

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const headers = new Headers(init?.headers ?? {});
  headers.set('Content-Type', 'application/json');
  headers.set('Accept', 'application/json');

  const token = getToken();
  if (token) {
    headers.set('Authorization', `Bearer ${token}`);
  }

  const res = await fetch(`${BASE_URL}${path}`, {
    ...init,
    headers,
  });

  if (res.status === 401) {
    handleUnauthorized();
    throw new Error('Sesión expirada.');
  }

  if (!res.ok) {
    const body = await res.text().catch(() => '');
    throw new Error(`${init?.method ?? 'GET'} ${path} → ${res.status}: ${body.slice(0, 200)}`);
  }

  if (res.status === 204) return undefined as T;
  return res.json() as Promise<T>;
}

export const chobiApi = {
  getUsers: () => request<ChobiUserDto[]>('/chrobi/users'),
  getProjects: () => request<ChobiProjectDto[]>('/chrobi/projects'),
  sync: () => request<ChobiSyncResultDto>('/chrobi/sync', { method: 'POST' }),
  setPersonChobiId: (id: string, chobiUserId: number) =>
    request<PersonDto>(`/persons/${id}/chobi-user-id`, {
      method: 'PATCH',
      body: JSON.stringify({ chobiUserId }),
    }),
  setProjectChobiId: (id: string, chobiProjectId: number) =>
    request<ProjectDto>(`/projects/${id}/chobi-project-id`, {
      method: 'PATCH',
      body: JSON.stringify({ chobiProjectId }),
    }),
};
