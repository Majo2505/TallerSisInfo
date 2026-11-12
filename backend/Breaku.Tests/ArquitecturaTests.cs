using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Breaku.Tests;

/// <summary>
/// HU20: la arquitectura modular se cumple porque una prueba falla si alguien la rompe. Las capas son carpetas de un
/// mismo proyecto, así que el compilador no lo impide; esta prueba sí.
/// Regla (de adentro hacia afuera): Domain no conoce a nadie; Application solo a Domain; Infrastructure a Application y
/// Domain; Presentation a Application y Domain (nunca a Infrastructure). <c>Program.cs</c> es la raíz de composición y
/// puede ver todo.
/// </summary>
public partial class ArquitecturaTests
{
    private static readonly Dictionary<string, string[]> Permitidas = new()
    {
        ["Domain"] = ["Domain"],
        ["Application"] = ["Domain", "Application"],
        ["Infrastructure"] = ["Domain", "Application", "Infrastructure"],
        ["Presentation"] = ["Domain", "Application", "Presentation"]
    };

    [Theory]
    [InlineData("Domain")]
    [InlineData("Application")]
    [InlineData("Infrastructure")]
    [InlineData("Presentation")]
    public void Cada_capa_solo_depende_de_las_de_adentro(string capa)
    {
        var violaciones = (
            from archivo in Directory.EnumerateFiles(Path.Combine(Backend(), capa), "*.cs", SearchOption.AllDirectories)
            from referencia in Referencias(archivo)
            let destino = CapaDe(referencia.Usa)
            where destino is not null && !Permitidas[capa].Contains(destino)
            select $"{Path.GetRelativePath(Backend(), archivo)}:{referencia.Linea} usa {referencia.Usa} ({destino})").ToList();

        Assert.True(violaciones.Count == 0, $"{capa} depende de capas de afuera:\n" + string.Join("\n", violaciones));
    }

    [Fact]
    public void Las_reglas_de_negocio_no_dependen_de_frameworks()
    {
        var prohibidos = new[] { "Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore", "MySqlConnector", "System.Net.Http" };
        var violaciones = (
            from archivo in Directory.EnumerateFiles(Path.Combine(Backend(), "Domain"), "*.cs", SearchOption.AllDirectories)
            from linea in File.ReadLines(archivo).Select((texto, i) => (Numero: i + 1, Texto: texto))
            where prohibidos.Any(p => linea.Texto.TrimStart().StartsWith($"using {p}", StringComparison.Ordinal))
            select $"{Path.GetFileName(archivo)}:{linea.Numero} {linea.Texto.Trim()}").ToList();

        Assert.True(violaciones.Count == 0, "Domain usa frameworks:\n" + string.Join("\n", violaciones));
    }

    [Fact]
    public void Cada_capa_registra_sus_servicios()
    {
        Assert.True(File.Exists(Path.Combine(Backend(), "Application", "DependencyInjection.cs")));
        Assert.True(File.Exists(Path.Combine(Backend(), "Infrastructure", "DependencyInjection.cs")));
        // Program.cs compone las capas, no registra casos de uso ni repositorios uno por uno.
        var program = File.ReadAllText(Path.Combine(Backend(), "Program.cs"));
        Assert.DoesNotMatch(@"AddScoped<\w*(Service|Repository)\b", program);
    }

    /// <summary>La regla misma, probada contra ejemplos (si no, una regex rota dejaría pasar todo).</summary>
    [Theory]
    [InlineData("using Breaku.Infrastructure.Persistence;", "Breaku.Infrastructure.Persistence")]
    [InlineData("using Breaku.Api.Hubs;", "Breaku.Api.Hubs")]
    [InlineData("    var x = new Breaku.Api.Hubs.UsuarioIdProvider();", "Breaku.Api.Hubs.UsuarioIdProvider")]
    [InlineData("namespace Breaku.Infrastructure;", null)]
    [InlineData("// usa Breaku.Infrastructure en un comentario", null)]
    public void Detecta_las_referencias(string linea, string? esperada)
        => Assert.Equal(esperada, ReferenciasDe(linea).FirstOrDefault());

    [Theory]
    [InlineData("Breaku.Domain.Rules", "Domain")]
    [InlineData("Breaku.Api.Controllers", "Presentation")]
    [InlineData("Breaku.Presentation.Rendimiento", "Presentation")]
    [InlineData("Breaku.Infrastructure.Services", "Infrastructure")]
    [InlineData("Breaku.Application.TiempoReal", "Application")]
    [InlineData("Breaku.Tests", null)]
    public void Reconoce_la_capa(string espacio, string? capa) => Assert.Equal(capa, CapaDe(espacio));

    // ---------- ayuda

    private static IEnumerable<(int Linea, string Usa)> Referencias(string archivo)
        => File.ReadLines(archivo).SelectMany((texto, i) => ReferenciasDe(texto).Select(r => (i + 1, r)));

    private static IEnumerable<string> ReferenciasDe(string linea)
    {
        var codigo = linea.Split("//")[0];
        if (codigo.TrimStart().StartsWith("namespace ", StringComparison.Ordinal)) return [];
        return Referencia().Matches(codigo).Select(m => m.Value.TrimEnd('.', ';'));
    }

    private static string? CapaDe(string espacio)
    {
        var segundo = espacio.Split('.').ElementAtOrDefault(1);
        return segundo switch
        {
            "Domain" => "Domain",
            "Application" => "Application",
            "Infrastructure" => "Infrastructure",
            "Api" or "Presentation" => "Presentation",
            _ => null
        };
    }

    private static string Backend([CallerFilePath] string esteArchivo = "")
        => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(esteArchivo)!, ".."));

    [GeneratedRegex(@"\bBreaku\.(Domain|Application|Infrastructure|Api|Presentation)(\.[A-Za-z_][A-Za-z0-9_]*)*")]
    private static partial Regex Referencia();
}
