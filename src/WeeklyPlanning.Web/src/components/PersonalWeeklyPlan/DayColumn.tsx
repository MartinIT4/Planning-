import type { PersonalPlanItemDto, DayOfWeek } from '../../types/personalPlan';
import { DAY_LABELS } from '../../types/personalPlan';
import { PersonalTaskCard } from './PersonalTaskCard';
import styles from './PersonalWeeklyPlan.module.css';

interface DayColumnProps {
  day: DayOfWeek | null; // null = sin día asignado
  date?: Date;
  items: PersonalPlanItemDto[];
  isMutating: boolean;
  onAddClick: () => void;
  onEdit: (item: PersonalPlanItemDto) => void;
  onDelete: (itemId: string) => void;
  onSendToExternal: (itemId: string) => void;
}

/**
 * Columna de un día de la semana en el plan personal del PM.
 * Muestra el nombre del día, fecha, horas totales y las tarjetas de tareas.
 */
export function DayColumn({
  day,
  date,
  items,
  isMutating,
  onAddClick,
  onEdit,
  onDelete,
  onSendToExternal,
}: DayColumnProps) {
  const totalHours = items.reduce((s, i) => s + (i.estimatedHours ?? 0), 0);
  const isToday =
    date != null &&
    date.toDateString() === new Date().toDateString();

  const label = day !== null ? DAY_LABELS[day] : 'Sin asignar';

  const formattedDate = date?.toLocaleDateString('es-AR', {
    day: 'numeric',
    month: 'short',
    timeZone: 'UTC',
  });

  return (
    <div className={`${styles.dayColumn} ${isToday ? styles.dayColumnToday : ''}`}>
      {/* Cabecera del día */}
      <div className={styles.dayHeader}>
        <div className={styles.dayLabel}>
          <span className={styles.dayName}>{label}</span>
          {formattedDate && <span className={styles.dayDate}>{formattedDate}</span>}
        </div>
        {totalHours > 0 && (
          <span className={styles.dayHours} title="Horas estimadas del día">
            {totalHours}h
          </span>
        )}
      </div>

      {/* Lista de tarjetas */}
      <div className={styles.dayItems}>
        {items.map((item) => (
          <PersonalTaskCard
            key={item.id}
            item={item}
            isMutating={isMutating}
            onEdit={() => onEdit(item)}
            onDelete={() => onDelete(item.id)}
            onSendToExternal={() => onSendToExternal(item.id)}
          />
        ))}
      </div>

      {/* Botón agregar */}
      <button
        className={styles.addTaskBtn}
        onClick={onAddClick}
        disabled={isMutating}
        title={`Agregar tarea para ${label}`}
      >
        + Agregar tarea
      </button>
    </div>
  );
}
