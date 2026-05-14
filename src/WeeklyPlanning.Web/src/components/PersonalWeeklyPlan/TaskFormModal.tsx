import { useEffect, useState } from 'react';
import type {
  PersonalPlanItemDto,
  PersonalItemCategory,
  DayOfWeek,
  CreatePersonalItemRequest,
} from '../../types/personalPlan';
import { CATEGORY_LABELS, ALL_DAYS, DAY_LABELS } from '../../types/personalPlan';
import { projectsApi } from '../../api/weeklyPlanApi';
import type { ProjectDto } from '../../api/weeklyPlanApi';
import styles from './PersonalWeeklyPlan.module.css';

interface TaskFormModalProps {
  item?: PersonalPlanItemDto;
  defaultDay?: DayOfWeek;
  onSave: (req: CreatePersonalItemRequest) => Promise<void>;
  onClose: () => void;
}

const EMPTY_FORM: CreatePersonalItemRequest = {
  title: '',
  description: '',
  category: 'Task',
  estimatedHours: undefined,
  plannedDayOfWeek: undefined,
  externalTaskId: '',
  chobiProjectId: undefined,
};

export function TaskFormModal({ item, defaultDay, onSave, onClose }: TaskFormModalProps) {
  const [form, setForm] = useState<CreatePersonalItemRequest>(() =>
    item
      ? {
          title: item.title,
          description: item.description ?? '',
          category: item.category,
          estimatedHours: item.estimatedHours ?? undefined,
          plannedDayOfWeek: item.plannedDayOfWeek ?? undefined,
          externalTaskId: item.externalTaskId ?? '',
          chobiProjectId: item.chobiProjectId ?? undefined,
        }
      : { ...EMPTY_FORM, plannedDayOfWeek: defaultDay }
  );
  const [projects, setProjects] = useState<ProjectDto[]>([]);
  const [projectsLoading, setProjectsLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [validationError, setValidationError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    setProjectsLoading(true);
    projectsApi.getAll()
      .then((data) => {
        if (!cancelled) setProjects(data);
      })
      .finally(() => {
        if (!cancelled) setProjectsLoading(false);
      });

    return () => { cancelled = true; };
  }, []);

  const set = <K extends keyof CreatePersonalItemRequest>(
    key: K,
    value: CreatePersonalItemRequest[K]
  ) => setForm((prev) => ({ ...prev, [key]: value }));

  const linkedProjects = projects.filter((project) => project.chobiProjectId != null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!form.title.trim()) {
      setValidationError('El título es requerido.');
      return;
    }
    if (form.estimatedHours !== undefined && (form.estimatedHours <= 0 || form.estimatedHours > 40)) {
      setValidationError('Las horas estimadas deben estar entre 0.5 y 40.');
      return;
    }
    setValidationError(null);
    setSaving(true);
    try {
      await onSave({
        ...form,
        title: form.title.trim(),
        description: form.description?.trim() || undefined,
        externalTaskId: form.externalTaskId?.trim() || undefined,
        chobiProjectId: form.chobiProjectId || undefined,
      });
      onClose();
    } catch {
      // El error se muestra en el componente padre
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className={styles.modalBackdrop} onClick={onClose} role="dialog" aria-modal="true">
      <div className={styles.modal} onClick={(e) => e.stopPropagation()}>
        <header className={styles.modalHeader}>
          <h2 className={styles.modalTitle}>
            {item ? 'Editar tarea' : 'Nueva tarea personal'}
          </h2>
          <button className={styles.modalClose} onClick={onClose} aria-label="Cerrar">✕</button>
        </header>

        <form onSubmit={handleSubmit} className={styles.modalForm}>
          <div className={styles.formGroup}>
            <label className={styles.label} htmlFor="title">Título *</label>
            <input
              id="title"
              className={styles.input}
              type="text"
              value={form.title}
              onChange={(e) => set('title', e.target.value)}
              placeholder="¿Qué vas a hacer?"
              autoFocus
            />
          </div>

          <div className={styles.formRow}>
            <div className={styles.formGroup}>
              <label className={styles.label} htmlFor="category">Categoría</label>
              <select
                id="category"
                className={styles.select}
                value={form.category}
                onChange={(e) => set('category', e.target.value as PersonalItemCategory)}
              >
                {(Object.entries(CATEGORY_LABELS) as [PersonalItemCategory, string][]).map(
                  ([value, label]) => (
                    <option key={value} value={value}>{label}</option>
                  )
                )}
              </select>
            </div>

            <div className={styles.formGroup}>
              <label className={styles.label} htmlFor="day">Día</label>
              <select
                id="day"
                className={styles.select}
                value={form.plannedDayOfWeek ?? ''}
                onChange={(e) =>
                  set('plannedDayOfWeek', e.target.value ? (Number(e.target.value) as DayOfWeek) : undefined)
                }
              >
                <option value="">Sin asignar</option>
                {ALL_DAYS.map((d) => (
                  <option key={d} value={d}>{DAY_LABELS[d]}</option>
                ))}
              </select>
            </div>

            <div className={styles.formGroup}>
              <label className={styles.label} htmlFor="hours">Horas estimadas</label>
              <input
                id="hours"
                className={styles.input}
                type="number"
                min="0.5"
                max="40"
                step="0.5"
                value={form.estimatedHours ?? ''}
                onChange={(e) =>
                  set('estimatedHours', e.target.value ? parseFloat(e.target.value) : undefined)
                }
                placeholder="ej: 2"
              />
            </div>
          </div>

          <div className={styles.formGroup}>
            <label className={styles.label} htmlFor="description">Descripción (opcional)</label>
            <textarea
              id="description"
              className={styles.textarea}
              rows={3}
              value={form.description ?? ''}
              onChange={(e) => set('description', e.target.value)}
              placeholder="Contexto adicional, objetivo, referencias…"
            />
          </div>

          <div className={styles.formGroup}>
            <label className={styles.label} htmlFor="chobiProjectId">Proyecto Chrobi</label>
            <select
              id="chobiProjectId"
              className={styles.select}
              value={form.chobiProjectId ?? ''}
              onChange={(e) => set('chobiProjectId', e.target.value ? Number(e.target.value) : undefined)}
              disabled={projectsLoading}
            >
              <option value="">Sin proyecto</option>
              {linkedProjects.map((project) => (
                <option key={project.id} value={project.chobiProjectId ?? undefined}>
                  {project.name} (ID: {project.chobiProjectId})
                </option>
              ))}
            </select>
          </div>

          <div className={styles.formGroup}>
            <label className={styles.label} htmlFor="externalTaskId">
              ID en sistema externo (opcional)
            </label>
            <input
              id="externalTaskId"
              className={styles.input}
              type="text"
              value={form.externalTaskId ?? ''}
              onChange={(e) => set('externalTaskId', e.target.value)}
              placeholder="ej: TASK-1234"
            />
          </div>

          {validationError && (
            <p className={styles.formError} role="alert">{validationError}</p>
          )}

          <footer className={styles.modalFooter}>
            <button type="button" className={styles.btnSecondary} onClick={onClose}>
              Cancelar
            </button>
            <button type="submit" className={styles.btnPrimary} disabled={saving}>
              {saving ? 'Guardando…' : item ? 'Guardar cambios' : 'Agregar tarea'}
            </button>
          </footer>
        </form>
      </div>
    </div>
  );
}
