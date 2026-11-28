using Dayspot.Api.Auth;

namespace Dayspot.Api.Tests;

// #211: os mesmos casos do teste do front (web/src/app/entrada/senha.spec.ts).
public class ForcaSenhaTests
{
    private const string Nome = "Ana Souza";
    private const string Email = "ana.rocha@salao.example";

    [Theory]
    [InlineData("123456789", "Use pelo menos 10 caracteres.")] // 9
    [InlineData("😀😀😀😀😀😀😀😀😀", "Use pelo menos 10 caracteres.")] // 9 caracteres, 18 unidades UTF-16
    [InlineData("1234567890", "Essa senha é muito usada. Escolha outra.")]
    [InlineData("QwertyUIOP", "Essa senha é muito usada. Escolha outra.")] // sem diferenciar maiúscula
    [InlineData("xx-ana.rocha-xx", "A senha não pode ter seu nome ou seu e-mail.")] // parte antes do @
    [InlineData("SOUZA-2024!", "A senha não pode ter seu nome ou seu e-mail.")]
    [InlineData("eu-sou-souzá-sim", "A senha não pode ter seu nome ou seu e-mail.")] // sem acento
    [InlineData("çççççççççç", "Essa senha é muito usada. Escolha outra.")] // sem acento, vira cccccccccc
    public void Recusa(string senha, string motivo) => Assert.Equal(motivo, ForcaSenha.Motivo(senha, Nome, Email));

    [Theory]
    [InlineData("cafe-com-pao-42")]
    [InlineData("banana-madura")] // "Ana" tem 3 letras: não barra
    [InlineData("çãéíóúçãéí")] // 10 caracteres
    public void Aceita(string senha) => Assert.Null(ForcaSenha.Motivo(senha, Nome, Email));

    [Fact]
    public void AcimaDe1024() =>
        Assert.Equal("Use até 1024 caracteres.", ForcaSenha.Motivo(new string('x', 1025), Nome, Email));
}
