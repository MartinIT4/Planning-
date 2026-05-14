import type { LoadLevel } from '../../types/weeklyPlan';
import styles from './WeeklyPlanningBoard.module.css';

interface CapacityIndicatorProps {
  plannedHours: number;
  capacityHours: number;
  level: LoadLevel;
}

/**
 * Barra visual de carga semanal de una persona.
 * Verde (≤80%) → Amarillo (80–100%) → Rojo (>100%)
 */
export function CapacityIndicator({ plannedHours, capacityHours, level }: CapacityIndicatorProps) {
  const pct = capacityHours > 0 ? (plannedHours / capacityHours) * 100 : 0;
  // La barra se corta en 100% visualmente aunque haya sobrecarga
  const barWidth = Math.min(pct, 100);

  const label: Record<LoadLevel, string> = {
    normal: 'Normal',
    warning: 'Atención',
    overload: 'Sobrecarga',
  };

  return (
    <div className={styles.capacityIndicator}>
      <div className={styles.capacityHeader}>
        <span className={styles.capacityHours}>
          {plannedHours}h / {capacityHours}h
        </span>
        <span className={`${styles.capacityBadge} ${styles[`badge_${level}`]}`}>
          {label[level]}
        </span>
      </div>

      {/* Barra de progreso */}
      <div className={styles.capacityBarBg}>
        <div
          className={`${styles.capacityBarFill} ${styles[`bar_${level}`]}`}
          style={{ width: `${barWidth}%` }}
          role="progressbar"
          aria-valuenow={plannedHours}
          aria-valuemin={0}
          aria-valuemax={capacityHours}
          aria-label={`${plannedHours} de ${capacityHours} horas planificadas`}
        />
      </div>
    </div>
  );
}
