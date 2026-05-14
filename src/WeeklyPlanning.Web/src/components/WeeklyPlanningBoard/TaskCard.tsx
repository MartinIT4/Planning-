import type { TaskAssignmentDto } from '../../types/weeklyPlan';
import styles from './WeeklyPlanningBoard.module.css';

interface TaskCardProps {
  assignment: TaskAssignmentDto;
}

/**
 * Tarjeta de tarea asignada dentro de una columna de persona.
 * Muestra título, horas planificadas (tentativas), tarea externa y notas.
 */
export function TaskCard({ assignment }: TaskCardProps) {
  return (
    <div className={styles.taskCard} title={assignment.notes ?? undefined}>
      {/* Cabecera: título y horas */}
      <div className={styles.taskCardHeader}>
        <span className={styles.taskTitle}>{assignment.taskTitle}</span>
        <span className={styles.taskHours} title="Horas planificadas tentativas">
          {assignment.plannedHours}h
        </span>
      </div>

      {/* ID externo — referencia al sistema de gestión de proyectos */}
      <span className={styles.taskExternalId}>#{assignment.externalTaskId}</span>

      {/* Notas opcionales */}
      {assignment.notes && (
        <p className={styles.taskNotes}>{assignment.notes}</p>
      )}

      {/* Disclaimer: recordatorio visual que no son horas imputadas */}
      <span className={styles.taskDisclaimer}>tentativo · sin imputación</span>
    </div>
  );
}
