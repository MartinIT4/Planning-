import { useState, useEffect, useCallback } from 'react';
import { NavBar, type TabId } from './components/Layout/NavBar';
import { ConfigView } from './components/ConfigView/ConfigView';
import { PlanningView } from './components/PlanningView/PlanningView';
import { PersonalWeeklyPlan } from './components/PersonalWeeklyPlan';
import { TeamWeeklyBoard } from './components/TeamWeeklyBoard/TeamWeeklyBoard';
import { PlanHistory } from './components/PlanHistory/PlanHistory';
import { personsApi } from './api/weeklyPlanApi';
import type { PersonDto } from './types/weeklyPlan';

function currentMonday(): string {
  const d = new Date();
  // Use local date parts to avoid UTC offset issues (e.g. Argentina UTC-3 at 21:00 = next UTC day)
  const year = d.getFullYear();
  const month = d.getMonth();
  const date = d.getDate();
  const day = d.getDay(); // 0=Sun…6=Sat, local
  const diff = day === 0 ? -6 : 1 - day;
  const monday = new Date(year, month, date + diff);
  const yyyy = monday.getFullYear();
  const mm = String(monday.getMonth() + 1).padStart(2, '0');
  const dd = String(monday.getDate()).padStart(2, '0');
  return `${yyyy}-${mm}-${dd}`;
}

function addWeeks(dateStr: string, weeks: number): string {
  const d = new Date(dateStr + 'T00:00:00Z');
  d.setUTCDate(d.getUTCDate() + weeks * 7);
  return d.toISOString().slice(0, 10);
}

function formatWeekRange(mondayStr: string): string {
  const start = new Date(mondayStr + 'T00:00:00Z');
  const end = new Date(mondayStr + 'T00:00:00Z');
  end.setUTCDate(end.getUTCDate() + 4);
  const fmt = (d: Date) =>
    d.toLocaleDateString('es-AR', { day: 'numeric', month: 'long', timeZone: 'UTC' });
  const fmtYear = (d: Date) =>
    d.toLocaleDateString('es-AR', { day: 'numeric', month: 'long', year: 'numeric', timeZone: 'UTC' });
  return `${fmt(start)} – ${fmtYear(end)}`;
}

