import { useState } from 'react';
import type { TaskAssignmentDto, PersonDto, WeeklyPlanDto } from '../../types/weeklyPlan';
import { weeklyPlanApi } from '../../api/weeklyPlanApi';
import styles from './AssignTaskModal.module.css';

interface EditAssignmentModalProps {
  assignment: TaskAssignmentDto;
  persons: PersonDto[];
  plan: WeeklyPlanDto;
  onSaved: () => void;
  onClose: () => void;
}

export function EditAssignmentModal({
  assignment,
  persons,
  plan,
  onSaved,
  onClose,
}: EditAssignmentModalProps) {
  const [plannedHours, setPlannedHours] = useState(assignment.plannedHours);
  const [notes, setNotes] = useState(assignment.notes ?? '');
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const person = persons.find((p) => p.id === assignment.personId);
  const otherPlanned = plan.assignments
    .filter((a) => a.personId === assignment.personId && a.id !== assignment.id)
    .reduce((s, a) => s + a.plannedHours, 0);
  const capacity = person?.weeklyCapacityHours ?? 40;
  const wouldExceed = otherPlanned + plannedHours > capacity;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    setError(null);
    try {
      await weeklyPlanApi.updateAssignment(plan.id, assignment.id, {
        taskTitle: assignment.taskTitle,
        plannedHours,
        notes: notes || undefined,
      });
      onSaved();
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Error al guardar cambios');
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className={styles.backdrop} onClick={onClose}>
      <div className={styles.modal} onClick={(e) => e.stopPropagation()} role="dialog" aria-modal>
        <div className={styles.modalHeader}>
          <h3 className={styles.modalTitle}>Editar asignación</h3>
          <button className={styles.closeBtn} onClick={onClose} aria-label="Cerrar">✕</button>
        </div>

        <div className={styles.taskInfo}>
          <p className={styles.taskTitle}>{assignment.taskTitle}</p>
          <div className={styles.taskMeta}>
            {person && <span>{person.name}</span>}
            <span>#{assignment.externalTaskId}</span>
          </div>
        </div>

        <form className={styles.form} onSubmit={handleSubmit}>
          <label className={styles.formLabel}>
            Horas planificadas
            <input
              type="number"
              className={styles.formInput}
              value={plannedHours}
              onChange={(e) => setPlannedHours(Number(e.target.value))}
              min={0.5}
              step={0.5}
              required
              autoFocus
            />
          </label>

          {person && (
            <div className={styles.capacityBar}>
              <div className={styles.capacityLabels}>
                <span>{person.name}</span>
                <span>{otherPlanned + plannedHours}h de {capacity}h usadas</span>
              </div>
              <div className={styles.barTrack}>
                <div
                  className={styles.barUsed}
                  style={{ width: `${Math.min((otherPlanned / capacity) * 100, 100)}%` }}
                />
                <div
                  className={wouldExceed ? styles.barNewOver : styles.barNew}
                  style={{
                    width: `${Math.min((plannedHours / capacity) * 100, 100 - Math.min((otherPlanned / capacity) * 100, 100))}%`,
                  }}
                />
              </div>
              <span className={styles.capacityAvail}>
                {wouldExceed
                  ? `⚠️ Excede capacidad por ${(otherPlanned + plannedHours - capacity).toFixed(1)}h (solo advertencia)`
                  : `✅ Quedarán ${(capacity - otherPlanned - plannedHours).toFixed(1)}h disponibles`}
              </span>
            </div>
          )}

          <label className={styles.formLabel}>
            Notas (opcional)
            <input
              type="text"
              className={styles.formInput}
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              placeholder="Contexto adicional…"
            />
          </label>

          {error && <p className={styles.formError}>{error}</p>}

          <div className={styles.formActions}>
            <button type="button" className={styles.btnSecondary} onClick={onClose} disabled={saving}>
              Cancelar
            </button>
            <button type="submit" className={styles.btnPrimary} disabled={saving}>
              {saving ? 'Guardando…' : 'Guardar cambios'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
