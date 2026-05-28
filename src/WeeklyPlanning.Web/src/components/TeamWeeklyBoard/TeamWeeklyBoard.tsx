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

  const handleDownloadHtml = () => {
    if (!plan) return;

    const weekLabel = weekStartDate
      ? new Date(weekStartDate + 'T12:00:00').toLocaleDateString('es-AR', { day: '2-digit', month: 'long', year: 'numeric' })
      : weekStartDate;

    const columnsHtml = Array.from(personGroups.entries()).map(([, assignments]) => {
      const name = assignments[0].personName;
      const totalHours = assignments.reduce((s, a) => s + a.plannedHours, 0);
      const tasksHtml = assignments.map(a => {
        const notes = a.notes ? `<p style="margin:4px 0 0;font-size:12px;color:#475569;">${a.notes}</p>` : '';
        return `
          <div style="background:white;border:1px solid #e2e8f0;border-radius:8px;padding:10px 12px;margin-bottom:8px;">
            <div style="display:flex;justify-content:space-between;align-items:flex-start;gap:8px;">
              <span style="font-size:13px;font-weight:600;color:#1e293b;flex:1;">${a.taskTitle}</span>
              <span style="font-size:13px;font-weight:700;color:#6F2DBD;white-space:nowrap;">${a.plannedHours}h</span>
            </div>
            ${notes}
          </div>`;
      }).join('');

      return `
        <div style="flex:1;min-width:200px;max-width:300px;background:#f8fafc;border-radius:10px;padding:12px;">
          <div style="display:flex;align-items:center;gap:10px;margin-bottom:12px;padding-bottom:10px;border-bottom:2px solid #e2e8f0;">
            <div style="width:36px;height:36px;border-radius:50%;background:#6F2DBD;color:white;display:flex;align-items:center;justify-content:center;font-weight:700;font-size:16px;flex-shrink:0;">
              ${name.charAt(0).toUpperCase()}
            </div>
            <div>
              <div style="font-weight:700;font-size:14px;color:#1e293b;">${name}</div>
              <div style="font-size:12px;color:#64748b;">${totalHours}h planificadas</div>
            </div>
          </div>
          ${tasksHtml}
        </div>`;
    }).join('');

    const html = `<!DOCTYPE html>
<html lang="es">
<head>
  <meta charset="UTF-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1.0" />
  <title>Tablero de equipo — ${weekLabel}</title>
  <style>
    * { box-sizing: border-box; }
    body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif; margin: 0; padding: 24px; background: #f1f5f9; color: #1e293b; }
    h1 { margin: 0 0 4px; font-size: 20px; font-weight: 700; }
    .subtitle { color: #64748b; font-size: 13px; margin-bottom: 20px; }
    .columns { display: flex; flex-wrap: wrap; gap: 16px; align-items: flex-start; }
    @media print { body { padding: 12px; background: white; } }
  </style>
</head>
<body>
  <h1>📋 Tablero de equipo</h1>
  <p class="subtitle">Semana del ${weekLabel} · Generado el ${new Date().toLocaleDateString('es-AR', { day: '2-digit', month: 'long', year: 'numeric', hour: '2-digit', minute: '2-digit' })}</p>
  <div class="columns">${columnsHtml}</div>
</body>
</html>`;

    const blob = new Blob([html], { type: 'text/html;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `tablero-equipo-${weekStartDate}.html`;
    a.click();
    URL.revokeObjectURL(url);
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

          <button
            type="button"
            className={styles.secondaryBtn}
            onClick={handleDownloadHtml}
            disabled={!plan || loading}
            title="Descargar tablero como archivo HTML"
          >
            📥 Descargar HTML
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