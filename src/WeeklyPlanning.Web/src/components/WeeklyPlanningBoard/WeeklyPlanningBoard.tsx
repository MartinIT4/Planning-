import { useMemo } from 'react';
import { useWeeklyPlan } from '../../hooks/useWeeklyPlan';
import { PersonColumn } from './PersonColumn';
import styles from './WeeklyPlanningBoard.module.css';
import type { WeeklyPlanDto, CapacitySummaryDto } from '../../types/weeklyPlan';

interface WeeklyPlanningBoardProps {
  /** ID del plan semanal a mostrar. */
  weeklyPlanId: string;
}

const STATUS_LABEL: Record<string, string> = {
  Draft: 'Borrador',
  Confirmed: 'Confirmado',
  Closed: 'Cerrado',
};

/**
 * Tablero de planificación semanal del equipo.
 *
 * Muestra una columna por persona con:
 *   - Indicador visual de carga (normal / atención / sobrecarga)
 *   - Tarjetas de tareas asignadas de forma tentativa
 *
 * Los datos son de solo lectura: ninguna acción en este componente
 * modifica el sistema externo de gestión de proyectos.
 */
export function WeeklyPlanningBoard({ weeklyPlanId }: WeeklyPlanningBoardProps) {
  const { plan, capacity, persons, isLoading, error, reload } = useWeeklyPlan(weeklyPlanId);

  const assignmentsByPerson = useMemo(() => {
    if (!plan) return new Map<string, WeeklyPlanDto['assignments']>();
    return plan.assignments.reduce((map, a) => {
      const list = map.get(a.personId) ?? [];
      list.push(a);
      map.set(a.personId, list);
      return map;
    }, new Map<string, WeeklyPlanDto['assignments']>());
  }, [plan]);

  // Índice de capacidad por persona para acceso O(1)
  const capacityByPerson = useMemo(() => {
    if (!capacity) return new Map<string, CapacitySummaryDto['personCapacities'][number]>();
    return new Map(capacity.personCapacities.map((c) => [c.personId, c]));
  }, [capacity]);

  // ── Estados de carga / error ──────────────────────────────────────────────
  if (isLoading) {
    return (
      <div className={styles.stateContainer}>
        <div className={styles.spinner} aria-label="Cargando..." />
        <p>Cargando plan semanal…</p>
      </div>
    );
  }

  if (error) {
    return (
      <div className={styles.stateContainer}>
        <div className={styles.errorBox} role="alert">
          <strong>Error al cargar el plan</strong>
          <p>{error}</p>
          <button className={styles.reloadBtn} onClick={reload}>
            Reintentar
          </button>
        </div>
      </div>
    );
  }

  if (!plan) return null;

  const formattedStart = new Date(plan.weekStartDate).toLocaleDateString('es-AR', {
    day: 'numeric', month: 'long', year: 'numeric', timeZone: 'UTC',
  });
  const formattedEnd = new Date(plan.weekEndDate).toLocaleDateString('es-AR', {
    day: 'numeric', month: 'long', timeZone: 'UTC',
  });

  const totalPlanned = plan.assignments.reduce((s, a) => s + a.plannedHours, 0);
  const overCount = capacity?.personCapacities.filter((c) => c.isOverCapacity).length ?? 0;

  return (
    <section className={styles.board} aria-label="Tablero de planificación semanal">

      {/* ── Encabezado del tablero ── */}
      <header className={styles.boardHeader}>
        <div className={styles.boardTitle}>
          <h1 className={styles.weekTitle}>
            Semana del {formattedStart} al {formattedEnd}
          </h1>
          <span className={`${styles.statusBadge} ${styles[`status_${plan.status}`]}`}>
            {STATUS_LABEL[plan.status] ?? plan.status}
          </span>
        </div>

        {/* Resumen global */}
        <div className={styles.boardSummary}>
          <span>{persons.length} personas</span>
          <span>·</span>
          <span>{plan.assignments.length} asignaciones</span>
          <span>·</span>
          <span>{totalPlanned}h planificadas</span>
          {overCount > 0 && (
            <>
              <span>·</span>
              <span className={styles.overloadBadge}>
                ⚠️ {overCount} {overCount === 1 ? 'persona sobrecargada' : 'personas sobrecargadas'}
              </span>
            </>
          )}
        </div>

        {plan.notes && <p className={styles.boardNotes}>{plan.notes}</p>}

        <button className={styles.reloadBtn} onClick={reload} title="Actualizar datos">
          ↻ Actualizar
        </button>
      </header>

      {/* ── Columnas por persona ── */}
      <div className={styles.columnsContainer}>
        {persons.filter((p) => p.isActive).map((person) => (
          <PersonColumn
            key={person.id}
            personId={person.id}
            personName={person.name}
            assignments={assignmentsByPerson.get(person.id) ?? []}
            capacity={capacityByPerson.get(person.id)}
          />
        ))}

        {persons.filter((p) => p.isActive).length === 0 && (
          <div className={styles.emptyBoard}>
            No hay personas activas en el equipo.
          </div>
        )}
      </div>

      {/* Recordatorio de que las asignaciones son tentativas */}
      <footer className={styles.boardFooter}>
        Las asignaciones son <strong>tentativas</strong>. Este sistema no imputa horas reales
        ni modifica el sistema de gestión de proyectos.
      </footer>
    </section>
  );
}
