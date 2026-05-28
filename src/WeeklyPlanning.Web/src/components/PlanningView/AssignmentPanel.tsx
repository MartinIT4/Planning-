import { useState } from 'react';
import type { PersonDto, WeeklyPlanDto, TaskAssignmentDto } from '../../types/weeklyPlan';
import { weeklyPlanApi } from '../../api/weeklyPlanApi';
import { EditAssignmentModal } from './EditAssignmentModal';
import styles from './AssignmentPanel.module.css';

interface AssignmentPanelProps {
  persons: PersonDto[];
  plan: WeeklyPlanDto | null;
  onAssignmentRemoved: (updatedPlan: WeeklyPlanDto) => void;
}

function getBarClass(planned: number, capacity: number): string {
  if (capacity === 0) return styles.barNormal;
  const pct = planned / capacity;
  if (pct > 1.0) return styles.barOverload;
  if (pct >= 0.8) return styles.barWarning;
  return styles.barNormal;
}

export function AssignmentPanel({ persons, plan, onAssignmentRemoved }: AssignmentPanelProps) {
  const [editingAssignment, setEditingAssignment] = useState<TaskAssignmentDto | null>(null);

  if (!plan) {
    return (
      <div className={styles.panel}>
        <div className={styles.panelHeader}>
          <h2 className={styles.panelTitle}>Equipo</h2>
        </div>
        <p className={styles.emptyMsg}>Selecciona un plan para ver las asignaciones.</p>
      </div>
    );
  }

  const handleRemove = async (assignId: string) => {
    try {
      await weeklyPlanApi.removeAssignment(plan.id, assignId);
      const updated = await weeklyPlanApi.getById(plan.id);
      onAssignmentRemoved(updated);
    } catch (err) {
      console.error('Error removing assignment:', err);
    }
  };

  const handleEditSaved = async () => {
    setEditingAssignment(null);
    const updated = await weeklyPlanApi.getById(plan.id);
    onAssignmentRemoved(updated);
  };

  const activePersns = persons.filter((p) => p.isActive);

  return (
    <>
      <div className={styles.panel}>
        <div className={styles.panelHeader}>
          <h2 className={styles.panelTitle}>Equipo</h2>
        </div>

        {activePersns.length === 0 ? (
          <p className={styles.emptyMsg}>No hay personas activas en el equipo.</p>
        ) : (
          <ul className={styles.personList}>
            {activePersns.map((person) => {
              const personAssignments = plan.assignments.filter((a) => a.personId === person.id);
              const plannedHours = personAssignments.reduce((s, a) => s + a.plannedHours, 0);
              const capacity = person.weeklyCapacityHours;
              const barPct = capacity > 0 ? Math.min((plannedHours / capacity) * 100, 100) : 0;
              const barClass = getBarClass(plannedHours, capacity);

              return (
                <li key={person.id} className={styles.personCard}>
                  <div className={styles.personHeader}>
                    <span className={styles.personName}>🧑 {person.name}</span>
                    <span className={styles.personHours}>
                      {plannedHours}h / {capacity}h
                    </span>
                  </div>
                  <div className={styles.barTrack}>
                    <div
                      className={`${styles.barFill} ${barClass}`}
                      style={{ width: `${barPct}%` }}
                    />
                  </div>

                  {personAssignments.length === 0 ? (
                    <p className={styles.noAssignments}>Sin asignaciones</p>
                  ) : (
                    <ul className={styles.assignmentList}>
                      {personAssignments.map((a) => (
                        <li key={a.id} className={styles.assignmentItem}>
                          <span className={styles.assignTitle}>{a.taskTitle}</span>
                          <span className={styles.assignHours}>{a.plannedHours}h</span>
                          <button
                            className={styles.editBtn}
                            title="Editar asignación"
                            onClick={() => setEditingAssignment(a)}
                          >
                            ✎
                          </button>
                          <button
                            className={styles.removeBtn}
                            title="Eliminar asignación"
                            onClick={() => handleRemove(a.id)}
                          >
                            ✕
                          </button>
                        </li>
                      ))}
                    </ul>
                  )}
                </li>
              );
            })}
          </ul>
        )}
      </div>

      {editingAssignment && (
        <EditAssignmentModal
          assignment={editingAssignment}
          persons={persons}
          plan={plan}
          onSaved={handleEditSaved}
          onClose={() => setEditingAssignment(null)}
        />
      )}
    </>
  );
}
