import { useState } from 'react';
import type { BacklogItemDto } from '../../types/backlog';
import { backlogApi } from '../../api/backlogApi';
import styles from './BacklogPanel.module.css';

interface BacklogPanelProps {
  items: BacklogItemDto[];
  onAssign: (item: BacklogItemDto) => void;
  onReload: () => void;
}

const STATUS_LABELS: Record<string, string> = {
  todo: 'Pendiente',
  in_progress: 'En curso',
  done: 'Hecho',
};

export function BacklogPanel({ items, onAssign, onReload }: BacklogPanelProps) {
  const [filter, setFilter] = useState('');
  const [seeding, setSeeding] = useState(false);
  const [collapsed, setCollapsed] = useState<Record<string, boolean>>({});

  const filtered = items.filter(
    (i) =>
      i.title.toLowerCase().includes(filter.toLowerCase()) ||
      (i.projectName ?? '').toLowerCase().includes(filter.toLowerCase())
  );

  // Group by project
  const byProject = filtered.reduce<Record<string, BacklogItemDto[]>>((acc, item) => {
    const proj = item.projectName ?? '(Sin proyecto)';
    (acc[proj] ??= []).push(item);
    return acc;
  }, {});
  const projectNames = Object.keys(byProject).sort();

  const toggleCollapse = (proj: string) =>
    setCollapsed((prev) => ({ ...prev, [proj]: !prev[proj] }));

  const handleSeed = async () => {
    setSeeding(true);
    try {
      await backlogApi.seed();
      onReload();
    } finally {
      setSeeding(false);
    }
  };

  return (
    <div className={styles.panel}>
      <div className={styles.panelHeader}>
        <h2 className={styles.panelTitle}>Backlog — Proyectos y Tareas</h2>
        <input
          className={styles.filterInput}
          type="search"
          placeholder="Filtrar por tarea o proyecto…"
          value={filter}
          onChange={(e) => setFilter(e.target.value)}
        />
      </div>

      {items.length === 0 ? (
        <div className={styles.empty}>
          <p>Backlog vacío</p>
          <button className={styles.seedBtn} onClick={handleSeed} disabled={seeding}>
            {seeding ? 'Cargando…' : 'Cargar datos de prueba'}
          </button>
        </div>
      ) : (
        <div className={styles.list}>
          {filtered.length === 0 && (
            <p className={styles.noResults}>Sin resultados para "{filter}"</p>
          )}
          {projectNames.map((proj) => {
            const projItems = byProject[proj];
            const isCollapsed = collapsed[proj];
            return (
              <div key={proj} className={styles.projectGroup}>
                <button
                  className={styles.projectHeader}
                  onClick={() => toggleCollapse(proj)}
                >
                  <span className={styles.projectToggle}>{isCollapsed ? '▶' : '▼'}</span>
                  <span className={styles.projectName}>📁 {proj}</span>
                  <span className={styles.projectCount}>{projItems.length} tarea{projItems.length !== 1 ? 's' : ''}</span>
                </button>
                {!isCollapsed && (
                  <ul className={styles.taskList}>
                    {projItems.map((item) => (
                      <li key={item.id} className={styles.item}>
                        <div className={styles.itemHeader}>
                          <span className={styles.itemTitle}>{item.title}</span>
                        </div>
                        <div className={styles.itemMeta}>
                          <span className={styles.metaHours}>Est: {item.estimatedHours}h</span>
                          {item.remainingHours !== item.estimatedHours && (
                            <span className={styles.metaRemaining}>Restante: {item.remainingHours}h</span>
                          )}
                          <span className={`${styles.metaStatus} ${styles[`status_${item.status}`]}`}>
                            {STATUS_LABELS[item.status] ?? item.status}
                          </span>
                          {item.assigneeName && (
                            <span className={styles.metaAssignee}>👤 {item.assigneeName}</span>
                          )}
                        </div>
                        <button className={styles.assignBtn} onClick={() => onAssign(item)}>
                          Asignar →
                        </button>
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
  );
}
