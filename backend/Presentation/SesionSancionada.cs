using Breaku.Api.Hubs;
using Breaku.Application.DTOs;
using Breaku.Application.Sanciones;
using Breaku.Domain.Rules;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;

namespace Breaku.Presentation;

/// <summary>
/// HU28: un token válido de una cuenta suspendida o dada de baja se rechaza con 401 y un cuerpo
/// <c>{ "codigo": "CUENTA_SANCIONADA", "mensaje": "…" }</c> (motivo y fecha, igual que el login). Un token vencido o inválido
/// sigue siendo un 401 sin ese código: así el frontend distingue los dos casos. También corre con el token que llega por
/// query string al hub de tiempo real (<c>/hubs</c>).
/// </summary>
public static class SesionSancionada
{
    public const string ClaveItem = "breaku.cuenta_sancionada";

    /// <summary>Se engancha a <c>JwtBearerEvents.OnTokenValidated</c>.</summary>
    public static async Task ValidarTokenAsync(TokenValidatedContext context)
    {
        var usuarioId = UsuarioIdProvider.Leer(context.Principal);
        var validador = context.HttpContext.RequestServices.GetRequiredService<IValidadorSesion>();
        var resultado = usuarioId is null ? new ResultadoSesion(false) : await validador.ValidarAsync(usuarioId.Value, context.HttpContext.RequestAborted);
        if (resultado.Permitido) return;

        // El mensaje queda para el desafío (401): aquí solo se rechaza el token.
        if (resultado.Mensaje is { } mensaje) context.HttpContext.Items[ClaveItem] = mensaje;
        context.Fail("La cuenta no puede usar la plataforma.");
    }

    /// <summary>Se engancha a <c>JwtBearerEvents.OnChallenge</c>: escribe el cuerpo solo cuando el rechazo fue por una sanción.</summary>
    public static async Task ResponderDesafioAsync(JwtBearerChallengeContext context)
    {
        if (context.HttpContext.Items[ClaveItem] is not string mensaje) return;

        context.HandleResponse();
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.Headers.WWWAuthenticate = "Bearer error=\"invalid_token\"";
        await context.Response.WriteAsJsonAsync(new ErrorNegocioDto(ReglasSancion.CodigoCuentaSancionada, mensaje), context.HttpContext.RequestAborted);
    }
}
