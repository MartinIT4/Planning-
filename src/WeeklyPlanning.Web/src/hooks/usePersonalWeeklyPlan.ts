import { useState, useEffect, useCallback } from 'react';
import { personalPlanApi } from '../api/personalPlanApi';
import type {
  PersonalWeeklyPlanDto,
  CreatePersonalItemRequest,
  UpdatePersonalItemRequest,
  PublishResultDto,
} from '../types/personalPlan';

interface UsePersonalWeeklyPlanResult {
  plan: PersonalWeeklyPlanDto | null;
  isLoading: boolean;
  isMutating: boolean;
  error: string | null;
  reload: () => void;
  addItem: (req: CreatePersonalItemRequest) => Promise<void>;
  updateItem: (itemId: string, req: UpdatePersonalItemRequest) => Promise<void>;
  deleteItem: (itemId: string) => Promise<void>;
  sendToExternal: (itemId: string) => Promise<void>;
  publishToExternal: () => Promise<PublishResultDto>;
}

function isPersonalWeeklyPlanDto(value: unknown): value is PersonalWeeklyPlanDto {
  return !!value && typeof value === 'object' && 'ownerId' in value && 'items' in value;
}

export function usePersonalWeeklyPlan(
  ownerId: string | null,
  weekStartDate: string | null
): UsePersonalWeeklyPlanResult {
  const [plan, setPlan] = useState<PersonalWeeklyPlanDto | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [isMutating, setIsMutating] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [tick, setTick] = useState(0);

  const reload = useCallback(() => setTick((t) => t + 1), []);

  useEffect(() => {
    if (!ownerId || !weekStartDate) return;

    let cancelled = false;
    setIsLoading(true);
    setError(null);

    personalPlanApi
      .getByOwnerAndWeek(ownerId, weekStartDate)
      .then(async (data) => {
        if (cancelled) return;
        if (data) {
          setPlan(data);
        } else {
          const newPlan = await personalPlanApi.create({ ownerId, weekStartDate });
          if (!cancelled) setPlan(newPlan);
        }
      })
      .catch((err: unknown) => {
        if (!cancelled) setError(err instanceof Error ? err.message : 'Error desconocido');
      })
      .finally(() => { if (!cancelled) setIsLoading(false); });

    return () => { cancelled = true; };
  }, [ownerId, weekStartDate, tick]);

  const withMutation = useCallback(async <T extends PersonalWeeklyPlanDto | PublishResultDto | void>(
    fn: () => Promise<T>
  ): Promise<T> => {
    setIsMutating(true);
    setError(null);
    try {
      const result = await fn();
      if (isPersonalWeeklyPlanDto(result)) setPlan(result);
      return result;
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Error al guardar');
      throw err;
    } finally {
      setIsMutating(false);
    }
  }, []);

  const addItem = useCallback(
    async (req: CreatePersonalItemRequest) => {
      await withMutation(() => personalPlanApi.addItem(plan!.id, req));
    },
    [plan, withMutation]
  );

  const updateItem = useCallback(
    async (itemId: string, req: UpdatePersonalItemRequest) => {
      await withMutation(() => personalPlanApi.updateItem(plan!.id, itemId, req));
    },
    [plan, withMutation]
  );

  const deleteItem = useCallback(
    async (itemId: string) => {
      await withMutation(async () => {
        await personalPlanApi.deleteItem(plan!.id, itemId);
        setPlan((prev) =>
          prev ? { ...prev, items: prev.items.filter((i) => i.id !== itemId) } : prev
        );
      });
    },
    [plan, withMutation]
  );

  const sendToExternal = useCallback(
    async (itemId: string) => {
      await withMutation(async () => {
        await personalPlanApi.sendItemToExternal(plan!.id, itemId);
        reload();
      });
    },
    [plan, withMutation, reload]
  );

  const publishToExternal = useCallback(
    () => withMutation(async () => {
      const result = await personalPlanApi.publishToExternal(plan!.id);
      reload();
      return result;
    }),
    [plan, withMutation, reload]
  );

  return {
    plan,
    isLoading,
    isMutating,
    error,
    reload,
    addItem,
    updateItem,
    deleteItem,
    sendToExternal,
    publishToExternal,
  };
}
