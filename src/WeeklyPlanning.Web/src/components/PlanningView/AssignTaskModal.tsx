import { useState } from 'react';
import type { BacklogItemDto } from '../../types/backlog';
import type { PersonDto, WeeklyPlanDto } from '../../types/weeklyPlan';
import { weeklyPlanApi } from '../../api/weeklyPlanApi';
import styles from './AssignTaskModal.module.css';

interface AssignTaskModalProps {
  task: BacklogItemDto;
  persons: PersonDto[];
  plan: WeeklyPlanDto;
  onAssigned: () => void;
  onClose: () => void;
}

export function AssignTaskModal({
  task,
  persons,
  plan,
  onAssigned,
  onClose,
}: AssignTaskModalProps) {
  const activePersns = persons.filter((p) => p.isActive);
  const [personId, setPersonId] = useState(activePersns[0]?.id ?? '');
  const [plannedHours, setPlannedHours] = useState(
    task.remainingHours > 0 ? task.remainingHours : task.estimatedHours
  );
  const [notes, setNotes] = useState('');
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Calculate already planned hours per person in this plan
  const plannedByPerson = (pid: string) =>
    plan.assignments.filter((a) => a.personId === pid).reduce((s, a) => s + a.plannedHours, 0);

  const selectedPerson = activePersns.find((p) => p.id === personId);
  const alreadyPlanned = selectedPerson ? plannedByPerson(selectedPerson.id) : 0;
  const available = selectedPerson ? selectedPerson.weeklyCapacityHours - alreadyPlanned : 0;
  const wouldExceed = selectedPerson ? alreadyPlanned + plannedHours > selectedPerson.weeklyCapacityHours : false;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!personId) { setError('Selecciona una persona'); return; }
    setSaving(true);
    setError(null);
    try {
      await weeklyPlanApi.addAssignment(plan.id, {
        personId,
        externalTaskId: task.externalTaskId,
        taskTitle: task.title,
        plannedHours,
        notes: notes || undefined,
      });
      onAssigned();
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Error al asignar tarea');
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className={styles.backdrop} onClick={onClose}>
      <div className={styles.modal} onClick={(e) => e.stopPropagation()} role="dialog" aria-modal>
        <div className={styles.modalHeader}>
          <h3 className={styles.modalTitle}>Asignar tarea</h3>
          <button className={styles.closeBtn} onClick={onClose} aria-label="Cerrar">✕</button>
        </div>

        <div className={styles.taskInfo}>
          <p className={styles.taskTitle}>{task.title}</p>
          <div className={styles.taskMeta}>
            {task.projectName && <span className={styles.taskProject}>{task.projectName}</span>}
            <span>Est: {task.estimatedHours}h</span>
            {task.remainingHours !== task.estimatedHours && (
              <span>Restante: {task.remainingHours}h</span>
            )}
          </div>
        </div>

        <form className={styles.form} onSubmit={handleSubmit}>
          <label className={styles.formLabel}>
            Persona
            <select
              className={styles.formSelect}
              value={personId}
              onChange={(e) => setPersonId(e.target.value)}
              required
            >
              {activePersns.length === 0 && <option value="">Sin personas activas</option>}
              {activePersns.map((p) => {
                const used = plannedByPerson(p.id);
                const avail = p.weeklyCapacityHours - used;
                return (
                  <option key={p.id} value={p.id}>
                    {p.name} — {avail > 0 ? `${avail}h disponibles` : 'sin capacidad'}
                  </option>
                );
              })}
            </select>
          </label>

          {selectedPerson && (
            <div className={styles.capacityBar}>
              <div className={styles.capacityLabels}>
                <span>{selectedPerson.name}</span>
                <span>{alreadyPlanned}h de {selectedPerson.weeklyCapacityHours}h usadas</span>
              </div>
              <div className={styles.barTrack}>
                <div
                  className={styles.barUsed}
                  style={{ width: `${Math.min((alreadyPlanned / selectedPerson.weeklyCapacityHours) * 100, 100)}%` }}
                />
                {plannedHours > 0 && (
                  <div
                    className={wouldExceed ? styles.barNewOver : styles.barNew}
                    style={{
                      width: `${Math.min((plannedHours / selectedPerson.weeklyCapacityHours) * 100, 100 - Math.min((alreadyPlanned / selectedPerson.weeklyCapacityHours) * 100, 100))}%`,
                    }}
                  />
                )}
              </div>
              <span className={styles.capacityAvail}>
                {available >= plannedHours
                  ? `✅ Quedarán ${available - plannedHours}h disponibles`
                  : `⚠️ Excede capacidad por ${plannedHours - available}h (solo advertencia)`}
              </span>
            </div>
          )}

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
            />
          </label>

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
            <button type="submit" className={styles.btnPrimary} disabled={saving || !personId}>
              {saving ? 'Asignando…' : 'Asignar'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
