'use client';

import React, { useState } from 'react';
import './AuthPage.css';
import { authService } from '@/services/authService';

export const AuthPage: React.FC = () => {
  const [isLogin, setIsLogin] = useState<boolean>(true);

  // Campos de formulario
  const [nombre, setNombre] = useState<string>('');
  const [email, setEmail] = useState<string>('');
  const [password, setPassword] = useState<string>('');
  const [confirmPassword, setConfirmPassword] = useState<string>('');
  const [aceptarTerminos, setAceptarTerminos] = useState<boolean>(false);

  // Feedback de UI
  const [mensaje, setMensaje] = useState<{ tipo: 'error' | 'exito' | ''; texto: string }>({
    tipo: '',
    texto: '',
  });
  const [cargando, setCargando] = useState<boolean>(false);

  const presionarAccesoRapido = (rol: 'Estudiante' | 'Admin') => {
    if (rol === 'Estudiante') {
      setEmail('estudiante@universidad.edu');
      setPassword('Estudiante123!');
    } else if (rol === 'Admin') {
      setEmail('admin@universidad.edu');
      setPassword('Admin123!');
    }
  };

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
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

    try {
      if (isLogin) {
        await authService.login(email, password);
        setMensaje({ tipo: 'exito', texto: '¡Inicio de sesión exitoso!' });
      } else {
        await authService.register(nombre, email, password);
        setMensaje({ tipo: 'exito', texto: '¡Registro completado con éxito!' });
      }

      setTimeout(() => {
        window.location.href = '/dashboard';
      }, 1200);
    } catch (error) {
      const errMessage = error instanceof Error ? error.message : 'Ocurrió un error inesperado.';
      setMensaje({ tipo: 'error', texto: errMessage });
    } finally {
      setCargando(false);
    }
  };

  return (
    <div className="auth-container">
      <div className="auth-card">
        {/* Encabezado Logo BreakU */}
        <div className="auth-header">
          <div className="logo-icon">
            <span>C</span>
          </div>
          <h1 className="logo-title">
            Break<span className="logo-purple">U</span>
          </h1>
          <p className="logo-subtitle">Tu tiempo libre, bien aprovechado.</p>
        </div>

        {/* Pestañas: Iniciar sesión / Registrarse */}
        <div className="auth-tabs">
          <button
            type="button"
            className={`tab-btn ${isLogin ? 'active' : ''}`}
            onClick={() => {
              setIsLogin(true);
              setMensaje({ tipo: '', texto: '' });
            }}
          >
            Iniciar sesión
          </button>
          <button
            type="button"
            className={`tab-btn ${!isLogin ? 'active' : ''}`}
            onClick={() => {
              setIsLogin(false);
              setMensaje({ tipo: '', texto: '' });
            }}
          >
            Registrarse
          </button>
        </div>

        {/* Mensajes de feedback */}
        {mensaje.texto && (
          <div className={`alert-box ${mensaje.tipo}`}>
            {mensaje.texto}
          </div>
        )}

        {/* Formulario */}
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

        {/* Acceso rápido */}
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

        {/* Footer */}
        <p className="auth-footer-text">
          Acceso restringido a correos institucionales verificados · JWT
        </p>
      </div>
    </div>
  );
};