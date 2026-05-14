import { useCallback, useEffect, useRef, useState } from 'react';
import { weeklyPlanApi } from '../../api/weeklyPlanApi';
import type { TaskAssignmentDto, WeeklyPlanDto } from '../../types/weeklyPlan';
import styles from './TeamWeeklyBoard.module.css';

interface TeamWeeklyBoardProps {
  weekStartDate: string;
}

interface SendResult {
  succeeded: number;
  failed: number;
}

function formatHours(hours: number): string {
  return `${hours}h`;
}

function getChobiProjectAvailable(assignment: TaskAssignmentDto): boolean {
  // ExternalTaskId format: "PROJ-{guid}" — project must exist and be linked
  return assignment.externalTaskId.startsWith('PROJ-');
}

export function TeamWeeklyBoard({ weekStartDate }: TeamWeeklyBoardProps) {
  const [plan, setPlan] = useState<WeeklyPlanDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [noPlan, setNoPlan] = useState(false);
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set());
  const [isSending, setIsSending] = useState(false);
  const [sendingTaskIds, setSendingTaskIds] = useState<Set<string>>(new Set());
  const [sendResult, setSendResult] = useState<SendResult | null>(null);
  const loadRef = useRef(0);
  const sendingRef = useRef(false); // synchronous guard against double-click

  const loadBoard = useCallback(async () => {
    const rid = ++loadRef.current;
    setLoading(true);
    setError(null);
    setNoPlan(false);

    try {
      const data = await weeklyPlanApi.getByWeek(weekStartDate);
      if (loadRef.current !== rid) return;
      setPlan(data);
    } catch (err: unknown) {
      if (loadRef.current !== rid) return;
      const msg = err instanceof Error ? err.message : String(err);
      if (msg.includes('404')) {
        setNoPlan(true);
        setPlan(null);
      } else {
        setError(msg);
        setPlan(null);
      }
    } finally {
      if (loadRef.current === rid) setLoading(false);
    }
  }, [weekStartDate]);

  useEffect(() => {
    setSelectedIds(new Set());
    setSendResult(null);
  }, [weekStartDate]);

  useEffect(() => { void loadBoard(); }, [loadBoard]);

  useEffect(() => {
    if (!sendResult) return;
    const t = window.setTimeout(() => setSendResult(null), 5000);
    return () => window.clearTimeout(t);
  }, [sendResult]);

  // Group assignments by personId
  const personGroups = (() => {
    if (!plan) return new Map<string, TaskAssignmentDto[]>();
    const map = new Map<string, TaskAssignmentDto[]>();
    for (const a of plan.assignments) {
      const list = map.get(a.personId) ?? [];
      list.push(a);
      map.set(a.personId, list);
    }
    return map;
  })();

  const personIds = Array.from(personGroups.keys());

  const canSend = (a: TaskAssignmentDto) =>
    a.sentToExternalAt == null && getChobiProjectAvailable(a);

  const sendableCount = selectedIds.size;

  const toggle = (id: string) => {
    setSelectedIds(prev => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id); else next.add(id);
      return next;
    });
  };

  const toggleAllForPerson = (personId: string) => {
    const sendable = (personGroups.get(personId) ?? []).filter(canSend).map(a => a.id);
    const allSelected = sendable.every(id => selectedIds.has(id));
    setSelectedIds(prev => {
      const next = new Set(prev);
      if (allSelected) {
        sendable.forEach(id => next.delete(id));
      } else {
        sendable.forEach(id => next.add(id));
      }
      return next;
    });
  };

  const handleSend = async () => {
    if (!plan || sendableCount === 0 || sendingRef.current) return;
    sendingRef.current = true;
    setIsSending(true);
    setSendResult(null);
    let ok = 0, fail = 0;
    const toSend = Array.from(selectedIds);
    for (const assignmentId of toSend) {
      try {
        await weeklyPlanApi.sendAssignmentToExternal(plan.id, assignmentId);
        ok++;
      } catch { fail++; }
    }
    setSelectedIds(new Set());
    await loadBoard();
    setSendResult({ succeeded: ok, failed: fail });
    setIsSending(false);
    sendingRef.current = false;
  };

  const handleSendOne = async (assignmentId: string) => {
    if (!plan || sendingRef.current) return;
    setSendingTaskIds(prev => new Set(prev).add(assignmentId));
    try {
      await weeklyPlanApi.sendAssignmentToExternal(plan.id, assignmentId);
    } catch {
      // errors will be shown by reload
    }
    setSelectedIds(prev => {
      const next = new Set(prev);
      next.delete(assignmentId);
      return next;
    });
    setSendingTaskIds(prev => {
      const next = new Set(prev);
      next.delete(assignmentId);
      return next;
    });
    await loadBoard();
  };

  return (
    <section className={styles.board} aria-label="Tablero de equipo">
      <header className={styles.boardHeader}>
        <h1 className={styles.boardTitle}>Tablero de equipo</h1>

        <div className={styles.headerActions}>
          <button
            type="button"
            className={styles.secondaryBtn}
            onClick={() => void loadBoard()}
            disabled={loading || isSending}
          >
            ↻ Actualizar
          </button>

          <span className={styles.selectedCount}>
            {sendableCount} seleccionada{sendableCount === 1 ? '' : 's'}
          </span>

          <button
            type="button"
            className={styles.sendBtn}
            onClick={() => void handleSend()}
            disabled={sendableCount === 0 || isSending}
          >
            {isSending ? 'Enviando…' : `🚀 Enviar ${sendableCount} tarea${sendableCount === 1 ? '' : 's'} a Chrobi`}
          </button>
        </div>
      </header>

      {sendResult && (
        <div
          className={`${styles.resultBanner} ${sendResult.failed > 0 ? styles.resultBannerWarning : styles.resultBannerSuccess}`}
          role="status"
        >
          ✓ {sendResult.succeeded} enviadas{sendResult.failed > 0 ? ` · ${sendResult.failed} error${sendResult.failed === 1 ? '' : 'es'}` : ''}
        </div>
      )}

      {loading ? (
        <div className={styles.stateContainer}>
          <div className={styles.spinner} aria-label="Cargando…" />
          <p>Cargando tablero…</p>
        </div>
      ) : error ? (
        <div className={styles.stateContainer}>
          <div className={styles.errorBox} role="alert">
            <strong>Error al cargar el tablero</strong>
            <p>{error}</p>
            <button type="button" className={styles.sendBtn} onClick={() => void loadBoard()}>Reintentar</button>
          </div>
        </div>
      ) : noPlan ? (
        <div className={styles.stateContainer}>
          <div className={styles.emptyBoard}>No hay plan de equipo para esta semana.</div>
        </div>
      ) : personIds.length === 0 ? (
        <div className={styles.stateContainer}>
          <div className={styles.emptyBoard}>El plan no tiene asignaciones.</div>
        </div>
      ) : (
        <div className={styles.columnsContainer}>
          {personIds.map(personId => {
            const assignments = personGroups.get(personId)!;
            const firstName = assignments[0].personName;

            return (
              <section key={personId} className={styles.personColumn}>
                <header className={styles.personHeader}>
                  <div className={styles.personAvatar} aria-hidden="true">
                    {firstName.charAt(0).toUpperCase()}
                  </div>
                  <div className={styles.personInfo}>
                    <span className={styles.personName}>{firstName}</span>
                  </div>
                  {(() => {
                    const sendable = assignments.filter(canSend);
                    if (sendable.length === 0) return null;
                    const selectedCount = sendable.filter(a => selectedIds.has(a.id)).length;
                    const allSelected = selectedCount === sendable.length;
                    const someSelected = selectedCount > 0 && !allSelected;
                    return (
                      <input
                        type="checkbox"
                        className={styles.selectAllCheckbox}
                        checked={allSelected}
                        ref={el => { if (el) el.indeterminate = someSelected; }}
                        onChange={() => toggleAllForPerson(personId)}
                        disabled={isSending || loading}
                        title={allSelected ? 'Deseleccionar todas' : 'Seleccionar todas'}
                        aria-label={`Seleccionar todas las tareas de ${firstName}`}
                      />
                    );
                  })()}
                </header>

                <div className={styles.taskList}>
                  {assignments.map(a => {
                    const sent = a.sentToExternalAt != null;
                    const sendable = canSend(a);
                    const noProject = !sent && !sendable;

                    return (
                      <article
                        key={a.id}
                        className={[
                          styles.taskCard,
                          sent ? styles.taskCardSent : '',
                          noProject ? styles.taskCardNoProject : '',
                        ].filter(Boolean).join(' ')}
                      >
                        <div className={styles.checkRow}>
                          {sendable ? (
                            <>
                              <input
                                type="checkbox"
                                className={styles.checkbox}
                                checked={selectedIds.has(a.id)}
                                onChange={() => toggle(a.id)}
                                disabled={isSending || loading || sendingTaskIds.has(a.id)}
                                aria-label={`Seleccionar ${a.taskTitle}`}
                              />
                              <button
                                type="button"
                                className={styles.quickSendBtn}
                                onClick={() => void handleSendOne(a.id)}
                                disabled={isSending || loading || sendingTaskIds.has(a.id)}
                                title="Enviar esta tarea a Chrobi"
                              >
                                {sendingTaskIds.has(a.id) ? '…' : '↑'}
                              </button>
                            </>
                          ) : sent ? (
                            <span className={styles.sentIcon} aria-hidden="true">✓</span>
                          ) : null}

                          <div className={styles.taskMain}>
                            <div className={styles.titleRow}>
                              <span className={styles.taskTitle}>{a.taskTitle}</span>
                              <span className={styles.taskHours}>{formatHours(a.plannedHours)}</span>
                            </div>
                            {a.notes && !sent && (
                              <div className={styles.metaRow}>
                                <span className={styles.noteText}>{a.notes}</span>
                              </div>
                            )}
                            {noProject && (
                              <span className={styles.noProjectText}>Sin proyecto Chrobi</span>
                            )}
                          </div>
                        </div>
                      </article>
                    );
                  })}
                </div>
              </section>
            );
          })}
        </div>
      )}
    </section>
  );
}