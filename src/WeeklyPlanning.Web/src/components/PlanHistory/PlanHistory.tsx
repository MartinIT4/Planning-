import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { weeklyPlanApi } from '../../api/weeklyPlanApi';
import type { TaskAssignmentDto, WeeklyPlanDto, WeeklyPlanStatus } from '../../types/weeklyPlan';
import styles from './PlanHistory.module.css';

interface PlanHistoryProps {
  currentBoardWeek: string;
}

const STATUS_LABEL: Record<WeeklyPlanStatus, string> = {
  Draft: 'Borrador',
  Confirmed: 'Confirmado',
  Closed: 'Cerrado',
};

const STATUS_CLASS: Record<WeeklyPlanStatus, string> = {
  Draft: styles.statusDraft,
  Confirmed: styles.statusConfirmed,
  Closed: styles.statusClosed,
};

function formatWeekRange(weekStartDate: string): string {
  const start = new Date(weekStartDate + 'T00:00:00Z');
  const end = new Date(weekStartDate + 'T00:00:00Z');
  end.setUTCDate(end.getUTCDate() + 4);

  const formatShort = (date: Date, withYear = false) =>
    date
      .toLocaleDateString('es-AR', {
        day: 'numeric',
        month: 'short',
        ...(withYear ? { year: 'numeric' as const } : {}),
        timeZone: 'UTC',
      })
      .replace('.', '')
      .toLowerCase();

  return `${formatShort(start)} – ${formatShort(end, true)}`;
}

function formatTargetLabel(weekStartDate: string): string {
  return `Semana ${new Date(weekStartDate + 'T00:00:00Z').toLocaleDateString('es-AR', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    timeZone: 'UTC',
  })}`;
}

function getSentSummary(plan: WeeklyPlanDto): string {
  const sent = plan.assignments.filter((assignment) => assignment.sentToExternalAt != null).length;
  return `${sent}/${plan.assignments.length} enviadas a Chrobi`;
}

interface PersonGroupHeaderProps {
  personName: string;
  assignmentIds: string[];
  selectedAssignmentIds: Set<string>;
  onToggle: (ids: string[]) => void;
  disabled: boolean;
}

function PersonGroupHeader({ personName, assignmentIds, selectedAssignmentIds, onToggle, disabled }: PersonGroupHeaderProps) {
  const checkedCount = assignmentIds.filter((id) => selectedAssignmentIds.has(id)).length;
  const allSelected = checkedCount === assignmentIds.length;
  const someSelected = checkedCount > 0 && !allSelected;
  const ref = useRef<HTMLInputElement>(null);

  useEffect(() => {
    if (ref.current) ref.current.indeterminate = someSelected;
  }, [someSelected]);

  return (
    <label className={styles.personLabel}>
      <input
        ref={ref}
        type="checkbox"
        className={styles.checkbox}
        checked={allSelected}
        onChange={() => onToggle(assignmentIds)}
        disabled={disabled}
        style={{ marginRight: '0.4rem' }}
      />
      {personName}
    </label>
  );
}

