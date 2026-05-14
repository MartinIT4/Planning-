export interface BacklogItemDto {
  id: string;
  externalTaskId: string;
  title: string;
  description?: string;
  projectId?: string;
  projectName?: string;
  estimatedHours: number;
  remainingHours: number;
  status: string;
  assigneeName?: string;
  lastSyncedAt: string;
}
