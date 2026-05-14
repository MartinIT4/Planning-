import { useState } from 'react';
import type { PersonDto, WeeklyPlanDto, TaskAssignmentDto } from '../../types/weeklyPlan';
import type { ProjectDto } from '../../api/weeklyPlanApi';
import { weeklyPlanApi } from '../../api/weeklyPlanApi';
import styles from './ProjectTaskBoard.module.css';

interface Props {
  projects: ProjectDto[];
  persons: PersonDto[];
  plan: WeeklyPlanDto | null;
  onPlanUpdated: (plan: WeeklyPlanDto) => void;
}

function usedHours(plan: WeeklyPlanDto, personId: string) {
  return plan.assignments
    .filter((a) => a.personId === personId)
    .reduce((s, a) => s + a.plannedHours, 0);
}

function CapacityBar({ used, capacity }: { used: number; capacity: number }) {
  const pct = capacity > 0 ? Math.min((used / capacity) * 100, 100) : 0;
  const color = pct >= 100 ? '#ef4444' : pct >= 80 ? '#f59e0b' : '#22c55e';
  return (
    <div className={styles.capBarTrack}>
      <div className={styles.capBarFill} style={{ width: `${pct}%`, background: color }} />
    </div>
  );
}

// ── Form para agregar un integrante a un proyecto ──
interface AddMemberFormProps {
  project: ProjectDto;
  persons: PersonDto[];
  plan: WeeklyPlanDto;
  onAdded: () => void;
  onCancel: () => void;
}

function AddMemberForm({ project, persons, plan, onAdded, onCancel }: AddMemberFormProps) {
  const active = persons.filter((p) => p.isActive);
  const [personId, setPersonId] = useState(active[0]?.id ?? '');
  const [description, setDescription] = useState('');
  const [hours, setHours] = useState(8);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const selected = active.find((p) => p.id === personId);
  const used = selected ? usedHours(plan, selected.id) : 0;
  const avail = selected ? selected.weeklyCapacityHours - used : 0;
  const over = selected && used + hours > selected.weeklyCapacityHours;

  const submit = async () => {
    if (!personId) { setError('Seleccioná una persona'); return; }
    if (!description.trim()) { setError('Ingresá una descripción'); return; }
    setSaving(true);
    setError(null);
    try {
      await weeklyPlanApi.addAssignment(plan.id, {
        personId,
        externalTaskId: `PROJ-${project.id}`,
        taskTitle: project.name,
        plannedHours: hours,
        notes: description.trim(),
      });
      onAdded();
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : 'Error al guardar');
      setSaving(false);
    }
  };

  return (
    <div className={styles.addForm}>
      <div className={styles.addFormRow}>
        <select
          value={personId}
          onChange={(e) => setPersonId(e.target.value)}
          className={styles.formSelect}
        >
          {active.length === 0 && <option value="">Sin equipo cargado</option>}
          {active.map((p) => {
            const a = p.weeklyCapacityHours - usedHours(plan, p.id);
            return (
              <option key={p.id} value={p.id}>
                {p.name} — {a > 0 ? `${a}h disponibles` : 'sin capacidad'}
              </option>
            );
          })}
        </select>
        <div className={styles.hoursGroup}>
          <input
            type="number" min={0.5} step={0.5} value={hours}
            onChange={(e) => setHours(Number(e.target.value))}
            className={styles.hoursInput}
          />
          <span className={styles.hLabel}>h</span>
          {over && (
            <span className={styles.overWarn} title={`Excede capacidad disponible (${avail}h)`}>⚠️</span>
          )}
        </div>
      </div>
      <textarea
        className={styles.descInput}
        rows={2}
        placeholder="Descripción de lo que va a hacer en este proyecto…"
        value={description}
        onChange={(e) => setDescription(e.target.value)}
      />
      {error && <p className={styles.formError}>{error}</p>}
      <div className={styles.addFormActions}>
        <button className={styles.btnPrimary} onClick={submit} disabled={saving}>
          {saving ? 'Guardando…' : '✓ Agregar'}
        </button>
        <button className={styles.btnSecondary} onClick={onCancel} disabled={saving}>
          Cancelar
        </button>
      </div>
    </div>
  );
}

// ── Tarjeta de proyecto ──
interface ProjectCardProps {
  project: ProjectDto;
  assignments: TaskAssignmentDto[];
  persons: PersonDto[];
  plan: WeeklyPlanDto | null;
  onPlanUpdated: (plan: WeeklyPlanDto) => void;
}

