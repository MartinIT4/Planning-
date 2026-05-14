import { useState, useEffect, useCallback } from 'react';
import type { ProjectDto } from '../../api/weeklyPlanApi';
import { projectsApi } from '../../api/weeklyPlanApi';
import { chobiApi } from '../../api/chobiApi';
import type { ChobiProjectDto } from '../../types/chrobi';
import styles from './ConfigView.module.css';

interface ProjectForm {
  name: string;
  description: string;
  isBillable: boolean;
}

const EMPTY_FORM: ProjectForm = { name: '', description: '', isBillable: false };

export function ProjectManager() {
  const [projects, setProjects] = useState<ProjectDto[]>([]);
  const [chobiProjects, setChobiProjects] = useState<ChobiProjectDto[]>([]);
  const [selectedProjects, setSelectedProjects] = useState<Record<string, string>>({});
  const [loading, setLoading] = useState(true);
  const [syncing, setSyncing] = useState(false);
  const [linkingProjectId, setLinkingProjectId] = useState<string | null>(null);
  const [showForm, setShowForm] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [form, setForm] = useState<ProjectForm>(EMPTY_FORM);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [statusMessage, setStatusMessage] = useState<string | null>(null);
  const [statusKind, setStatusKind] = useState<'success' | 'error'>('success');

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const [projectsData, chobiProjectsData] = await Promise.all([
        projectsApi.getAll(),
        chobiApi.getProjects(),
      ]);
      setProjects(projectsData);
      setChobiProjects(chobiProjectsData);
      setSelectedProjects((prev) => {
        const next = { ...prev };
        projectsData.forEach((project) => {
          if (!next[project.id]) {
            const matchedProject = chobiProjectsData.find(
              (chobiProject) => chobiProject.name.toLowerCase() === project.name.toLowerCase()
            );
            if (matchedProject) next[project.id] = String(matchedProject.id);
          }
        });
        return next;
      });
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : 'Error al cargar datos');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { load(); }, [load]);

  const openAdd = () => { setForm(EMPTY_FORM); setEditingId(null); setShowForm(true); setError(null); };

  const openEdit = (p: ProjectDto) => {
    setForm({ name: p.name, description: p.description ?? '', isBillable: p.isBillable });
    setEditingId(p.id);
    setShowForm(true);
    setError(null);
  };

  const cancel = () => { setShowForm(false); setEditingId(null); };

  const save = async () => {
    if (!form.name.trim()) { setError('El nombre es requerido.'); return; }
    setSaving(true);
    setError(null);
    try {
      const body = { name: form.name.trim(), description: form.description.trim() || undefined, isBillable: form.isBillable };
      if (editingId) {
        const updated = await projectsApi.update(editingId, body);
        setProjects((prev) => prev.map((p) => (p.id === updated.id ? updated : p)));
      } else {
        const created = await projectsApi.create(body);
        setProjects((prev) => [...prev, created]);
      }
      cancel();
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : 'Error al guardar');
    } finally {
      setSaving(false);
    }
  };

  const syncFromChobi = async () => {
    setSyncing(true);
    setStatusMessage(null);
    try {
      const result = await chobiApi.sync();
      await load();
      setStatusKind('success');
      setStatusMessage(
        `✓ ${result.projectsLinked} vinculados, ${result.projectsCreated} nuevos · ${result.personsLinked} personas vinculadas, ${result.personsCreated} nuevas`
      );
    } catch (e: unknown) {
      setStatusKind('error');
      setStatusMessage(e instanceof Error ? e.message : 'Error al importar desde Chrobi');
    } finally {
      setSyncing(false);
    }
  };

  const linkProject = async (project: ProjectDto) => {
    const selectedId = Number(selectedProjects[project.id]);
    if (!selectedId) return;

    setLinkingProjectId(project.id);
    setStatusMessage(null);
    try {
      await chobiApi.setProjectChobiId(project.id, selectedId);
      await load();
      setStatusKind('success');
      setStatusMessage(`✓ ${project.name} vinculado correctamente`);
    } catch (e: unknown) {
      setStatusKind('error');
      setStatusMessage(e instanceof Error ? e.message : 'Error al vincular proyecto');
    } finally {
      setLinkingProjectId(null);
    }
  };

  const deactivate = async (id: string, name: string) => {
    if (!confirm(`¿Desactivar el proyecto "${name}"? Ya no aparecerá en los planes.`)) return;
    try {
      await projectsApi.deactivate(id);
      setProjects((prev) => prev.filter((p) => p.id !== id));
    } catch (e: unknown) {
      alert(e instanceof Error ? e.message : 'Error al desactivar');
    }
  };

  return (
    <div className={styles.section}>
      <div className={styles.sectionHeader}>
        <h2 className={styles.sectionTitle}>🗂️ Proyectos</h2>
        <div className={styles.headerActions}>
          <button className={styles.btnSecondary} onClick={syncFromChobi} disabled={syncing || loading}>
            {syncing ? 'Importando…' : '🔄 Importar desde Chrobi'}
          </button>
          {!showForm && (
            <button className={styles.btnPrimary} onClick={openAdd}>+ Agregar proyecto</button>
          )}
        </div>
      </div>

      {statusMessage && (
        <p className={`${styles.statusMessage} ${statusKind === 'success' ? styles.statusSuccess : styles.statusError}`}>
          {statusMessage}
        </p>
      )}

      {showForm && (
        <div className={styles.formCard}>
          <p className={styles.formTitle}>{editingId ? 'Editar proyecto' : 'Nuevo proyecto'}</p>
          {error && <p className={`${styles.statusMessage} ${styles.statusError}`}>{error}</p>}
          <div className={styles.formRow}>
            <div className={styles.formGroup}>
              <label>Nombre *</label>
              <input value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })}
                placeholder="Ej: Portal Web" style={{ width: 220 }} />
            </div>
            <div className={styles.formGroup}>
              <label>Descripción</label>
              <textarea value={form.description}
                onChange={(e) => setForm({ ...form, description: e.target.value })}
                placeholder="Descripción opcional" style={{ width: 320, height: 60 }} />
            </div>
            <div className={styles.formGroup}>
              <label>
                <input
                  type="checkbox"
                  checked={form.isBillable}
                  onChange={(e) => setForm({ ...form, isBillable: e.target.checked })}
                  style={{ marginRight: '0.4rem' }}
                />
                Facturable
              </label>
            </div>
          </div>
          <div className={styles.formActions}>
            <button className={styles.btnPrimary} onClick={save} disabled={saving}>
              {saving ? 'Guardando…' : editingId ? 'Actualizar' : 'Agregar'}
            </button>
            <button className={styles.btnSecondary} onClick={cancel}>Cancelar</button>
          </div>
        </div>
      )}

      {loading ? (
        <p style={{ color: '#94a3b8' }}>Cargando…</p>
      ) : error ? (
        <p className={`${styles.statusMessage} ${styles.statusError}`}>{error}</p>
      ) : projects.length === 0 ? (
        <div className={styles.empty}>
          No hay proyectos activos.<br />Agregá el primero para organizar las tareas.
        </div>
      ) : (
        <table className={styles.table}>
          <thead>
            <tr>
              <th>Nombre</th><th>Descripción</th><th>Facturable</th><th>ID Chrobi</th><th>Acciones</th>
            </tr>
          </thead>
          <tbody>
            {projects.map((p) => (
              <tr key={p.id}>
                <td><strong>{p.name}</strong></td>
                <td>{p.description ?? <span style={{ color: '#94a3b8' }}>—</span>}</td>
                <td style={{ textAlign: 'center' }}>{p.isBillable ? '✅' : '—'}</td>
                <td>
                  {p.chobiProjectId ? (
                    <span className={styles.badgeSuccess}>#{p.chobiProjectId}</span>
                  ) : (
                    <div className={styles.linkRow}>
                      <select
                        className={styles.linkSelect}
                        value={selectedProjects[p.id] ?? ''}
                        onChange={(e) => setSelectedProjects((prev) => ({ ...prev, [p.id]: e.target.value }))}
                      >
                        <option value="">Seleccionar proyecto…</option>
                        {chobiProjects.map((project) => (
                          <option key={project.id} value={project.id}>
                            {project.name} (ID: {project.id})
                          </option>
                        ))}
                      </select>
                      <button
                        className={styles.btnSecondary}
                        onClick={() => linkProject(p)}
                        disabled={!selectedProjects[p.id] || linkingProjectId === p.id}
                      >
                        {linkingProjectId === p.id ? 'Vinculando…' : 'Vincular'}
                      </button>
                    </div>
                  )}
                </td>
                <td>
                  <div className={styles.actions}>
                    <button className={styles.btnSecondary} onClick={() => openEdit(p)}>✏️ Editar</button>
                    <button className={styles.btnDanger} onClick={() => deactivate(p.id, p.name)}>Desactivar</button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}
