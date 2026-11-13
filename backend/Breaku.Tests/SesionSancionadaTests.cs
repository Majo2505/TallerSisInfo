using System.Security.Claims;
using System.Text.Json;
using Breaku.Application.Admin;
using Breaku.Application.Sanciones;
using Breaku.Domain;
using Breaku.Domain.Rules;
using Breaku.Presentation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Breaku.Tests;

public class SesionSancionadaTests
{
    private static readonly DateTime Fin = new(2026, 11, 20, 15, 0, 0, DateTimeKind.Utc);

    private sealed class ValidadorFalso(ResultadoSesion resultado) : IValidadorSesion
    {
        public int? UsuarioConsultado { get; private set; }

        public Task<ResultadoSesion> ValidarAsync(int usuarioId, CancellationToken ct = default)
        {
            UsuarioConsultado = usuarioId;
            return Task.FromResult(resultado);
        }

        public void Invalidar(int usuarioId) { }
    }

    private static (DefaultHttpContext Http, AuthenticationScheme Esquema, JwtBearerOptions Opciones) Contexto(ValidadorFalso validador)
    {
        var servicios = new ServiceCollection().AddSingleton<IValidadorSesion>(validador).BuildServiceProvider();
        var http = new DefaultHttpContext { RequestServices = servicios };
        http.Response.Body = new MemoryStream();
        return (http, new AuthenticationScheme("Bearer", null, typeof(JwtBearerHandler)), new JwtBearerOptions());
    }

    private static TokenValidatedContext TokenDe(DefaultHttpContext http, AuthenticationScheme esquema, JwtBearerOptions opciones, int usuarioId)
        => new(http, esquema, opciones)
        {
            Principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", usuarioId.ToString())], "Bearer"))
        };

    private static async Task<(int Estado, JsonElement Cuerpo)> Desafio(DefaultHttpContext http, AuthenticationScheme esquema, JwtBearerOptions opciones)
    {
        var desafio = new JwtBearerChallengeContext(http, esquema, opciones, new AuthenticationProperties());
        await SesionSancionada.ResponderDesafioAsync(desafio);
        Assert.True(desafio.Handled);
        http.Response.Body.Position = 0;
        return (http.Response.StatusCode, JsonDocument.Parse(http.Response.Body).RootElement.Clone());
    }

    [Fact]
    public async Task Una_cuenta_suspendida_responde_401_con_CUENTA_SANCIONADA_y_el_mensaje_con_motivo_y_fecha()
    {
        var validador = new ValidadorFalso(new ResultadoSesion(false, new ReglasSancion.BloqueoCuenta(Sancion.TipoSuspension, "Acoso", Fin)));
        var (http, esquema, opciones) = Contexto(validador);
        var token = TokenDe(http, esquema, opciones, 10);

        await SesionSancionada.ValidarTokenAsync(token);
        var (estado, cuerpo) = await Desafio(http, esquema, opciones);

        Assert.Equal(10, validador.UsuarioConsultado);
        Assert.NotNull(token.Result?.Failure);
        Assert.Equal(401, estado);
        Assert.Equal("CUENTA_SANCIONADA", cuerpo.GetProperty("codigo").GetString());
        var mensaje = cuerpo.GetProperty("mensaje").GetString();
        Assert.Equal("Tu cuenta está suspendida hasta el 20/11/2026 11:00. Motivo: Acoso", mensaje); // igual que el login
    }

    [Fact]
    public async Task Una_cuenta_dada_de_baja_tambien_responde_CUENTA_SANCIONADA_con_su_motivo()
    {
        var (http, esquema, opciones) = Contexto(new ValidadorFalso(new ResultadoSesion(false, new ReglasSancion.BloqueoCuenta(Sancion.TipoBaja, "Fraude", null))));
        await SesionSancionada.ValidarTokenAsync(TokenDe(http, esquema, opciones, 10));

        var (estado, cuerpo) = await Desafio(http, esquema, opciones);

        Assert.Equal(401, estado);
        Assert.Equal("CUENTA_SANCIONADA", cuerpo.GetProperty("codigo").GetString());
        Assert.Equal("Tu cuenta fue dada de baja. Motivo: Fraude", cuerpo.GetProperty("mensaje").GetString());
    }

