import { useState, useEffect, useCallback } from 'react';
import type { WeeklyPlanDto, PersonDto } from '../../types/weeklyPlan';
import { weeklyPlanApi, projectsApi, type ProjectDto } from '../../api/weeklyPlanApi';
import { PlanSelector } from './PlanSelector';
import { ProjectTaskBoard } from './ProjectTaskBoard';
import styles from './PlanningView.module.css';

export function PlanningView() {
  const [plans, setPlans] = useState<WeeklyPlanDto[]>([]);
  const [selectedPlan, setSelectedPlan] = useState<WeeklyPlanDto | null>(null);
  const [persons, setPersons] = useState<PersonDto[]>([]);
  const [projects, setProjects] = useState<ProjectDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const loadAll = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const [allPlans, allPersons, allProjects] = await Promise.all([
        weeklyPlanApi.getAll(),
        weeklyPlanApi.getPersons(),
        projectsApi.getAll(),
      ]);
      setPlans(allPlans);
      setPersons(allPersons);
      setProjects(allProjects);
      if (allPlans.length > 0) {
        const full = await weeklyPlanApi.getById(allPlans[0].id);
        setSelectedPlan(full);
      }
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Error al cargar datos');
    } finally {
      setLoading(false);
    }
  }, []); // eslint-disable-line react-hooks/exhaustive-deps

  useEffect(() => { loadAll(); }, [loadAll]);

  const handleSelectPlan = async (plan: WeeklyPlanDto) => {
    try { setSelectedPlan(await weeklyPlanApi.getById(plan.id)); }
    catch { setSelectedPlan(plan); }
  };

  const handlePlanCreated = async (plan: WeeklyPlanDto) => {
    setPlans((prev) => [plan, ...prev]);
    setSelectedPlan(await weeklyPlanApi.getById(plan.id));
  };

  const handlePlanUpdated = (plan: WeeklyPlanDto) => {
    setPlans((prev) => prev.map((p) => (p.id === plan.id ? plan : p)));
    setSelectedPlan(plan);
  };

  if (loading) {
    return (
      <div className={styles.stateContainer}>
        <div className={styles.spinner} aria-label="Cargando…" />
        <p>Cargando planificación…</p>
      </div>
    );
  }

  if (error) {
    return (
      <div className={styles.stateContainer}>
        <div className={styles.errorBox} role="alert">
          <strong>Error al cargar</strong><p>{error}</p>
          <button className={styles.btnPrimary} onClick={loadAll}>Reintentar</button>
        </div>
      </div>
    );
  }

  return (
    <div className={styles.view}>
      <PlanSelector
        plans={plans}
        selectedPlan={selectedPlan}
        onSelect={handleSelectPlan}
        onPlanCreated={handlePlanCreated}
        onPlanUpdated={handlePlanUpdated}
      />
      <div className={styles.boardWrapper}>
        <ProjectTaskBoard
          projects={projects}
          persons={persons}
          plan={selectedPlan}
          onPlanUpdated={handlePlanUpdated}
        />
      </div>
    </div>
  );
}

