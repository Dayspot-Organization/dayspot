# Imagem da API (8.1). Build na raiz do repositório: docker build -t dayspot-api .
# Config só por variável de ambiente (.env.example); o .env não entra na imagem.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
# Só o que o restore lê, para a camada do restore ficar em cache enquanto o csproj não muda.
COPY global.json ./
COPY src/Api/Api.csproj src/Api/
RUN dotnet restore src/Api/Api.csproj
COPY src/Api/ src/Api/
# Lista de senhas comuns, embutida pelo Api.csproj (#211).
COPY web/src/senhas-comuns.txt web/src/
RUN dotnet publish src/Api/Api.csproj -c Release -o /out --no-restore -p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /out ./
# A API escuta na PORTA_HTTP; o Render passa a porta em PORT, que vence se existir.
ENV PORTA_HTTP=8080 \
    ASPNETCORE_HTTP_PORTS=
EXPOSE 8080
USER app
ENTRYPOINT ["sh", "-c", "export PORTA_HTTP=\"${PORT:-$PORTA_HTTP}\"; exec dotnet Dayspot.Api.dll"]