export default function App() {
  const [tab, setTab] = useState<TabId>('config');
  const [boardWeek, setBoardWeek] = useState<string>(currentMonday());
  const [personalWeek, setPersonalWeek] = useState<string>(currentMonday());
  const [persons, setPersons] = useState<PersonDto[]>([]);
  const [selectedPersonId, setSelectedPersonId] = useState<string>('');

  // Carga equipo para el selector de persona
  const loadPersons = useCallback(async () => {
    try {
      const list = await personsApi.getAll();
      setPersons(list);
      if (list.length > 0 && !selectedPersonId) {
        setSelectedPersonId(list[0].id);
      }
    } catch { /* silencioso */ }
  }, []); // eslint-disable-line react-hooks/exhaustive-deps

  useEffect(() => {
    if (tab === 'personal') loadPersons();
  }, [tab, loadPersons]);

  const currentWeek = currentMonday();
  const isCurrentPersonalWeek = personalWeek === currentWeek;
  const isCurrentBoardWeek = boardWeek === currentWeek;

  return (
    <div style={{ display: 'flex', flexDirection: 'column', height: '100vh', fontFamily: 'system-ui, sans-serif', background: '#f8fafc' }}>
      <NavBar activeTab={tab} onTabChange={setTab} />

      <div style={{ flex: 1, overflow: 'hidden', display: 'flex', flexDirection: 'column' }}>
        {tab === 'config' && <ConfigView />}

        {tab === 'planning' && <PlanningView />}

        {tab === 'board' && (
          <div style={{ flex: 1, overflow: 'auto', display: 'flex', flexDirection: 'column' }}>
            <div style={{
              display: 'flex', alignItems: 'center', gap: '0.75rem', flexWrap: 'wrap',
              padding: '0.75rem 1.5rem', background: '#fff',
              borderBottom: '1px solid #e2e8f0', flexShrink: 0,
            }}>
              <button
                onClick={() => setBoardWeek((w) => addWeeks(w, -1))}
                style={{ border: '1px solid #e2e8f0', background: '#f8fafc', borderRadius: 6, padding: '0.3rem 0.7rem', cursor: 'pointer', fontSize: '1rem' }}
                title="Semana anterior"
              >←</button>

              <span style={{ fontWeight: 500, color: '#1e293b', minWidth: 240, textAlign: 'center' }}>
                {formatWeekRange(boardWeek)}
              </span>

              <button
                onClick={() => setBoardWeek((w) => addWeeks(w, 1))}
                style={{ border: '1px solid #e2e8f0', background: '#f8fafc', borderRadius: 6, padding: '0.3rem 0.7rem', cursor: 'pointer', fontSize: '1rem' }}
                title="Semana siguiente"
              >→</button>

              {!isCurrentBoardWeek && (
                <button
                  onClick={() => setBoardWeek(currentWeek)}
                  style={{ border: '1px solid #3b82f6', color: '#3b82f6', background: '#fff', borderRadius: 6, padding: '0.3rem 0.75rem', cursor: 'pointer', fontSize: '0.85rem', fontWeight: 500 }}
                >
                  Hoy
                </button>
              )}
            </div>

            <div style={{ flex: 1, overflow: 'hidden' }}>
              <TeamWeeklyBoard weekStartDate={boardWeek} />
            </div>
          </div>
        )}

        {tab === 'history' && (
          <div style={{ flex: 1, overflow: 'auto' }}>
            <PlanHistory currentBoardWeek={boardWeek} />
          </div>
        )}

        {tab === 'personal' && (
          <div style={{ flex: 1, overflow: 'auto', display: 'flex', flexDirection: 'column' }}>
            {/* Barra de navegación: persona + semana */}
            <div style={{
              display: 'flex', alignItems: 'center', gap: '0.75rem', flexWrap: 'wrap',
              padding: '0.75rem 1.5rem', background: '#fff',
              borderBottom: '1px solid #e2e8f0', flexShrink: 0,
            }}>
              {/* Selector de persona */}
              <select
                value={selectedPersonId}
                onChange={(e) => setSelectedPersonId(e.target.value)}
                style={{
                  border: '1px solid #e2e8f0', borderRadius: 6, padding: '0.35rem 0.6rem',
                  fontSize: '0.9rem', fontWeight: 600, color: '#1e293b', background: '#fff',
                  cursor: 'pointer',
                }}
              >
                {persons.length === 0
                  ? <option value="">Cargando equipo…</option>
                  : persons.map((p) => (
                    <option key={p.id} value={p.id}>{p.name}</option>
                  ))
                }
              </select>

              <span style={{ color: '#cbd5e1' }}>|</span>

              {/* Navegación de semana */}
              <button
                onClick={() => setPersonalWeek(w => addWeeks(w, -1))}
                style={{ border: '1px solid #e2e8f0', background: '#f8fafc', borderRadius: 6, padding: '0.3rem 0.7rem', cursor: 'pointer', fontSize: '1rem' }}
                title="Semana anterior"
              >←</button>

              <span style={{ fontWeight: 500, color: '#1e293b', minWidth: 240, textAlign: 'center' }}>
                {formatWeekRange(personalWeek)}
              </span>

              <button
                onClick={() => setPersonalWeek(w => addWeeks(w, 1))}
                style={{ border: '1px solid #e2e8f0', background: '#f8fafc', borderRadius: 6, padding: '0.3rem 0.7rem', cursor: 'pointer', fontSize: '1rem' }}
                title="Semana siguiente"
              >→</button>

              {!isCurrentPersonalWeek && (
                <button
                  onClick={() => setPersonalWeek(currentWeek)}
                  style={{ border: '1px solid #3b82f6', color: '#3b82f6', background: '#fff', borderRadius: 6, padding: '0.3rem 0.75rem', cursor: 'pointer', fontSize: '0.85rem', fontWeight: 500 }}
                >
                  Hoy
                </button>
              )}
            </div>

            <div style={{ flex: 1, overflow: 'auto' }}>
              {selectedPersonId
                ? <PersonalWeeklyPlan ownerId={selectedPersonId} weekStartDate={personalWeek} />
                : <p style={{ color: '#94a3b8', padding: '2rem' }}>Seleccioná una persona del equipo.</p>
              }
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
