import { useState, useEffect, useCallback } from 'react';
import type { PersonDto } from '../../types/weeklyPlan';
import { personsApi } from '../../api/weeklyPlanApi';
import { chobiApi } from '../../api/chobiApi';
import type { ChobiUserDto } from '../../types/chrobi';
import styles from './ConfigView.module.css';

interface PersonForm {
  name: string;
  weeklyCapacityHours: number;
}

const EMPTY_FORM: PersonForm = { name: '', weeklyCapacityHours: 40 };

export function TeamManager() {
  const [persons, setPersons] = useState<PersonDto[]>([]);
  const [chobiUsers, setChobiUsers] = useState<ChobiUserDto[]>([]);
  const [selectedUsers, setSelectedUsers] = useState<Record<string, string>>({});
  const [loading, setLoading] = useState(true);
  const [syncing, setSyncing] = useState(false);
  const [linkingPersonId, setLinkingPersonId] = useState<string | null>(null);
  const [showForm, setShowForm] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [form, setForm] = useState<PersonForm>(EMPTY_FORM);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [statusMessage, setStatusMessage] = useState<string | null>(null);
  const [statusKind, setStatusKind] = useState<'success' | 'error'>('success');

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const [personsData, chobiUsersData] = await Promise.all([
        personsApi.getAll(),
        chobiApi.getUsers(),
      ]);
      setPersons(personsData);
      setChobiUsers(chobiUsersData);
      setSelectedUsers((prev) => {
        const next = { ...prev };
        personsData.forEach((person) => {
          if (!next[person.id]) {
            const matchedUser = chobiUsersData.find(
              (user) => user.description.toLowerCase() === person.name.toLowerCase()
            );
            if (matchedUser) next[person.id] = String(matchedUser.id);
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

  const openEdit = (p: PersonDto) => {
    setForm({ name: p.name, weeklyCapacityHours: p.weeklyCapacityHours });
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
      if (editingId) {
        const updated = await personsApi.update(editingId, form);
        setPersons((prev) => prev.map((p) => (p.id === updated.id ? updated : p)));
      } else {
        const created = await personsApi.create(form);
        setPersons((prev) => [...prev, created]);
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
        `✓ ${result.personsLinked} vinculadas, ${result.personsCreated} nuevas · ${result.projectsLinked} proyectos vinculados, ${result.projectsCreated} nuevos`
      );
    } catch (e: unknown) {
      setStatusKind('error');
      setStatusMessage(e instanceof Error ? e.message : 'Error al importar desde Chrobi');
    } finally {
      setSyncing(false);
    }
  };

  const linkPerson = async (person: PersonDto) => {
    const selectedId = Number(selectedUsers[person.id]);
    if (!selectedId) return;

    setLinkingPersonId(person.id);
    setStatusMessage(null);
    try {
      await chobiApi.setPersonChobiId(person.id, selectedId);
      await load();
      setStatusKind('success');
      setStatusMessage(`✓ ${person.name} vinculada correctamente`);
    } catch (e: unknown) {
      setStatusKind('error');
      setStatusMessage(e instanceof Error ? e.message : 'Error al vincular persona');
    } finally {
      setLinkingPersonId(null);
    }
  };

  const deactivate = async (id: string, name: string) => {
    if (!confirm(`¿Desactivar a "${name}"? Ya no aparecerá en los planes.`)) return;
    try {
      await personsApi.deactivate(id);
      setPersons((prev) => prev.filter((p) => p.id !== id));
    } catch (e: unknown) {
      alert(e instanceof Error ? e.message : 'Error al desactivar');
    }
  };

  return (
    <div className={styles.section}>
      <div className={styles.sectionHeader}>
        <h2 className={styles.sectionTitle}>👥 Equipo</h2>
        <div className={styles.headerActions}>
          <button className={styles.btnSecondary} onClick={syncFromChobi} disabled={syncing || loading}>
            {syncing ? 'Importando…' : '🔄 Importar desde Chrobi'}
          </button>
          {!showForm && (
            <button className={styles.btnPrimary} onClick={openAdd}>+ Agregar persona</button>
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
          <p className={styles.formTitle}>{editingId ? 'Editar persona' : 'Nueva persona'}</p>
          {error && <p className={`${styles.statusMessage} ${styles.statusError}`}>{error}</p>}
          <div className={styles.formRow}>
            <div className={styles.formGroup}>
              <label>Nombre *</label>
              <input value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })}
                placeholder="Ej: Ana García" style={{ width: 200 }} />
            </div>
            <div className={styles.formGroup}>
              <label>Capacidad semanal (horas)</label>
              <input type="number" min={1} max={60} value={form.weeklyCapacityHours}
                onChange={(e) => setForm({ ...form, weeklyCapacityHours: Number(e.target.value) })}
                style={{ width: 80 }} />
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
      ) : persons.length === 0 ? (
        <div className={styles.empty}>
          No hay personas en el equipo.<br />Agregá la primera para poder asignar tareas.
        </div>
      ) : (
        <table className={styles.table}>
          <thead>
            <tr>
              <th>Nombre</th><th>Capacidad semanal</th><th>ID Chrobi</th><th>Acciones</th>
            </tr>
          </thead>
          <tbody>
            {persons.map((p) => (
              <tr key={p.id}>
                <td><strong>{p.name}</strong></td>
                <td>{p.weeklyCapacityHours}h</td>
                <td>
                  {p.chobiUserId ? (
                    <span className={styles.badgeSuccess}>#{p.chobiUserId}</span>
                  ) : (
                    <div className={styles.linkRow}>
                      <select
                        className={styles.linkSelect}
                        value={selectedUsers[p.id] ?? ''}
                        onChange={(e) => setSelectedUsers((prev) => ({ ...prev, [p.id]: e.target.value }))}
                      >
                        <option value="">Seleccionar usuario…</option>
                        {chobiUsers.map((user) => (
                          <option key={user.id} value={user.id}>
                            {user.description} (ID: {user.id})
                          </option>
                        ))}
                      </select>
                      <button
                        className={styles.btnSecondary}
                        onClick={() => linkPerson(p)}
                        disabled={!selectedUsers[p.id] || linkingPersonId === p.id}
                      >
                        {linkingPersonId === p.id ? 'Vinculando…' : 'Vincular'}
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
