"use client";
import { useEffect, useState } from "react";
import { Bloque, BloqueInput, crearBloque, editarBloque, eliminarBloque, listarHorario } from "@/lib/horario";

const DIAS = ["", "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado"];
const VACIO: BloqueInput = { materia: "", diaSemana: 1, horaInicio: "", horaFin: "", aula: "" };

export default function HorarioPage() {
  const [bloques, setBloques] = useState<Bloque[]>([]);
  const [form, setForm] = useState<BloqueInput>(VACIO);
  const [editandoId, setEditandoId] = useState<number | null>(null);
  const [error, setError] = useState("");
  const [cargando, setCargando] = useState(true);

  const cargar = async () => {
    try { setBloques(await listarHorario()); }
    catch (e) { setError((e as Error).message); }
    finally { setCargando(false); }
  };
  useEffect(() => { cargar(); }, []);

  // Validación de formato en el cliente (las reglas reales las aplica el backend).
  const validar = (): string => {
    if (!form.materia.trim() || !form.horaInicio || !form.horaFin) return "Por favor, complete todos los campos.";
    if (form.horaFin <= form.horaInicio) return "La hora de fin debe ser posterior a la de inicio.";
    return "";
  };

  const guardar = async (e: React.FormEvent) => {
    e.preventDefault();
    const msg = validar();
    if (msg) return setError(msg);
    try {
      const datos = { ...form, aula: form.aula?.trim() || null };
      if (editandoId) await editarBloque(editandoId, datos);
      else await crearBloque(datos);
      setForm(VACIO); setEditandoId(null); setError("");
      await cargar();
    } catch (e) { setError((e as Error).message); } // incluye el mensaje de solapamiento
  };

  const empezarEdicion = (b: Bloque) => {
    setEditandoId(b.id);
    setForm({ materia: b.materia, diaSemana: b.diaSemana, horaInicio: b.horaInicio, horaFin: b.horaFin, aula: b.aula ?? "" });
    setError("");
  };

  const borrar = async (id: number) => {
    if (!confirm("¿Eliminar este bloque?")) return;
    try { await eliminarBloque(id); await cargar(); } catch (e) { setError((e as Error).message); }
  };

  return (
    <main style={{ maxWidth: 720, margin: "0 auto", padding: 16 }}>
      <h1>Mi horario académico</h1>

      <form onSubmit={guardar} style={{ display: "grid", gap: 8, marginBottom: 16 }}>
        <input placeholder="Materia" value={form.materia} onChange={(e) => setForm({ ...form, materia: e.target.value })} />
        <select value={form.diaSemana} onChange={(e) => setForm({ ...form, diaSemana: Number(e.target.value) })}>
          {[1, 2, 3, 4, 5, 6].map((d) => <option key={d} value={d}>{DIAS[d]}</option>)}
        </select>
        <div style={{ display: "flex", gap: 8 }}>
          <input type="time" value={form.horaInicio} onChange={(e) => setForm({ ...form, horaInicio: e.target.value })} />
          <input type="time" value={form.horaFin} onChange={(e) => setForm({ ...form, horaFin: e.target.value })} />
        </div>
        <input placeholder="Aula (opcional)" maxLength={30} value={form.aula ?? ""} onChange={(e) => setForm({ ...form, aula: e.target.value })} />
        {error && <p role="alert" style={{ color: "crimson" }}>{error}</p>}
        <div style={{ display: "flex", gap: 8 }}>
          <button type="submit">{editandoId ? "Guardar cambios" : "Agregar bloque"}</button>
          {editandoId && <button type="button" onClick={() => { setEditandoId(null); setForm(VACIO); setError(""); }}>Cancelar</button>}
        </div>
      </form>

      {cargando ? <p>Cargando…</p> : bloques.length === 0 ? <p>Aún no registraste clases.</p> : (
        <ul style={{ listStyle: "none", padding: 0, display: "grid", gap: 8 }}>
          {bloques.map((b) => (
            <li key={b.id} style={{ border: "1px solid #ccc", borderRadius: 8, padding: 8, display: "flex", justifyContent: "space-between", gap: 8 }}>
              <span><strong>{b.materia}</strong><br />{DIAS[b.diaSemana]} · {b.horaInicio}–{b.horaFin}{b.aula ? ` · ${b.aula}` : ""}</span>
              <span style={{ display: "flex", gap: 4, alignItems: "center" }}>
                <button onClick={() => empezarEdicion(b)}>Editar</button>
                <button onClick={() => borrar(b.id)}>Eliminar</button>
              </span>
            </li>
          ))}
        </ul>
      )}
    </main>
  );
}
