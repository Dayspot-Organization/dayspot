using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.HttpOverrides;

namespace Dayspot.Api;

/// <summary>IP do cliente atrás do Caddy da VPS (deploy/borda), para os limites por IP.</summary>
public static class IpReal
{
    public static IServiceCollection AddIpReal(this IServiceCollection servicos)
    {
        // O Caddy grava no X-Forwarded-For o IP de quem conectou nele e descarta o que o
        // cliente mandou. Numa lista, só o último item é do Caddy: o começo é do cliente.
        servicos.Configure<ForwardedHeadersOptions>(o =>
        {
            o.ForwardedHeaders = ForwardedHeaders.XForwardedFor;
            o.ForwardLimit = 1;
            // ponytail: confia em qualquer par. A API não publica porta: só o Caddy, pela
            // rede interna do Docker, chega nela. Publicou porta? Restrinja ao IP do Caddy.
            o.KnownProxies.Clear();
            o.KnownIPNetworks.Clear();
        });
        return servicos;
    }

    /// <summary>Chave do cliente para contar limite: o IP real, com IPv6 agrupado por /64.</summary>
    public static string Chave(HttpContext ctx) => ChaveDoIp(ctx.Connection.RemoteIpAddress);

    // IPv6 conta por /64: é o bloco de um assinante, que troca de endereço dentro dele à vontade.
    private static string ChaveDoIp(IPAddress? ip)
    {
        if (ip is null)
        {
            return "";
        }
        if (ip.IsIPv4MappedToIPv6)
        {
            return ip.MapToIPv4().ToString();
        }
        if (ip.AddressFamily != AddressFamily.InterNetworkV6)
        {
            return ip.ToString();
        }
        var bytes = ip.GetAddressBytes();
        Array.Clear(bytes, 8, 8);
        return new IPAddress(bytes).ToString();
    }
}
