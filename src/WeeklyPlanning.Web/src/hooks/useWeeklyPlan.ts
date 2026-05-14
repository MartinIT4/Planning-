import { useState, useEffect, useCallback } from 'react';
import { weeklyPlanApi } from '../api/weeklyPlanApi';
import type { WeeklyPlanDto, CapacitySummaryDto, PersonDto } from '../types/weeklyPlan';

interface UseWeeklyPlanResult {
  plan: WeeklyPlanDto | null;
  capacity: CapacitySummaryDto | null;
  persons: PersonDto[];
  isLoading: boolean;
  error: string | null;
  reload: () => void;
}

/**
 * Hook que carga un plan semanal, su resumen de capacidad y
 * la lista de personas activas del equipo.
 *
 * @param weeklyPlanId - ID del plan a cargar. Si es null, no hace fetch.
 */
export function useWeeklyPlan(weeklyPlanId: string | null): UseWeeklyPlanResult {
  const [plan, setPlan] = useState<WeeklyPlanDto | null>(null);
  const [capacity, setCapacity] = useState<CapacitySummaryDto | null>(null);
  const [persons, setPersons] = useState<PersonDto[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [tick, setTick] = useState(0);

  const reload = useCallback(() => setTick((t) => t + 1), []);

  useEffect(() => {
    if (!weeklyPlanId) return;

    let cancelled = false;
    setIsLoading(true);
    setError(null);

    Promise.all([
      weeklyPlanApi.getById(weeklyPlanId),
      weeklyPlanApi.getCapacitySummary(weeklyPlanId),
      weeklyPlanApi.getPersons(),
    ])
      .then(([planData, capacityData, personsData]) => {
        if (cancelled) return;
        setPlan(planData);
        setCapacity(capacityData);
        setPersons(personsData);
      })
      .catch((err: unknown) => {
        if (cancelled) return;
        setError(err instanceof Error ? err.message : 'Error desconocido');
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false);
      });

    return () => { cancelled = true; };
  }, [weeklyPlanId, tick]);

  return { plan, capacity, persons, isLoading, error, reload };
}
