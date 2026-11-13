using Breaku.Domain.Rules;

namespace Breaku.Application.Common.Interfaces;

/// <summary>HU28: la cuenta está suspendida o dada de baja. Lleva el motivo y, si lo hay, el fin de la suspensión.</summary>
public sealed class CuentaBloqueadaException : Exception
{
    public ReglasSancion.BloqueoCuenta Bloqueo { get; }

    public CuentaBloqueadaException(ReglasSancion.BloqueoCuenta bloqueo) : base(ReglasSancion.MensajeBloqueo(bloqueo)) => Bloqueo = bloqueo;
}
