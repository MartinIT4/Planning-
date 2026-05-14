import { useEffect, useMemo, useState } from 'react';
import { usePersonalWeeklyPlan } from '../../hooks/usePersonalWeeklyPlan';
import { DayColumn } from './DayColumn';
import { TaskFormModal } from './TaskFormModal';
import type {
  PersonalPlanItemDto,
  DayOfWeek,
  CreatePersonalItemRequest,
  PublishResultDto,
} from '../../types/personalPlan';
import { ALL_DAYS } from '../../types/personalPlan';
import styles from './PersonalWeeklyPlan.module.css';

interface PersonalWeeklyPlanProps {
  ownerId: string;
  weekStartDate: string;
}

type ModalState =
  | { mode: 'closed' }
  | { mode: 'create'; defaultDay?: DayOfWeek }
  | { mode: 'edit'; item: PersonalPlanItemDto };

export function PersonalWeeklyPlan({ ownerId, weekStartDate }: PersonalWeeklyPlanProps) {
  const {
    plan,
    isLoading,
    isMutating,
    error,
    reload,
    addItem,
    updateItem,
    deleteItem,
    sendToExternal,
    publishToExternal,
  } = usePersonalWeeklyPlan(ownerId, weekStartDate);

  const [modal, setModal] = useState<ModalState>({ mode: 'closed' });
  const [sendingId, setSendingId] = useState<string | null>(null);
  const [confirmDeleteId, setConfirmDeleteId] = useState<string | null>(null);
  const [isPublishing, setIsPublishing] = useState(false);
  const [publishResult, setPublishResult] = useState<PublishResultDto | null>(null);

  useEffect(() => {
    if (!publishResult) return;
    const timer = window.setTimeout(() => setPublishResult(null), 6000);
    return () => window.clearTimeout(timer);
  }, [publishResult]);

  const itemsByDay = useMemo(() => {
    const map = new Map<DayOfWeek | null, PersonalPlanItemDto[]>();
    ALL_DAYS.forEach((d) => map.set(d, []));
    map.set(null, []);
    plan?.items.forEach((item) => {
      const key = item.plannedDayOfWeek ?? null;
      map.get(key)!.push(item);
    });
    return map;
  }, [plan]);

  const totalHours = useMemo(
    () => plan?.items.reduce((s, i) => s + (i.estimatedHours ?? 0), 0) ?? 0,
    [plan]
  );
  const sentCount = plan?.items.filter((i) => i.status === 'SentToExternal').length ?? 0;
  const publishableCount = plan?.items.filter(
    (i) => i.status !== 'SentToExternal' && i.chobiProjectId != null
  ).length ?? 0;

  const dayDates = useMemo(() => {
    const start = new Date(weekStartDate + 'T00:00:00Z');
    return ALL_DAYS.reduce((acc, d) => {
      const date = new Date(start);
      date.setUTCDate(start.getUTCDate() + (d - 1));
      acc[d] = date;
      return acc;
    }, {} as Record<DayOfWeek, Date>);
  }, [weekStartDate]);

  const handleSave = async (req: CreatePersonalItemRequest) => {
    if (modal.mode === 'edit') {
      await updateItem(modal.item.id, req);
    } else {
      await addItem(req);
    }
  };

  const handleDelete = async (itemId: string) => {
    setConfirmDeleteId(null);
    await deleteItem(itemId);
  };

  const handleSendToExternal = async (itemId: string) => {
    setSendingId(itemId);
    try {
      await sendToExternal(itemId);
    } finally {
      setSendingId(null);
    }
  };

  const handlePublish = async () => {
    if (!plan || publishableCount === 0) return;
    if (!confirm(`¿Enviar ${publishableCount} tareas a Chrobi?`)) return;

    setIsPublishing(true);
    try {
      const result = await publishToExternal();
      setPublishResult(result);
    } finally {
      setIsPublishing(false);
    }
  };

  const formattedWeek = useMemo(() => {
    const start = new Date(weekStartDate + 'T00:00:00Z');
    const end = new Date(weekStartDate + 'T00:00:00Z');
    end.setUTCDate(end.getUTCDate() + 4);
    const fmt = (d: Date) =>
      d.toLocaleDateString('es-AR', { day: 'numeric', month: 'long', year: 'numeric', timeZone: 'UTC' });
    return `${fmt(start)} – ${fmt(end)}`;
  }, [weekStartDate]);

  if (isLoading) {
    return (
      <div className={styles.stateContainer}>
        <div className={styles.spinner} aria-label="Cargando…" />
        <p>Cargando plan personal…</p>
      </div>
    );
  }

  if (error && !plan) {
    return (
      <div className={styles.stateContainer}>
        <div className={styles.errorBox} role="alert">
          <strong>Error al cargar el plan</strong>
          <p>{error}</p>
          <button className={styles.btnPrimary} onClick={reload}>Reintentar</button>
        </div>
      </div>
    );
  }

  if (!plan) return null;

  const unassigned = itemsByDay.get(null) ?? [];

  return (
    <section className={styles.personalBoard} aria-label="Plan semanal personal">
      <header className={styles.personalHeader}>
        <div className={styles.personalTitle}>
          <h1 className={styles.weekTitle}>Mi semana · {formattedWeek}</h1>
          <span className={`${styles.statusBadge} ${plan.status === 'Confirmed' ? styles.status_Confirmed : styles.status_Draft}`}>
            {plan.status === 'Confirmed' ? 'Confirmado' : 'Borrador'}
          </span>
        </div>

        <div className={styles.personalSummary}>
          <span>{plan.items.length} tareas</span>
          <span>·</span>
          <span>{totalHours}h estimadas</span>
          {sentCount > 0 && (
            <>
              <span>·</span>
              <span className={styles.sentSummary}>
                ✓ {sentCount} enviada{sentCount !== 1 ? 's' : ''} al sistema
              </span>
            </>
          )}
        </div>

        {plan.notes && <p className={styles.boardNotes}>{plan.notes}</p>}

        <div className={styles.headerActions}>
          <button
            className={styles.btnSecondary}
            onClick={handlePublish}
            disabled={isMutating || isPublishing || publishableCount === 0}
          >
            {isPublishing ? 'Publicando…' : '🚀 Publicar en Chrobi'}
          </button>
          <button
            className={styles.btnPrimary}
            onClick={() => setModal({ mode: 'create' })}
            disabled={isMutating || isPublishing}
          >
            + Nueva tarea
          </button>
          <button className={styles.btnSecondary} onClick={reload} title="Actualizar">
            ↻ Actualizar
          </button>
        </div>
      </header>

      {publishResult && (
        <div
          className={`${styles.publishBanner} ${publishResult.failed > 0 ? styles.publishBannerWarning : styles.publishBannerSuccess}`}
          role="status"
        >
          ✓ {publishResult.succeeded} tareas enviadas{publishResult.failed > 0 ? `, ${publishResult.failed} error${publishResult.failed !== 1 ? 'es' : ''}` : ''}
        </div>
      )}

      {isMutating && (
        <div className={styles.mutatingBanner} role="status">
          Guardando cambios…
        </div>
      )}

      <div className={styles.daysContainer}>
        {ALL_DAYS.map((day) => (
          <DayColumn
            key={day}
            day={day}
            date={dayDates[day]}
            items={itemsByDay.get(day) ?? []}
            isMutating={isMutating || sendingId !== null || isPublishing}
            onAddClick={() => setModal({ mode: 'create', defaultDay: day })}
            onEdit={(item) => setModal({ mode: 'edit', item })}
            onDelete={(id) => setConfirmDeleteId(id)}
            onSendToExternal={handleSendToExternal}
          />
        ))}
      </div>

      {unassigned.length > 0 && (
        <div className={styles.unassignedSection}>
          <DayColumn
            day={null}
            items={unassigned}
            isMutating={isMutating || sendingId !== null || isPublishing}
            onAddClick={() => setModal({ mode: 'create' })}
            onEdit={(item) => setModal({ mode: 'edit', item })}
            onDelete={(id) => setConfirmDeleteId(id)}
            onSendToExternal={handleSendToExternal}
          />
        </div>
      )}

      {modal.mode !== 'closed' && (
        <TaskFormModal
          item={modal.mode === 'edit' ? modal.item : undefined}
          defaultDay={modal.mode === 'create' ? modal.defaultDay : undefined}
          onSave={handleSave}
          onClose={() => setModal({ mode: 'closed' })}
        />
      )}

      {confirmDeleteId && (
        <div className={styles.modalBackdrop} onClick={() => setConfirmDeleteId(null)}>
          <div className={styles.confirmDialog} onClick={(e) => e.stopPropagation()}>
            <p>¿Eliminar esta tarea del plan personal?</p>
            <div className={styles.confirmActions}>
              <button className={styles.btnSecondary} onClick={() => setConfirmDeleteId(null)}>
                Cancelar
              </button>
              <button
                className={styles.btnDanger}
                onClick={() => handleDelete(confirmDeleteId)}
              >
                Eliminar
              </button>
            </div>
          </div>
        </div>
      )}
    </section>
  );
}
