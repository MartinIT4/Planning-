import type {
  WeeklyPlanDto,
  CapacitySummaryDto,
  PersonDto,
  TaskAssignmentDto,
} from '../types/weeklyPlan';

const BASE_URL = import.meta.env.VITE_API_BASE_URL ?? '/api';

export function getToken(): string | null {
  return localStorage.getItem('auth_token');
}

export function clearStoredAuth(): void {
  localStorage.removeItem('auth_token');
  localStorage.removeItem('auth_user');
}

export function handleUnauthorized(): void {
  clearStoredAuth();
  window.location.reload();
}

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

export interface AddAssignmentRequest {
  personId: string;
  externalTaskId: string;
  taskTitle: string;
  plannedHours: number;
  notes?: string;
}

export const weeklyPlanApi = {
  /** Obtiene todos los planes ordenados por fecha descendente. */
  getAll: () => request<WeeklyPlanDto[]>('/weekly-plans'),

  /** Obtiene un plan por su ID, incluye asignaciones. */
  getById: (id: string) => request<WeeklyPlanDto>(`/weekly-plans/${id}`),

  /** Busca el plan de una semana específica (lunes, formato yyyy-MM-dd). */
  getByWeek: (weekStartDate: string) =>
    request<WeeklyPlanDto>(`/weekly-plans/by-week?weekStartDate=${weekStartDate}`),

  /** Envía una asignación al sistema externo (Chrobi). */
  sendAssignmentToExternal: (planId: string, assignmentId: string) =>
    request<{ externalTaskId: string; externalTaskUrl: string | null }>(
      `/weekly-plans/${planId}/assignments/${assignmentId}/send-to-external`,
      { method: 'POST' }
    ),

  async copyAssignments(targetPlanId: string, sourceAssignmentIds: string[]): Promise<void> {
    await request<TaskAssignmentDto[]>(`/weekly-plans/${targetPlanId}/copy-assignments`, {
      method: 'POST',
      body: JSON.stringify({ sourceAssignmentIds }),
    });
  },

  /** Resumen de capacidad por persona con advertencias. */
  getCapacitySummary: (weeklyPlanId: string) =>
    request<CapacitySummaryDto>(`/weekly-plans/${weeklyPlanId}/capacity`),

  /** Personas activas del equipo. */
  getPersons: () => request<PersonDto[]>('/persons'),

  /** Crea un nuevo plan semanal. */
  createPlan: (body: { weekStartDate: string; notes?: string }) =>
    request<WeeklyPlanDto>('/weekly-plans', {
      method: 'POST',
      body: JSON.stringify(body),
    }),

  /** Cambia el estado de un plan (confirm | close | revert-to-draft). */
  updateStatus: (id: string, action: 'confirm' | 'close' | 'revert-to-draft') =>
    request<WeeklyPlanDto>(`/weekly-plans/${id}/status`, {
      method: 'PATCH',
      body: JSON.stringify({ action }),
    }),

  /** Agrega una asignación al plan. */
  addAssignment: (planId: string, req: AddAssignmentRequest) =>
    request<TaskAssignmentDto>(`/weekly-plans/${planId}/assignments`, {
      method: 'POST',
      body: JSON.stringify(req),
    }),

  /** Actualiza una asignación existente (título, horas, notas). */
  updateAssignment: (
    planId: string,
    assignId: string,
    req: { taskTitle: string; plannedHours: number; notes?: string }
  ) =>
    request<TaskAssignmentDto>(`/weekly-plans/${planId}/assignments/${assignId}`, {
      method: 'PUT',
      body: JSON.stringify(req),
    }),

  /** Elimina una asignación del plan. */
  removeAssignment: (planId: string, assignId: string) =>
    request<void>(`/weekly-plans/${planId}/assignments/${assignId}`, {
      method: 'DELETE',
    }),
};

export const personsApi = {
  getAll: () => request<PersonDto[]>('/persons'),
  create: (body: { name: string; weeklyCapacityHours: number; email?: string }) =>
    request<PersonDto>('/persons', { method: 'POST', body: JSON.stringify(body) }),
  update: (id: string, body: { name: string; weeklyCapacityHours: number; email?: string }) =>
    request<PersonDto>(`/persons/${id}`, { method: 'PUT', body: JSON.stringify(body) }),
  setChobiUserId: (id: string, chobiUserId: number) =>
    request<PersonDto>(`/persons/${id}/chobi-user-id`, {
      method: 'PATCH',
      body: JSON.stringify({ chobiUserId }),
    }),
  deactivate: (id: string) =>
    request<void>(`/persons/${id}`, { method: 'DELETE' }),
};

export const projectsApi = {
  getAll: () => request<ProjectDto[]>('/projects'),
  create: (body: { name: string; description?: string; isBillable: boolean }) =>
    request<ProjectDto>('/projects', { method: 'POST', body: JSON.stringify(body) }),
  update: (id: string, body: { name: string; description?: string; isBillable: boolean }) =>
    request<ProjectDto>(`/projects/${id}`, { method: 'PUT', body: JSON.stringify(body) }),
  deactivate: (id: string) => request<void>(`/projects/${id}`, { method: 'DELETE' }),
};

export type ProjectDto = {
  id: string;
  name: string;
  description?: string;
  chobiProjectId?: number | null;
  isActive: boolean;
  isBillable: boolean;
};
