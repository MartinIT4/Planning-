import type { TaskAssignmentDto, PersonCapacityDto } from '../../types/weeklyPlan';
import { getLoadLevel } from '../../types/weeklyPlan';
import { CapacityIndicator } from './CapacityIndicator';
import { TaskCard } from './TaskCard';
import styles from './WeeklyPlanningBoard.module.css';

interface PersonColumnProps {
  personId: string;
  personName: string;
  assignments: TaskAssignmentDto[];
  capacity: PersonCapacityDto | undefined;
}

/**
 * Columna del tablero correspondiente a una persona del equipo.
 * Muestra su indicador de carga y las tarjetas de tareas asignadas.
 */
export function PersonColumn({ personId: _personId, personName, assignments, capacity }: PersonColumnProps) {
  const plannedHours = capacity?.plannedHours ?? assignments.reduce((s, a) => s + a.plannedHours, 0);
  const capacityHours = capacity?.weeklyCapacityHours ?? 40;
  const level = getLoadLevel(plannedHours, capacityHours);

  return (
    <div className={`${styles.personColumn} ${styles[`col_${level}`]}`}>
      {/* Cabecera de columna */}
      <div className={styles.personHeader}>
        <div className={styles.personAvatar} aria-hidden="true">
          {personName.charAt(0).toUpperCase()}
        </div>
        <span className={styles.personName}>{personName}</span>
      </div>

      {/* Indicador de carga */}
      <CapacityIndicator
        plannedHours={plannedHours}
        capacityHours={capacityHours}
        level={level}
      />

      {/* Mensaje de advertencia si existe */}
      {capacity?.warningMessage && (
        <div className={styles.warningBanner} role="alert">
          ⚠️ {capacity.warningMessage}
        </div>
      )}

      {/* Tarjetas de tareas */}
      <div className={styles.taskList}>
        {assignments.length === 0 ? (
          <div className={styles.emptyColumn}>Sin tareas asignadas</div>
        ) : (
          assignments.map((a) => <TaskCard key={a.id} assignment={a} />)
        )}
      </div>
    </div>
  );
}
