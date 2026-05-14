import { useState } from 'react';
import type { WeeklyPlanDto, WeeklyPlanStatus } from '../../types/weeklyPlan';
import { weeklyPlanApi } from '../../api/weeklyPlanApi';
import styles from './PlanningView.module.css';

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

function formatWeekLabel(plan: WeeklyPlanDto) {
  const fmt = (d: string) =>
    new Date(d + 'T00:00:00Z').toLocaleDateString('es-AR', {
      day: '2-digit',
      month: '2-digit',
      timeZone: 'UTC',
    });
  return `Semana ${fmt(plan.weekStartDate)} → ${fmt(plan.weekEndDate)} [${STATUS_LABEL[plan.status]}]`;
}

/** Returns the Monday of the current week as yyyy-MM-dd */
function currentMonday(): string {
  const d = new Date();
  const day = d.getDay();
  const diff = day === 0 ? -6 : 1 - day;
  d.setDate(d.getDate() + diff);
  return d.toISOString().slice(0, 10);
}

interface PlanSelectorProps {
  plans: WeeklyPlanDto[];
  selectedPlan: WeeklyPlanDto | null;
  onSelect: (plan: WeeklyPlanDto) => void;
  onPlanCreated: (plan: WeeklyPlanDto) => void;
  onPlanUpdated: (plan: WeeklyPlanDto) => void;
}

export function PlanSelector({
  plans,
  selectedPlan,
  onSelect,
  onPlanCreated,
  onPlanUpdated,
}: PlanSelectorProps) {
  const [showNewForm, setShowNewForm] = useState(false);
  const [newDate, setNewDate] = useState(currentMonday());
  const [newNotes, setNewNotes] = useState('');
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    setError(null);
    try {
      const plan = await weeklyPlanApi.createPlan({
        weekStartDate: newDate,
        notes: newNotes || undefined,
      });
      onPlanCreated(plan);
      setShowNewForm(false);
      setNewDate(currentMonday());
      setNewNotes('');
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Error al crear el plan');
    } finally {
      setSaving(false);
    }
  };

  const handleStatusAction = async (action: 'confirm' | 'close' | 'revert-to-draft') => {
    if (!selectedPlan) return;
    setSaving(true);
    setError(null);
    try {
      const updated = await weeklyPlanApi.updateStatus(selectedPlan.id, action);
      onPlanUpdated(updated);
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Error al cambiar estado');
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className={styles.planSelectorWrapper}>
      <div className={styles.planSelectorRow}>
        <div className={styles.planSelectorLeft}>
          <label className={styles.selectorLabel}>Semana:</label>
          <select
            className={styles.selectorSelect}
            value={selectedPlan?.id ?? ''}
            onChange={(e) => {
              const p = plans.find((x) => x.id === e.target.value);
              if (p) onSelect(p);
            }}
          >
            {plans.length === 0 && <option value="">Sin planes</option>}
            {plans.map((p) => (
              <option key={p.id} value={p.id}>
                {formatWeekLabel(p)}
              </option>
            ))}
          </select>
          <button
            className={styles.btnSecondary}
            onClick={() => setShowNewForm((v) => !v)}
            disabled={saving}
          >
            + Nueva semana
          </button>
        </div>

        {selectedPlan && (
          <div className={styles.planSelectorRight}>
            <span className={`${styles.statusBadge} ${STATUS_CLASS[selectedPlan.status]}`}>
              {STATUS_LABEL[selectedPlan.status]}
            </span>
            {selectedPlan.status === 'Draft' && (
              <button
                className={styles.btnPrimary}
                onClick={() => handleStatusAction('confirm')}
                disabled={saving}
              >
                Confirmar
              </button>
            )}
            {selectedPlan.status === 'Confirmed' && (
              <button
                className={styles.btnWarning}
                onClick={() => handleStatusAction('close')}
                disabled={saving}
              >
                Cerrar
              </button>
            )}
            {selectedPlan.status !== 'Draft' && (
              <button
                className={styles.btnSecondary}
                onClick={() => handleStatusAction('revert-to-draft')}
                disabled={saving}
              >
                Reabrir borrador
              </button>
            )}
            {selectedPlan.notes && (
              <span className={styles.planNotes} title={selectedPlan.notes}>
                📝 {selectedPlan.notes.slice(0, 60)}{selectedPlan.notes.length > 60 ? '…' : ''}
              </span>
            )}
          </div>
        )}
      </div>

      {showNewForm && (
        <form className={styles.newPlanForm} onSubmit={handleCreate}>
          <label className={styles.formLabel}>
            Lunes de la semana:
            <input
              type="date"
              className={styles.formInput}
              value={newDate}
              onChange={(e) => setNewDate(e.target.value)}
              required
            />
          </label>
          <label className={styles.formLabel}>
            Notas (opcional):
            <input
              type="text"
              className={styles.formInput}
              value={newNotes}
              onChange={(e) => setNewNotes(e.target.value)}
              placeholder="Objetivos de la semana…"
            />
          </label>
          <div className={styles.formActions}>
            <button type="submit" className={styles.btnPrimary} disabled={saving}>
              {saving ? 'Creando…' : 'Crear plan'}
            </button>
            <button
              type="button"
              className={styles.btnSecondary}
              onClick={() => setShowNewForm(false)}
              disabled={saving}
            >
              Cancelar
            </button>
          </div>
          {error && <p className={styles.formError}>{error}</p>}
        </form>
      )}
    </div>
  );
}
