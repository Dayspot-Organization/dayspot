using Dayspot.Api.Auth;

namespace Dayspot.Api.Tests;

public class EmailTests
{
    [Theory]
    [InlineData(" Ana@Exemplo.com ", "ana@exemplo.com")] // exemplo do card
    [InlineData("ana@exemplo.com", "ana@exemplo.com")] // já normalizado
    [InlineData("\tANA@EXEMPLO.COM\n", "ana@exemplo.com")] // tab e quebra de linha
    [InlineData("   ", "")] // vazio
    public void Normalizar(string entrada, string quer) => Assert.Equal(quer, Email.Normalizar(entrada));
}

public class SenhaTests
{
    private const string SenhaDeTeste = "senha-de-teste-123";

    [Fact]
    public void GerarHashEConferir()
    {
        var hash = Senha.GerarHash(SenhaDeTeste);

        Assert.StartsWith("$argon2id$v=19$m=19456,t=2,p=1$", hash);
        Assert.DoesNotContain(SenhaDeTeste, hash);
        // O salt é aleatório: dois hashes da mesma senha saem diferentes.
        Assert.NotEqual(hash, Senha.GerarHash(SenhaDeTeste));

        Assert.True(Senha.Conferir(SenhaDeTeste, hash));
        Assert.False(Senha.Conferir("senha-errada", hash));
        Assert.False(Senha.Conferir("", hash));
        Assert.False(Senha.Conferir(SenhaDeTeste.ToUpperInvariant(), hash)); // maiúscula muda a senha
    }

    // Hashes gerados pela implementação anterior (Go, pacote x/crypto/argon2):
    // senha gravada antes da troca de linguagem continua valendo. O primeiro tem os
    // parâmetros de produção; os outros, parâmetros diferentes, que é o que permite
    // subir o custo depois sem migration.
    [Theory]
    [InlineData(SenhaDeTeste, "$argon2id$v=19$m=19456,t=2,p=1$YoDOkuQuoEgCFdR6PSJliQ$4fsPYsOxD+wmNd4DsqUfPsOv5YfRST6sdfrXEPNbUq0")]
    [InlineData("antiga", "$argon2id$v=19$m=64,t=1,p=1$c2FsdC1kZS0xNi1ieXRlcw$NFTDJ9cq3WvszRKMfHUtSPOhiiV37zqd72IVjt1hwj8")]
    [InlineData("senha com acento é ção", "$argon2id$v=19$m=32,t=3,p=4$YWJj$/SKEviJoDk317/e4M4cknIABxcapqQQp")]
    public void ConfereHashDaImplementacaoAnterior(string senha, string hash)
    {
        Assert.True(Senha.Conferir(senha, hash));
        Assert.False(Senha.Conferir(senha + "x", hash));
    }

    [Theory]
    [InlineData(-1, "")] // vazio
    [InlineData(-1, "$2a$12$R9h/cIPz0gi.URNNX3kh2OPST9/PgBkqquzi.Ss7KIUgO2t0jWMUW")] // bcrypt
    [InlineData(1, "argon2i")]
    [InlineData(2, "v=16")] // versão antiga
    [InlineData(3, "m=abc")] // parâmetros ruins
    [InlineData(3, "m=19456,t=2,p=0")] // threads zero
    [InlineData(3, "m=19456,t=0,p=1")] // tempo zero
    [InlineData(4, "!!!")] // salt sem base64
    [InlineData(5, "")] // chave vazia
    public void HashInvalido(int parte, string valor)
    {
        // parte >= 0 troca só aquele pedaço de um hash válido; -1 usa o valor inteiro.
        var hash = valor;
        if (parte >= 0)
        {
            var partes = Senha.GerarHash("x").Split('$');
            partes[parte] = valor;
            hash = string.Join('$', partes);
        }
        Assert.Throws<HashInvalidoException>(() => Senha.Conferir("x", hash));
    }
}
