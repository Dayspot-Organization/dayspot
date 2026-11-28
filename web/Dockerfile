# Imagem do front. Build com web/ como contexto: docker build -t dayspot-web web/
# Só arquivos estáticos: HTTPS e o /api ficam com a borda (deploy/borda/Caddyfile).

FROM node:22-alpine AS build
WORKDIR /web
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
RUN npm run build

FROM caddy:2-alpine
COPY Caddyfile /etc/caddy/Caddyfile
COPY --from=build /web/dist/web/browser /srv