    [Fact]
    public async Task Un_usuario_sin_sancion_pasa_sin_rechazo()
    {
        var (http, esquema, opciones) = Contexto(new ValidadorFalso(new ResultadoSesion(true)));
        var token = TokenDe(http, esquema, opciones, 10);

        await SesionSancionada.ValidarTokenAsync(token);

        Assert.Null(token.Result?.Failure);
        Assert.False(http.Items.ContainsKey(SesionSancionada.ClaveItem));
    }

    [Fact]
    public async Task Un_usuario_que_ya_no_existe_se_rechaza_sin_codigo_de_sancion()
    {
        var (http, esquema, opciones) = Contexto(new ValidadorFalso(new ResultadoSesion(false)));
        var token = TokenDe(http, esquema, opciones, 10);

        await SesionSancionada.ValidarTokenAsync(token);
        var desafio = new JwtBearerChallengeContext(http, esquema, opciones, new AuthenticationProperties());
        await SesionSancionada.ResponderDesafioAsync(desafio);

        Assert.NotNull(token.Result?.Failure);
        Assert.False(desafio.Handled); // 401 normal de JwtBearer, sin cuerpo con código
    }

    [Fact]
    public async Task Un_token_vencido_o_invalido_no_lleva_el_codigo_de_sancion()
    {
        // Un token vencido nunca llega a OnTokenValidated: el desafío sale sin la marca de sanción y se deja al 401 por defecto.
        var (http, esquema, opciones) = Contexto(new ValidadorFalso(new ResultadoSesion(true)));
        var desafio = new JwtBearerChallengeContext(http, esquema, opciones, new AuthenticationProperties());

        await SesionSancionada.ResponderDesafioAsync(desafio);

        Assert.False(desafio.Handled);
        Assert.Equal(0, http.Response.Body.Length);
    }

    [Fact]
    public async Task Un_token_sin_id_de_usuario_se_rechaza_sin_consultar()
    {
        var validador = new ValidadorFalso(new ResultadoSesion(true));
        var (http, esquema, opciones) = Contexto(validador);
        var token = new TokenValidatedContext(http, esquema, opciones) { Principal = new ClaimsPrincipal(new ClaimsIdentity("Bearer")) };

        await SesionSancionada.ValidarTokenAsync(token);

        Assert.NotNull(token.Result?.Failure);
        Assert.Null(validador.UsuarioConsultado);
    }

    [Fact]
    public async Task El_validador_devuelve_el_bloqueo_y_el_mensaje_para_el_401_y_lo_guarda_en_el_cache()
    {
        var lecturas = 0;
        var validador = new ValidadorSesion((id, ahora, ct) =>
        {
            lecturas++;
            return Task.FromResult<EstadoSesion?>(new EstadoSesion(true, new ReglasSancion.BloqueoCuenta(Sancion.TipoSuspension, "Acoso", Fin)));
        }, TimeProvider.System);

        var primero = await validador.ValidarAsync(10);
        var segundo = await validador.ValidarAsync(10);

        Assert.False(primero.Permitido);
        Assert.Equal("Tu cuenta está suspendida hasta el 20/11/2026 11:00. Motivo: Acoso", primero.Mensaje);
        Assert.Equal(primero, segundo);
        Assert.Equal(1, lecturas);
    }

    [Fact]
    public void El_reporte_del_admin_sigue_construyendose_sin_los_campos_nuevos()
    {
        var antiguo = new ReporteAdminDto(1, "LUGAR_INCORRECTO", "texto", "PENDIENTE", "Sala", "Ana", DateTime.UtcNow);
        var nuevo = antiguo with { UsuarioReportadoId = 7, UsuarioReportadoNombre = "Beto" };

        Assert.Null(antiguo.UsuarioReportadoId);
        Assert.Null(antiguo.UsuarioReportadoNombre);
        Assert.Equal((7, "Beto"), (nuevo.UsuarioReportadoId, nuevo.UsuarioReportadoNombre));
    }
}
