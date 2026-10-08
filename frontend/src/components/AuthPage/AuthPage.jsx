import React, { useState } from 'react';
import './AuthPage.css';

const API_URL = 'http://localhost:5000/api/auth';

export const AuthPage = () => {
  const [isLogin, setIsLogin] = useState(true);
  
  const [nombre, setNombre] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [aceptarTerminos, setAceptarTerminos] = useState(false);


  const [mensaje, setMensaje] = useState({ tipo: '', texto: '' });
  const [cargando, setCargando] = useState(false);

  const presionarAccesoRapido = (rol) => {
    if (rol === 'Estudiante') {
      setEmail('estudiante@universidad.edu');
      setPassword('Estudiante123!');
    } else if (rol === 'Admin') {
      setEmail('admin@universidad.edu');
      setPassword('Admin123!');
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setMensaje({ tipo: '', texto: '' });

    if (!isLogin) {
      if (password !== confirmPassword) {
        setMensaje({ tipo: 'error', texto: 'Las contraseñas no coinciden.' });
        return;
      }
      if (!aceptarTerminos) {
        setMensaje({ tipo: 'error', texto: 'Debes aceptar los términos de uso.' });
        return;
      }
    }

    setCargando(true);
    const endpoint = isLogin ? `${API_URL}/login` : `${API_URL}/register`;
    const body = isLogin 
      ? { email, password }
      : { nombre, email, password };

    try {
      const response = await fetch(endpoint, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body),
      });

      const data = await response.json();

      if (!response.ok) {
        throw new Error(data.message || 'Ocurrió un error en la solicitud.');
      }

      // Guardar JWT y datos de sesión en localStorage
      localStorage.setItem('jwtToken', data.token);
      localStorage.setItem('userData', JSON.stringify(data));

      setMensaje({
        tipo: 'exito',
        texto: isLogin ? '¡Inicio de sesión exitoso!' : '¡Registro completado con éxito!',
      });

      // Redireccionar al dashboard o vista principal
      setTimeout(() => {
        window.location.href = '/dashboard';
      }, 1200);

    } catch (error) {
      setMensaje({ tipo: 'error', texto: error.message });
    } finally {
      setCargando(false);
    }
  };

  return (
    <div className="auth-container">
      <div className="auth-card">
        {/* Encabezado con Logo BreakU */}
        <div className="auth-header">
          <div className="logo-icon">
            <span>C</span>
          </div>
          <h1 className="logo-title">
            Break<span className="logo-purple">U</span>
          </h1>
          <p className="logo-subtitle">Tu tiempo libre, bien aprovechado.</p>
        </div>

        {/* Selector de pestañas: Iniciar sesión / Registrarse */}
        <div className="auth-tabs">
          <button
            type="button"
            className={`tab-btn ${isLogin ? 'active' : ''}`}
            onClick={() => { setIsLogin(true); setMensaje({ tipo: '', texto: '' }); }}
          >
            Iniciar sesión
          </button>
          <button
            type="button"
            className={`tab-btn ${!isLogin ? 'active' : ''}`}
            onClick={() => { setIsLogin(false); setMensaje({ tipo: '', texto: '' }); }}
          >
            Registrarse
          </button>
        </div>

        {/* Alertas de error / éxito */}
        {mensaje.texto && (
          <div className={`alert-box ${mensaje.tipo}`}>
            {mensaje.texto}
          </div>
        )}

        {/* Formulario principal */}
        <form onSubmit={handleSubmit} className="auth-form">
          {!isLogin && (
            <div className="form-group">
              <label>Nombre completo</label>
              <input
                type="text"
                placeholder="María Fernández"
                value={nombre}
                onChange={(e) => setNombre(e.target.value)}
                required
              />
            </div>
          )}

          <div className="form-group">
            <label>Correo institucional</label>
            <input
              type="email"
              placeholder="usuario@universidad.edu"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
            />
          </div>

          <div className="form-group">
            <label>Contraseña</label>
            <input
              type="password"
              placeholder="••••••••"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
            />
          </div>

          {!isLogin ? (
            <>
              <div className="form-group">
                <label>Confirmar contraseña</label>
                <input
                  type="password"
                  placeholder="••••••••"
                  value={confirmPassword}
                  onChange={(e) => setConfirmPassword(e.target.value)}
                  required
                />
              </div>

              <div className="terms-container">
                <input
                  type="checkbox"
                  id="terms"
                  checked={aceptarTerminos}
                  onChange={(e) => setAceptarTerminos(e.target.checked)}
                />
                <label htmlFor="terms">
                  Acepto los <span className="link-purple">términos de uso</span> y entiendo que BREAKU solo utiliza mi ubicación para generar recomendaciones activas, nunca la almacena ni la comparte.
                </label>
              </div>
            </>
          ) : (
            <div className="forgot-password-container">
              <a href="#forgot" className="link-purple">¿Olvidaste tu contraseña?</a>
            </div>
          )}

          <button type="submit" className="btn-primary" disabled={cargando}>
            {cargando ? 'Procesando...' : isLogin ? 'Iniciar sesión' : 'Crear cuenta'}
          </button>
        </form>

        {/* Sección de Acceso rápido */}
        <div className="quick-access-section">
          <div className="divider">
            <span>Acceso rápido</span>
          </div>

          <div className="quick-buttons">
            <button
              type="button"
              className="quick-btn student-btn"
              onClick={() => presionarAccesoRapido('Estudiante')}
            >
              🎓 Estudiante
            </button>
            <button
              type="button"
              className="quick-btn admin-btn"
              onClick={() => presionarAccesoRapido('Admin')}
            >
              ⚙️ Admin
            </button>
          </div>
        </div>

        {/* Pie de página */}
        <p className="auth-footer-text">
          Acceso restringido a correos institucionales verificados · JWT
        </p>
      </div>
    </div>
  );
};