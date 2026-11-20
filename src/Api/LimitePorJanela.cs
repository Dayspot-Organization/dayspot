using System.Collections.Concurrent;

namespace Dayspot.Api;

/// <summary>
/// Contador em memória, por chave, de janela fixa: a janela abre na 1ª tentativa e, passado o
/// prazo, a chave recomeça do zero. Conta toda tentativa, inclusive a recusada. O estado é do
/// processo (some no reinício e não é dividido entre instâncias).
/// </summary>
public sealed class LimitePorJanela(int limite, TimeSpan janela, TimeProvider relogio)
{
    private const int MaxChaves = 10_000;

    private sealed class Entrada
    {
        public DateTimeOffset Abertura;
        public int Contagem;
    }

    private readonly ConcurrentDictionary<string, Entrada> _entradas = new();

    /// <summary>Conta a tentativa; true se ainda está dentro do limite. Ao recusar, espera é o que falta para a janela fechar.</summary>
    public bool Tentar(string chave, out TimeSpan espera)
    {
        var agora = relogio.GetUtcNow();
        if (_entradas.Count > MaxChaves)
        {
            Varrer(agora);
        }
        var entrada = _entradas.GetOrAdd(chave, _ => new Entrada { Abertura = agora });
        lock (entrada)
        {
            if (agora - entrada.Abertura >= janela)
            {
                entrada.Abertura = agora;
                entrada.Contagem = 0;
            }
            if (entrada.Contagem < limite)
            {
                entrada.Contagem++;
                espera = TimeSpan.Zero;
                return true;
            }
            espera = entrada.Abertura + janela - agora;
            return false;
        }
    }

    // Sem timer: a limpeza pega carona na tentativa que encontra o mapa grande demais.
    // Remover uma entrada que outra thread acabou de pegar só reinicia a contagem dela,
    // e isso só acontece com a janela já vencida.
    private void Varrer(DateTimeOffset agora)
    {
        foreach (var (chave, entrada) in _entradas)
        {
            bool vencida;
            lock (entrada)
            {
                vencida = agora - entrada.Abertura >= janela;
            }
            if (vencida)
            {
                _entradas.TryRemove(chave, out _);
            }
        }
    }
}
