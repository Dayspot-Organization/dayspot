namespace Dayspot.Api.Tests;

public class LimitePorJanelaTests
{
    private static readonly TimeSpan Hora = TimeSpan.FromHours(1);

    private static (LimitePorJanela Limite, RelogioAjustavel Relogio) Criar()
    {
        var relogio = new RelogioAjustavel(new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero));
        return (new LimitePorJanela(5, Hora, relogio), relogio);
    }

    [Fact]
    public void CincoPassamOSextoEhRecusado()
    {
        var (limite, _) = Criar();
        for (var i = 0; i < 5; i++)
        {
            Assert.True(limite.Tentar("a", out _));
        }
        Assert.False(limite.Tentar("a", out var espera));
        Assert.Equal(Hora, espera);
    }

    [Fact]
    public void EsperaEhOQueFaltaDaJanela()
    {
        var (limite, relogio) = Criar();
        Assert.True(limite.Tentar("a", out _));
        relogio.Agora += TimeSpan.FromMinutes(40);
        for (var i = 0; i < 4; i++)
        {
            Assert.True(limite.Tentar("a", out _));
        }
        Assert.False(limite.Tentar("a", out var espera));
        Assert.Equal(TimeSpan.FromMinutes(20), espera);
    }

    [Fact]
    public void PassadaAJanelaVoltaAAceitar()
    {
        var (limite, relogio) = Criar();
        for (var i = 0; i < 6; i++)
        {
            limite.Tentar("a", out _);
        }
        relogio.Agora += Hora;
        Assert.True(limite.Tentar("a", out _));
    }

    [Fact]
    public void ChavesSeparadas()
    {
        var (limite, _) = Criar();
        for (var i = 0; i < 6; i++)
        {
            limite.Tentar("a", out _);
        }
        Assert.False(limite.Tentar("a", out _));
        Assert.True(limite.Tentar("b", out _));
    }

    [Fact]
    public async Task ConcorrenciaNaoPassaDoLimite()
    {
        var (limite, _) = Criar();
        var aceitas = 0;
        await Task.WhenAll(Enumerable.Range(0, 50).Select(__ => Task.Run(() =>
        {
            if (limite.Tentar("a", out _))
            {
                Interlocked.Increment(ref aceitas);
            }
        })));
        Assert.Equal(5, aceitas);
    }

    [Fact]
    public void EntradasVencidasSaemQuandoOMapaCresce()
    {
        var (limite, relogio) = Criar();
        for (var i = 0; i < 10_001; i++)
        {
            limite.Tentar("k" + i, out _);
        }
        relogio.Agora += Hora;
        // Uma chave nova com o mapa acima do teto varre as vencidas; a antiga recomeça do zero.
        Assert.True(limite.Tentar("nova", out _));
        for (var i = 0; i < 5; i++)
        {
            Assert.True(limite.Tentar("k0", out _));
        }
        Assert.False(limite.Tentar("k0", out _));
    }
}
