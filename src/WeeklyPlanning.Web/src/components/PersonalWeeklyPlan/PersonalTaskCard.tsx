import type { PersonalPlanItemDto } from '../../types/personalPlan';
import { CATEGORY_LABELS, CATEGORY_COLORS } from '../../types/personalPlan';
import styles from './PersonalWeeklyPlan.module.css';

interface PersonalTaskCardProps {
  item: PersonalPlanItemDto;
  isMutating: boolean;
  onEdit: () => void;
  onDelete: () => void;
  onSendToExternal: () => void;
}

export function PersonalTaskCard({
  item,
  isMutating,
  onEdit,
  onDelete,
  onSendToExternal,
}: PersonalTaskCardProps) {
  const isSent = item.status === 'SentToExternal';
  const bgColor = CATEGORY_COLORS[item.category];

  return (
    <div
      className={`${styles.personalCard} ${isSent ? styles.personalCardSent : ''}`}
      style={{ borderLeftColor: isSent ? '#22c55e' : '#94a3b8' }}
    >
      <div className={styles.personalCardHeader}>
        <span className={styles.categoryPill} style={{ background: bgColor }}>
          {CATEGORY_LABELS[item.category]}
        </span>
        {item.estimatedHours != null && (
          <span className={styles.hoursChip} title="Horas estimadas">
            {item.estimatedHours}h
          </span>
        )}
      </div>

      <p className={`${styles.personalCardTitle} ${item.isDone ? styles.done : ''}`}>
        {item.title}
      </p>

      {item.description && (
        <p className={styles.personalCardDesc}>{item.description}</p>
      )}

      {item.externalTaskId && (
        <span className={styles.externalRef}>🔗 #{item.externalTaskId}</span>
      )}

      <div className={styles.personalCardFooter}>
        {isSent ? (
          item.externalTaskUrl ? (
            <a
              className={styles.externalLink}
              href={item.externalTaskUrl}
              target="_blank"
              rel="noreferrer"
            >
              ↗ Ver en Chrobi
            </a>
          ) : (
            <span className={styles.sentBadge}>✓ En Chrobi</span>
          )
        ) : (
          <span className={styles.plannedBadge}>Planificado</span>
        )}

        <div className={styles.cardActions}>
          {!isSent && (
            <button
              className={styles.btnSend}
              onClick={onSendToExternal}
              disabled={isMutating}
              title="Enviar al sistema externo de gestión de proyectos"
            >
              ↗ Enviar
            </button>
          )}
          <button
            className={styles.btnIcon}
            onClick={onEdit}
            disabled={isMutating}
            title="Editar tarea"
            aria-label="Editar"
          >
            ✏️
          </button>
          <button
            className={styles.btnIcon}
            onClick={onDelete}
            disabled={isMutating}
            title="Eliminar tarea"
            aria-label="Eliminar"
          >
            🗑️
          </button>
        </div>
      </div>
    </div>
  );
}