function ProjectCard({ project, assignments, persons, plan, onPlanUpdated }: ProjectCardProps) {
  const [showForm, setShowForm] = useState(false);
  const totalHours = assignments.reduce((s, a) => s + a.plannedHours, 0);

  const handleAdded = async () => {
    if (!plan) return;
    setShowForm(false);
    const updated = await weeklyPlanApi.getById(plan.id);
    onPlanUpdated(updated);
  };

  const handleRemove = async (assignId: string) => {
    if (!plan) return;
    await weeklyPlanApi.removeAssignment(plan.id, assignId);
    const updated = await weeklyPlanApi.getById(plan.id);
    onPlanUpdated(updated);
  };

  const handleAddClick = () => {
    if (!plan) { alert('Seleccioná o creá un plan primero'); return; }
    if (persons.filter(p => p.isActive).length === 0) {
      alert('Agregá personas en ⚙️ Configuración → 👥 Equipo');
      return;
    }
    setShowForm((v) => !v);
  };

  return (
    <div className={styles.projectCard}>
      {/* Header con botón + siempre visible */}
      <div className={styles.cardHeader}>
        <div className={styles.cardTitleRow}>
          <span className={styles.cardTitle}>📁 {project.name}</span>
          <div className={styles.cardTitleActions}>
            {totalHours > 0 && (
              <span className={styles.cardTotalHours}>{totalHours}h</span>
            )}
            <button
              className={`${styles.addBtn} ${showForm ? styles.addBtnActive : ''}`}
              onClick={handleAddClick}
              title={showForm ? 'Cancelar' : 'Agregar integrante'}
            >
              {showForm ? '✕' : '+'}
            </button>
          </div>
        </div>
        {project.description && (
          <p className={styles.cardDesc}>{project.description}</p>
        )}
      </div>

      {/* Formulario inline (al tope, antes de la lista) */}
      {showForm && plan && (
        <AddMemberForm
          project={project}
          persons={persons}
          plan={plan}
          onAdded={handleAdded}
          onCancel={() => setShowForm(false)}
        />
      )}

      {/* Integrantes asignados */}
      <div className={styles.memberList}>
        {assignments.length === 0 && !showForm && (
          <p className={styles.noMembers}>Sin integrantes asignados aún</p>
        )}
        {assignments.map((a) => (
          <div key={a.id} className={styles.memberRow}>
            <div className={styles.memberInfo}>
              <span className={styles.memberName}>🧑 {a.personName}</span>
              <span className={styles.memberHours}>{a.plannedHours}h</span>
            </div>
            {a.notes && <p className={styles.memberDesc}>{a.notes}</p>}
            <button
              className={styles.removeBtn}
              onClick={() => handleRemove(a.id)}
              title="Quitar integrante"
            >✕ Quitar</button>
          </div>
        ))}
      </div>
    </div>
  );
}

// ── Board principal ──
export function ProjectTaskBoard({ projects, persons, plan, onPlanUpdated }: Props) {
  const [filter, setFilter] = useState('');

  const filtered = projects.filter((p) =>
    p.name.toLowerCase().includes(filter.toLowerCase()) ||
    (p.description ?? '').toLowerCase().includes(filter.toLowerCase())
  );

  const assignsByProject = (plan?.assignments ?? []).reduce<Record<string, TaskAssignmentDto[]>>(
    (acc, a) => { (acc[a.externalTaskId] ??= []).push(a); return acc; }, {}
  );

  return (
    <div className={styles.board}>
      {/* LEFT */}
      <div className={styles.leftPanel}>
        <div className={styles.panelHeader}>
          <span className={styles.panelTitle}>📁 Proyectos</span>
          <input
            className={styles.filterInput} type="search" placeholder="Filtrar proyectos…"
            value={filter} onChange={(e) => setFilter(e.target.value)}
          />
        </div>

        {projects.length === 0 ? (
          <div className={styles.empty}>
            No hay proyectos activos.<br />
            <small>Cargalos en ⚙️ Configuración → 🗂️ Proyectos</small>
          </div>
        ) : (
          <div className={styles.cardGrid}>
            {filtered.length === 0 && (
              <p style={{ color: '#94a3b8', padding: '0.5rem', fontSize: '0.85rem' }}>
                Sin resultados para "{filter}"
              </p>
            )}
            {filtered.map((project) => (
              <ProjectCard
                key={project.id}
                project={project}
                assignments={assignsByProject[`PROJ-${project.id}`] ?? []}
                persons={persons}
                plan={plan}
                onPlanUpdated={onPlanUpdated}
              />
            ))}
          </div>
        )}
      </div>

      {/* RIGHT */}
      <div className={styles.rightPanel}>
        <div className={styles.panelHeader}>
          <span className={styles.panelTitle}>👥 Equipo</span>
        </div>
        {persons.filter((p) => p.isActive).length === 0 ? (
          <div className={styles.empty}>
            No hay personas en el equipo.<br />
            <small>Cargalas en ⚙️ Configuración → 👥 Equipo</small>
          </div>
        ) : (
          <div className={styles.teamList}>
            {persons.filter((p) => p.isActive).map((person) => {
              const used = plan ? usedHours(plan, person.id) : 0;
              const pct = person.weeklyCapacityHours > 0
                ? Math.min((used / person.weeklyCapacityHours) * 100, 100) : 0;
              const personAssigns = plan?.assignments.filter((a) => a.personId === person.id) ?? [];
              return (
                <div key={person.id} className={styles.personCard}>
                  <div className={styles.personHeader}>
                    <span className={styles.personName}>🧑 {person.name}</span>
                    <span className={styles.personHours}
                      style={{ color: pct >= 100 ? '#ef4444' : pct >= 80 ? '#d97706' : '#16a34a' }}>
                      {used}h / {person.weeklyCapacityHours}h
                    </span>
                  </div>
                  <CapacityBar used={used} capacity={person.weeklyCapacityHours} />
                  {personAssigns.length === 0 ? (
                    <p className={styles.noAssign}>Sin asignaciones</p>
                  ) : (
                    <ul className={styles.personAssignList}>
                      {personAssigns.map((a) => (
                        <li key={a.id} className={styles.personAssignItem}>
                          <span className={styles.paTitle}>{a.taskTitle}</span>
                          <span className={styles.paHours}>{a.plannedHours}h</span>
                        </li>
                      ))}
                    </ul>
                  )}
                </div>
              );
            })}
          </div>
        )}
      </div>
    </div>
  );
}
