// Tipos que mapean 1:1 con los DTOs de la API REST

export type WeeklyPlanStatus = 'Draft' | 'Confirmed' | 'Closed';

export interface PersonDto {
  id: string;
  name: string;
  email: string;
  weeklyCapacityHours: number;
  chobiUserId?: number | null;
  isActive: boolean;
}

export interface TaskAssignmentDto {
  id: string;
  weeklyPlanId: string;
  personId: string;
  personName: string;
  externalTaskId: string;
  taskTitle: string;
  plannedHours: number;
  notes: string | null;
  sentToExternalAt: string | null;
  externalCreatedTaskId: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface WeeklyPlanDto {
  id: string;
  weekStartDate: string; // 'yyyy-MM-dd'
  weekEndDate: string;
  status: WeeklyPlanStatus;
  notes: string | null;
  assignments: TaskAssignmentDto[];
  createdAt: string;
  updatedAt: string;
}

export interface PersonCapacityDto {
  personId: string;
  personName: string;
  weeklyCapacityHours: number;
  plannedHours: number;
  remainingHours: number;
  isOverCapacity: boolean;
  warningMessage: string | null;
}

export interface CapacitySummaryDto {
  weeklyPlanId: string;
  weekStartDate: string;
  weekEndDate: string;
  personCapacities: PersonCapacityDto[];
}

// Nivel de carga para indicador visual
export type LoadLevel = 'normal' | 'warning' | 'overload';

export function getLoadLevel(plannedHours: number, capacityHours: number): LoadLevel {
  if (capacityHours === 0) return 'normal';
  const pct = plannedHours / capacityHours;
  if (pct > 1.0) return 'overload';
  if (pct >= 0.8) return 'warning';
  return 'normal';
}
