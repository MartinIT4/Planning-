import styles from './NavBar.module.css';

export type TabId = 'planning' | 'board' | 'personal' | 'history' | 'config';

interface NavBarProps {
  activeTab: TabId;
  onTabChange: (tab: TabId) => void;
  userName: string;
  onLogout: () => void;
}

export function NavBar({ activeTab, onTabChange, userName, onLogout }: NavBarProps) {
  const tabs: { id: TabId; label: string }[] = [
    { id: 'config', label: '⚙️ Configuración' },
    { id: 'planning', label: '📅 Planificación' },
    { id: 'board', label: '📊 Tablero Equipo' },
    { id: 'personal', label: '🗓️ Plan Personal' },
    { id: 'history', label: '📋 Historial' },
  ];

  return (
    <nav className={styles.navbar}>
      <div className={styles.brand}>
        <img src="/logo-it4.png" alt="IT4W" className={styles.brandLogo} />
        Weekly Planning PM
      </div>
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
      <div className={styles.spacer} />
      <div className={styles.actions}>
        <span className={styles.userName}>{userName}</span>
        <button type="button" className={styles.logoutButton} onClick={onLogout}>
          Salir
        </button>
      </div>
    </nav>
  );
}
