import { useState, useEffect, useCallback } from 'react';
import type { BacklogItemDto } from '../../types/backlog';
import { backlogApi, type CreateBacklogItemRequest } from '../../api/backlogApi';
import { projectsApi, type ProjectDto } from '../../api/weeklyPlanApi';
import styles from './ConfigView.module.css';

interface TaskForm {
  title: string;
  description: string;
  projectId: string;
  estimatedHours: number;
}

const EMPTY_FORM: TaskForm = { title: '', description: '', projectId: '', estimatedHours: 8 };

export function BacklogManager() {
  const [items, setItems] = useState<BacklogItemDto[]>([]);
  const [projects, setProjects] = useState<ProjectDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [showForm, setShowForm] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [form, setForm] = useState<TaskForm>(EMPTY_FORM);
  const [saving, setSaving] = useState(false);
  const [seeding, setSeeding] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const [taskData, projectData] = await Promise.all([backlogApi.getAll(), projectsApi.getAll()]);
      setItems(taskData);
      setProjects(projectData);
    }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { load(); }, [load]);

  // Group by project
  const byProject = items.reduce<Record<string, BacklogItemDto[]>>((acc, item) => {
    const proj = item.projectName ?? '(Sin proyecto)';
    (acc[proj] ??= []).push(item);
    return acc;
  }, {});
  const projectNames = Object.keys(byProject).sort();

  const openAdd = (projectId = '') => {
    setForm({ ...EMPTY_FORM, projectId });
    setEditingId(null); setShowForm(true); setError(null);
  };

  const openEdit = (item: BacklogItemDto) => {
    // Try to match projectName to a known project id
    const matchedProject = projects.find(p => p.name === item.projectName);
    setForm({
      title: item.title,
      description: item.description ?? '',
      projectId: matchedProject?.id ?? '',
      estimatedHours: item.estimatedHours,
    });
    setEditingId(item.id); setShowForm(true); setError(null);
  };

  const cancel = () => { setShowForm(false); setEditingId(null); };

  const save = async () => {
    if (!form.title.trim()) { setError('El título es requerido.'); return; }
    setSaving(true); setError(null);
    try {
      const selectedProject = projects.find(p => p.id === form.projectId);
      const payload: CreateBacklogItemRequest = {
        title: form.title.trim(),
        description: form.description.trim() || undefined,
        projectId: selectedProject?.id,
        projectName: selectedProject?.name,
        estimatedHours: form.estimatedHours,
      };
      if (editingId) {
        const updated = await backlogApi.update(editingId, payload);
        setItems((prev) => prev.map((i) => (i.id === updated.id ? updated : i)));
      } else {
        const created = await backlogApi.create(payload);
        setItems((prev) => [...prev, created]);
      }
      cancel();
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : 'Error al guardar');
    } finally { setSaving(false); }
  };

  const remove = async (id: string, title: string) => {
    if (!confirm(`¿Eliminar "${title}" del backlog?`)) return;
    try {
      await backlogApi.delete(id);
      setItems((prev) => prev.filter((i) => i.id !== id));
    } catch (e: unknown) { alert(e instanceof Error ? e.message : 'Error'); }
  };

  const seed = async () => {
    setSeeding(true);
    try { await backlogApi.seed(); await load(); }
    finally { setSeeding(false); }
  };

  return (
    <div className={styles.section}>
      <div className={styles.sectionHeader}>
        <h2 className={styles.sectionTitle}>📋 Tareas / Backlog</h2>
        {!showForm && (
          <button className={styles.btnPrimary} onClick={() => openAdd()}>+ Nueva tarea</button>
        )}
      </div>

      <div className={styles.seedRow}>
        <button className={styles.btnSecondary} onClick={seed} disabled={seeding}>
          {seeding ? 'Cargando…' : '🧪 Cargar tareas de prueba'}
        </button>
        <span className={styles.hint}>Carga 8 tareas de ejemplo para probar la planificación</span>
      </div>

      {showForm && (
        <div className={styles.formCard}>
          <p className={styles.formTitle}>{editingId ? 'Editar tarea' : 'Nueva tarea'}</p>
          {error && <p style={{ color: '#dc2626', fontSize: '0.82rem', marginBottom: '0.5rem' }}>{error}</p>}
          <div className={styles.formRow}>
            <div className={styles.formGroup} style={{ flex: 1, minWidth: 200 }}>
              <label>Título *</label>
              <input value={form.title} onChange={(e) => setForm({ ...form, title: e.target.value })}
                placeholder="Descripción breve de la tarea" style={{ width: '100%' }} />
            </div>
            <div className={styles.formGroup} style={{ minWidth: 180 }}>
              <label>Proyecto</label>
              <select
                value={form.projectId}
                onChange={(e) => setForm({ ...form, projectId: e.target.value })}
                style={{ width: 180, padding: '0.35rem 0.5rem', border: '1px solid #cbd5e1', borderRadius: 6, fontSize: '0.875rem' }}
              >
                <option value="">— Sin proyecto —</option>
                {projects.map((p) => (
                  <option key={p.id} value={p.id}>{p.name}</option>
                ))}
              </select>
            </div>
            <div className={styles.formGroup}>
              <label>Horas estimadas</label>
              <input type="number" min={0.5} step={0.5} value={form.estimatedHours}
                onChange={(e) => setForm({ ...form, estimatedHours: Number(e.target.value) })}
                style={{ width: 80 }} />
            </div>
          </div>
          <div className={styles.formRow}>
            <div className={styles.formGroup} style={{ flex: 1 }}>
              <label>Descripción (opcional)</label>
              <textarea value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })}
                placeholder="Contexto, criterios de aceptación, etc." rows={2}
                style={{ width: '100%', resize: 'vertical' }} />
            </div>
          </div>
          <div className={styles.formActions}>
            <button className={styles.btnPrimary} onClick={save} disabled={saving}>
              {saving ? 'Guardando…' : editingId ? 'Actualizar' : 'Agregar al backlog'}
            </button>
            <button className={styles.btnSecondary} onClick={cancel}>Cancelar</button>
          </div>
        </div>
      )}

      {loading ? (
        <p style={{ color: '#94a3b8' }}>Cargando backlog…</p>
      ) : items.length === 0 ? (
        <div className={styles.empty}>
          El backlog está vacío.<br />
          Agregá tareas manualmente o usá el botón "Cargar tareas de prueba".
        </div>
      ) : (
        projectNames.map((proj) => (
          <div key={proj} className={styles.projectGroup}>
            <div className={styles.projectName}>
              <span>📁 {proj}</span>
              <button className={styles.btnSecondary}
                onClick={() => {
                  const matched = projects.find(p => p.name === proj);
                  openAdd(matched?.id ?? '');
                }}
                style={{ fontSize: '0.75rem', padding: '0.25rem 0.6rem' }}>
                + Tarea en este proyecto
              </button>
            </div>
            <table className={styles.table}>
              <thead>
                <tr><th>Tarea</th><th>Est.</th><th>Estado</th><th>Acciones</th></tr>
              </thead>
              <tbody>
                {byProject[proj].map((item) => (
                  <tr key={item.id}>
                    <td>
                      <strong>{item.title}</strong>
                      {item.description && (
                        <div style={{ fontSize: '0.78rem', color: '#64748b', marginTop: 2 }}>
                          {item.description}
                        </div>
                      )}
                    </td>
                    <td style={{ whiteSpace: 'nowrap' }}>{item.estimatedHours}h</td>
                    <td>
                      <span style={{
                        fontSize: '0.75rem', padding: '0.2rem 0.5rem', borderRadius: 9999,
                        background: item.status === 'in_progress' ? '#dbeafe' : '#f1f5f9',
                        color: item.status === 'in_progress' ? '#1d4ed8' : '#475569',
                      }}>
                        {item.status === 'todo' ? 'Pendiente' : item.status === 'in_progress' ? 'En curso' : item.status}
                      </span>
                    </td>
                    <td>
                      <div className={styles.actions}>
                        <button className={styles.btnSecondary} onClick={() => openEdit(item)}>✏️</button>
                        <button className={styles.btnDanger} onClick={() => remove(item.id, item.title)}>🗑️</button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ))
      )}
    </div>
  );
}
