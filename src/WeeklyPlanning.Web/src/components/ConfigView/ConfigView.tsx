import { useState } from 'react';
import { TeamManager } from './TeamManager';
import { BacklogManager } from './BacklogManager';
import { ProjectManager } from './ProjectManager';
import styles from './ConfigView.module.css';

type ConfigTab = 'team' | 'projects' | 'backlog';

export function ConfigView() {
  const [tab, setTab] = useState<ConfigTab>('team');

  return (
    <div className={styles.view}>
      <div className={styles.tabs}>
        <button className={`${styles.tab} ${tab === 'team' ? styles.active : ''}`}
          onClick={() => setTab('team')}>
          👥 Equipo
        </button>
        <button className={`${styles.tab} ${tab === 'projects' ? styles.active : ''}`}
          onClick={() => setTab('projects')}>
          🗂️ Proyectos
        </button>
        <button className={`${styles.tab} ${tab === 'backlog' ? styles.active : ''}`}
          onClick={() => setTab('backlog')}>
          📋 Proyectos y Tareas
        </button>
      </div>
      <div className={styles.content}>
        {tab === 'team' && <TeamManager />}
        {tab === 'projects' && <ProjectManager />}
        {tab === 'backlog' && <BacklogManager />}
      </div>
    </div>
  );
}
