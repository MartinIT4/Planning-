// ─── Enums ────────────────────────────────────────────────────────────────────

export type PersonalPlanStatus = 'Draft' | 'Confirmed';

export type PersonalItemStatus = 'Planned' | 'SentToExternal';

export type PersonalItemCategory = 'Meeting' | 'Review' | 'Task' | 'Admin' | 'Other';

// Día de semana: 1=Lunes … 5=Viernes. null = sin día asignado
export type DayOfWeek = 1 | 2 | 3 | 4 | 5;

// ─── DTOs (mapean 1:1 con la API REST) ────────────────────────────────────────

export interface PersonalPlanItemDto {
  id: string;
  personalWeeklyPlanId: string;
  title: string;
  description: string | null;
  category: PersonalItemCategory;
  estimatedHours: number | null;
  plannedDayOfWeek: DayOfWeek | null;
  externalTaskId: string | null;
  chobiProjectId: number | null;
  externalTaskUrl: string | null;
  sortOrder: number;
  /** Estado local: Planned = solo planificado, SentToExternal = enviado al sistema. */
  status: PersonalItemStatus;
  isDone: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface PersonalWeeklyPlanDto {
  id: string;
  ownerId: string;
  weekStartDate: string; // 'yyyy-MM-dd'
  weekEndDate: string;
  status: PersonalPlanStatus;
  notes: string | null;
  items: PersonalPlanItemDto[];
  createdAt: string;
  updatedAt: string;
}

// ─── Request types ────────────────────────────────────────────────────────────

export interface CreatePersonalPlanRequest {
  ownerId: string;
  weekStartDate: string;
  notes?: string;
}

export interface CreatePersonalItemRequest {
  title: string;
  description?: string;
  category: PersonalItemCategory;
  estimatedHours?: number;
  plannedDayOfWeek?: DayOfWeek;
  externalTaskId?: string;
  chobiProjectId?: number;
}

export interface SendItemResultDto {
  itemId: string;
  title: string;
  success: boolean;
  externalTaskId?: string;
  externalTaskUrl?: string;
  errorMessage?: string;
}

export interface PublishResultDto {
  totalItems: number;
  succeeded: number;
  failed: number;
  results: SendItemResultDto[];
}

export type UpdatePersonalItemRequest = CreatePersonalItemRequest;

// ─── Constants ────────────────────────────────────────────────────────────────

export const DAY_LABELS: Record<DayOfWeek, string> = {
  1: 'Lunes',
  2: 'Martes',
  3: 'Miércoles',
  4: 'Jueves',
  5: 'Viernes',
};

export const CATEGORY_LABELS: Record<PersonalItemCategory, string> = {
  Meeting: '🤝 Reunión',
  Review:  '🔍 Revisión',
  Task:    '✅ Tarea',
  Admin:   '📋 Admin',
  Other:   '📌 Otro',
};

export const CATEGORY_COLORS: Record<PersonalItemCategory, string> = {
  Meeting: '#dbeafe', // blue-100
  Review:  '#ede9fe', // violet-100
  Task:    '#dcfce7', // green-100
  Admin:   '#fef3c7', // amber-100
  Other:   '#f1f5f9', // slate-100
};

export const ALL_DAYS: DayOfWeek[] = [1, 2, 3, 4, 5];
