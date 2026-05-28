import { useState, type FormEvent } from 'react';
import { useAuth } from '../../auth/AuthContext';
import styles from './LoginPage.module.css';

export function LoginPage() {
  const { login } = useAuth();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setError('');
    setLoading(true);

    try {
      await login(email, password);
    } catch {
      setError('No pudimos iniciar sesión. Verificá tu email y contraseña de Chrobi.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className={styles.page}>
      <form className={styles.card} onSubmit={handleSubmit}>
        <div className={styles.header}>
          <img src="/logo-it4.png" alt="IT4W" className={styles.logo} />
          <h1>Weekly Planning PM</h1>
          <p>Ingresá con tus credenciales de Chrobi.</p>
        </div>

        <label className={styles.field}>
          <span>Email</span>
          <input
            type="email"
            autoComplete="email"
            value={email}
            onChange={(event) => setEmail(event.target.value)}
            placeholder="tu@email.com"
            required
          />
        </label>

        <label className={styles.field}>
          <span>Contraseña</span>
          <input
            type="password"
            autoComplete="current-password"
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            placeholder="••••••••"
            required
          />
        </label>

        {error && <div className={styles.error}>{error}</div>}

        <button className={styles.submit} type="submit" disabled={loading}>
          {loading ? 'Ingresando…' : 'Ingresar'}
        </button>
      </form>
    </div>
  );
}
