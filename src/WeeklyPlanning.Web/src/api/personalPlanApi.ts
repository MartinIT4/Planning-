import type {
  PersonalWeeklyPlanDto,
  CreatePersonalPlanRequest,
  CreatePersonalItemRequest,
  UpdatePersonalItemRequest,
  PublishResultDto,
} from '../types/personalPlan';

const BASE_URL = '/api';

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(`${BASE_URL}${path}`, {
    headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
    ...init,
  });
  if (!res.ok) {
    const body = await res.text().catch(() => '');
    throw new Error(`${init?.method ?? 'GET'} ${path} → ${res.status}: ${body.slice(0, 200)}`);
  }
  // 204 No Content
  if (res.status === 204) return undefined as T;
  return res.json() as Promise<T>;
}

export const personalPlanApi = {
  /** Obtiene el plan personal de un owner para una semana específica. */
  getByOwnerAndWeek: (ownerId: string, weekStartDate: string) =>
    request<PersonalWeeklyPlanDto | null>(
      `/personal-plans?ownerId=${ownerId}&weekStartDate=${weekStartDate}`
    ),

  /** Crea un nuevo plan personal. */
  create: (body: CreatePersonalPlanRequest) =>
    request<PersonalWeeklyPlanDto>('/personal-plans', {
      method: 'POST',
      body: JSON.stringify(body),
    }),

  /** Agrega un ítem al plan. */
  addItem: (planId: string, body: CreatePersonalItemRequest) =>
    request<PersonalWeeklyPlanDto>(`/personal-plans/${planId}/items`, {
      method: 'POST',
      body: JSON.stringify(body),
    }),

  /** Actualiza un ítem existente. */
  updateItem: (planId: string, itemId: string, body: UpdatePersonalItemRequest) =>
    request<PersonalWeeklyPlanDto>(`/personal-plans/${planId}/items/${itemId}`, {
      method: 'PUT',
      body: JSON.stringify(body),
    }),

  /** Elimina un ítem del plan. */
  deleteItem: (planId: string, itemId: string) =>
    request<void>(`/personal-plans/${planId}/items/${itemId}`, { method: 'DELETE' }),

  /** Envía un ítem al sistema externo de gestión de proyectos. */
  sendItemToExternal: (planId: string, itemId: string) =>
    request<void>(`/personal-plans/${planId}/items/${itemId}/send-to-external`, {
      method: 'POST',
    }),

  publishToExternal: (planId: string) =>
    request<PublishResultDto>(`/personal-plans/${planId}/publish-to-external`, {
      method: 'POST',
    }),
};
