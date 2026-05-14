import styles from './NavBar.module.css';

export type TabId = 'planning' | 'board' | 'personal' | 'history' | 'config';

interface NavBarProps {
  activeTab: TabId;
  onTabChange: (tab: TabId) => void;
}

export function NavBar({ activeTab, onTabChange }: NavBarProps) {
  const tabs: { id: TabId; label: string }[] = [
    { id: 'config', label: '⚙️ Configuración' },
    { id: 'planning', label: '📅 Planificación' },
    { id: 'board', label: '📊 Tablero Equipo' },
    { id: 'personal', label: '🗓️ Plan Personal' },
    { id: 'history', label: '📋 Historial' },
  ];

  return (
    <nav className={styles.navbar}>
      <div className={styles.brand}>Weekly Planning PM</div>
      <div className={styles.tabs}>
        {tabs.map((t) => (
          <button
            key={t.id}
            className={`${styles.tab} ${activeTab === t.id ? styles.active : ''}`}
            onClick={() => onTabChange(t.id)}
          >
            {t.label}
          </button>
        ))}
      </div>
    </nav>
  );
}
