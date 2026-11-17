namespace Dayspot.Api.Auth;

/// <summary>Autenticação da Fatia 2 (cadastro, login e sessão).</summary>
public static class Email
{
    /// <summary>
    /// Tira espaços das pontas e passa para minúsculas, no cadastro e no login
    /// (2.15). O índice usuarios_email_unico, em lower(email), garante a unicidade
    /// mesmo se alguém gravar sem passar por aqui.
    /// </summary>
    public static string Normalizar(string email) => email.Trim().ToLowerInvariant();
}