export function PlanHistory({ currentBoardWeek }: PlanHistoryProps) {
  const [plans, setPlans] = useState<WeeklyPlanDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [copying, setCopying] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [expandedPlanIds, setExpandedPlanIds] = useState<Set<string>>(new Set());
  const [selectedAssignmentIds, setSelectedAssignmentIds] = useState<Set<string>>(new Set());
  const [targetPlanId, setTargetPlanId] = useState('');

  const loadPlans = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const allPlans = await weeklyPlanApi.getAll();
      setPlans([...allPlans].sort((a, b) => b.weekStartDate.localeCompare(a.weekStartDate)));
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Error al cargar historial');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadPlans();
  }, [loadPlans]);

  useEffect(() => {
    if (!message) return;
    const timer = window.setTimeout(() => setMessage(null), 5000);
    return () => window.clearTimeout(timer);
  }, [message]);

  const assignmentIndex = useMemo(() => {
    const index = new Map<string, TaskAssignmentDto>();
    for (const plan of plans) {
      for (const assignment of plan.assignments) {
        index.set(assignment.id, assignment);
      }
    }
    return index;
  }, [plans]);

  const selectedSourcePlanIds = useMemo(() => {
    const ids = new Set<string>();
    for (const assignmentId of selectedAssignmentIds) {
      const assignment = assignmentIndex.get(assignmentId);
      if (assignment) ids.add(assignment.weeklyPlanId);
    }
    return ids;
  }, [assignmentIndex, selectedAssignmentIds]);

  const availableTargetPlans = useMemo(
    () => plans.filter((plan) => !selectedSourcePlanIds.has(plan.id)),
    [plans, selectedSourcePlanIds],
  );

  useEffect(() => {
    setTargetPlanId((current) => {
      if (current && availableTargetPlans.some((plan) => plan.id === current)) return current;
      const currentWeekPlan = availableTargetPlans.find((plan) => plan.weekStartDate === currentBoardWeek);
      return currentWeekPlan?.id ?? availableTargetPlans[0]?.id ?? '';
    });
  }, [availableTargetPlans, currentBoardWeek]);

  const toggleExpanded = (planId: string) => {
    setExpandedPlanIds((prev) => {
      const next = new Set(prev);
      if (next.has(planId)) next.delete(planId);
      else next.add(planId);
      return next;
    });
  };

  const toggleAssignment = (assignmentId: string) => {
    setSelectedAssignmentIds((prev) => {
      const next = new Set(prev);
      if (next.has(assignmentId)) next.delete(assignmentId);
      else next.add(assignmentId);
      return next;
    });
  };

  const togglePersonAssignments = (assignmentIds: string[]) => {
    setSelectedAssignmentIds((prev) => {
      const next = new Set(prev);
      const allSelected = assignmentIds.every((id) => next.has(id));
      if (allSelected) assignmentIds.forEach((id) => next.delete(id));
      else assignmentIds.forEach((id) => next.add(id));
      return next;
    });
  };

  const handleCopy = async () => {
    if (copying || selectedAssignmentIds.size === 0 || !targetPlanId) return;
    setCopying(true);
    setError(null);
    setMessage(null);

    try {
      await weeklyPlanApi.copyAssignments(targetPlanId, [...selectedAssignmentIds]);
      const copiedCount = selectedAssignmentIds.size;
      setSelectedAssignmentIds(new Set());
      await loadPlans();
      setMessage(`Se copiaron ${copiedCount} asignación${copiedCount === 1 ? '' : 'es'}.`);
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Error al copiar asignaciones');
    } finally {
      setCopying(false);
    }
  };

  return (
    <section className={styles.container} aria-label="Historial de planes">
      <div className={styles.actionBar}>
        <strong>
          {selectedAssignmentIds.size} asignación{selectedAssignmentIds.size === 1 ? '' : 'es'} seleccionada{selectedAssignmentIds.size === 1 ? '' : 's'}
        </strong>

        <label>
          Copiar al plan:
          <select
            value={targetPlanId}
            onChange={(event) => setTargetPlanId(event.target.value)}
            disabled={loading || copying || availableTargetPlans.length === 0}
            style={{ marginLeft: '0.5rem' }}
          >
            {availableTargetPlans.length === 0 ? (
              <option value="">Sin destino disponible</option>
            ) : (
              availableTargetPlans.map((plan) => (
                <option key={plan.id} value={plan.id}>
                  {formatTargetLabel(plan.weekStartDate)}
                </option>
              ))
            )}
          </select>
        </label>

        <button
          type="button"
          className={styles.copyBtn}
          onClick={() => void handleCopy()}
          disabled={copying || selectedAssignmentIds.size === 0 || !targetPlanId}
        >
          {copying ? 'Copiando…' : '📥 Copiar'}
        </button>
      </div>

      {message && <div className={styles.sentCount}>{message}</div>}
      {error && <div className={styles.noPlans} role="alert">{error}</div>}

      {loading ? (
        <p className={styles.noPlans}>Cargando historial…</p>
      ) : plans.length === 0 ? (
        <p className={styles.noPlans}>No hay planes semanales todavía.</p>
      ) : (
        plans.map((plan) => {
          const expanded = expandedPlanIds.has(plan.id);
          const groups = plan.assignments.reduce<Map<string, TaskAssignmentDto[]>>((map, assignment) => {
            const key = assignment.personName || 'Sin persona';
            const list = map.get(key) ?? [];
            list.push(assignment);
            map.set(key, list);
            return map;
          }, new Map());

          return (
            <article key={plan.id} className={styles.planCard}>
              <header className={styles.planHeader}>
                <div>
                  <h2 style={{ margin: 0, fontSize: '1rem', color: '#0f172a' }}>{formatWeekRange(plan.weekStartDate)}</h2>
                  <div style={{ display: 'flex', gap: '0.75rem', alignItems: 'center', flexWrap: 'wrap', marginTop: '0.45rem' }}>
                    <span className={`${styles.statusBadge} ${STATUS_CLASS[plan.status]}`}>
                      {STATUS_LABEL[plan.status]}
                    </span>
                    <span className={styles.sentCount}>{getSentSummary(plan)}</span>
                  </div>
                </div>

                <button
                  type="button"
                  className={styles.expandBtn}
                  onClick={() => toggleExpanded(plan.id)}
                  disabled={plan.assignments.length === 0}
                >
                  {expanded ? '▼' : '▶'} {expanded ? 'Ocultar' : 'Ver tareas'}
                </button>
              </header>

              {expanded && (
                <div className={styles.assignmentList}>
                  {plan.assignments.length === 0 ? (
                    <p className={styles.noPlans}>Sin asignaciones en esta semana.</p>
                  ) : (
                    Array.from(groups.entries()).map(([personName, assignments]) => (
                      <div key={personName} className={styles.personGroup}>
                      <PersonGroupHeader
                        personName={personName}
                        assignmentIds={assignments.map((a) => a.id)}
                        selectedAssignmentIds={selectedAssignmentIds}
                        onToggle={togglePersonAssignments}
                        disabled={copying}
                      />
                        {assignments.map((assignment) => (
                          <label key={assignment.id} className={styles.assignmentRow}>
                            <input
                              type="checkbox"
                              className={styles.checkbox}
                              checked={selectedAssignmentIds.has(assignment.id)}
                              onChange={() => toggleAssignment(assignment.id)}
                              disabled={copying}
                            />
                            <span>
                              {assignment.taskTitle} · {assignment.plannedHours}h
                              {assignment.sentToExternalAt ? ' · ✓ enviada' : ''}
                            </span>
                          </label>
                        ))}
                      </div>
                    ))
                  )}
                </div>
              )}
            </article>
          );
        })
      )}
    </section>
  );
}
