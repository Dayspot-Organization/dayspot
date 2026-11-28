# Imagem da API (8.1). Build na raiz do repositório: docker build -t dayspot-api .
# Config só por variável de ambiente (.env.example); o .env não entra na imagem.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
# Só o que o restore lê, para a camada do restore ficar em cache enquanto o csproj não muda.
COPY global.json ./
COPY .config/dotnet-tools.json .config/
COPY src/Api/Api.csproj src/Api/
RUN dotnet restore src/Api/Api.csproj && dotnet tool restore
COPY src/Api/ src/Api/
RUN dotnet publish src/Api/Api.csproj -c Release -o /out --no-restore -p:UseAppHost=false
# Migrations num executável: o deploy aplica sem SDK nem código-fonte no servidor
# (deploy/compose.yml, serviço migracoes). Ao rodar, lê a MIGRATIONS_DATABASE_URL do ambiente
# (AppDbDesignTime). A URL daqui só monta o AppDb para gerar o bundle: nada conecta no build.
RUN MIGRATIONS_DATABASE_URL=postgres://build@localhost/build \
    dotnet ef migrations bundle --project src/Api --configuration Release -o /out/efbundle

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /out ./
ENV PORTA_HTTP=8080 \
    ASPNETCORE_HTTP_PORTS=
EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "Dayspot.Api.dll"]
