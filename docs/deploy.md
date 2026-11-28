# Deploy — Dayspot

Produção e homologação rodam na mesma VPS (Ubuntu, Docker Compose), decisão de
26/11/2026 (RNF09 em `requisitos.md`). Este roteiro cobre a preparação do servidor,
o deploy, a volta de versão e a regra das migrations.

| Ambiente | Endereço | Publica quando |
|---|---|---|
| Produção | `https://dayspot.app.br` (o `www` redireciona) | **Run workflow** do CI na `main` |
| Homologação | `https://homolog.dayspot.app.br`, só para os IPs liberados | push na `dev` (merge de PR) |

## Como funciona

```
Internet ─▶ :80/:443  Caddy (deploy/borda: HTTPS, filtro de IP na homologação)
              ├─ dayspot.app.br ─── /api/* ─▶ producao-api ─▶ banco da produção
              │                  └─ resto ─▶ producao-web (Angular)
              └─ homolog.dayspot.app.br ─── /api/* ─▶ homologacao-api ─▶ banco da homologação
                                         └─ resto ─▶ homologacao-web
```

- **Imagens:** o CI gera `dayspot-api` e `dayspot-web` com a tag do sha do commit e
  publica no GHCR (`ghcr.io/dayspot-organization/`). As imagens são públicas e não
  têm segredo.
- **Deploy:** o CI entra por SSH como `deploy` e pede `<ambiente> <sha>`. A chave só
  roda o `deploy/deploy.sh` (comando forçado). O script segue esta ordem: código do
  commit → imagens → migrations → contêineres → saúde (`/api/saude` e front pela rede
  interna). O deploy de produção também atualiza a borda.
- **Portas:** só o Caddy publica porta (80, 443 e 443/udp). O Docker passa por cima do
  ufw, então nenhum outro serviço do compose pode ganhar `ports:`.
- **Segredos:** ficam nos `.env` do servidor (`chmod 600`, dono `deploy`). No GitHub
  fica só a chave SSH do deploy.

## No servidor

```
/srv/dayspot/
├── borda.env               # HOMOLOG_IPS
├── producao/
│   ├── .env                # senhas e URLs do banco, AMBIENTE=producao, IDs fixos
│   └── repo/               # clone do repositório, no commit publicado
└── homologacao/
    ├── .env
    └── repo/
/usr/local/bin/dayspot-deploy   # cópia do deploy/deploy.sh (dono root)
```

O `.env` de cada ambiente segue o `.env.example`, com o host `db` nas URLs e mais a
variável `AMBIENTE` (`producao` ou `homologacao`). As senhas são geradas no próprio
servidor, com `openssl rand -hex 24`, e não saem de lá.

### Preparar uma VPS nova (uma vez, como root)

1. **SSH:** só por chave (`PasswordAuthentication no`, `KbdInteractiveAuthentication no`,
   `PermitRootLogin prohibit-password`). Firewall com `ufw allow OpenSSH`, `80/tcp`,
   `443/tcp` e `443/udp`, e depois `ufw enable`.
2. **Pacotes:** `apt install docker.io docker-compose-v2 git`. No
   `/etc/docker/daemon.json`, `{"log-driver": "local"}`, que faz rotação do log. Swap de
   2 GB.
3. **Usuário:** `useradd -m -s /bin/bash -G docker deploy`. O grupo `docker` equivale
   a root, por isso a chave do CI só roda o comando forçado.
4. **Pastas:** a estrutura acima com os dois `.env`, e o repositório clonado como
   `deploy` em cada ambiente:
   `git clone https://github.com/Dayspot-Organization/dayspot.git /srv/dayspot/<ambiente>/repo`.
5. **Script:** instale com
   `install -o root -g root -m 755 /srv/dayspot/producao/repo/deploy/deploy.sh /usr/local/bin/dayspot-deploy`.
   Quando o script mudar no repositório, instale de novo à mão, depois de revisar.
6. **Chave do CI:** gere fora do servidor com `ssh-keygen -t ed25519 -N "" -f deploy_ci`.
   A pública vai para o `/home/deploy/.ssh/authorized_keys` assim:
   ```
   command="/usr/local/bin/dayspot-deploy",restrict ssh-ed25519 AAAA... deploy-ci
   ```
   A privada vai para o secret do GitHub (abaixo). Depois, apague as duas cópias locais.
7. **Borda:** como `deploy`, com o `docker network` criado pelo próprio compose:
   ```sh
   cd /srv/dayspot/producao/repo/deploy/borda
   docker compose --env-file /srv/dayspot/borda.env up -d
   ```

### No GitHub (uma vez)

- **Branches:** `dev` criada a partir da `main` e definida como **branch padrão**. Sem
  isso, o `Closes #N` só fecha a issue quando o PR chega na `main`. Proteção na `dev` e
  na `main`: PR com 1 aprovação e CI verde.
- **Environments:** `homologacao` (deployment branch: só `dev`) e `producao` (só
  `main`).
- **Secret do repositório:** `DEPLOY_SSH_KEY`, a chave privada do passo 6.
- **Variáveis do repositório:** `DEPLOY_HOST` com o IP da VPS, e `DEPLOY_KNOWN_HOSTS`
  com a linha `ssh-ed25519` da VPS. Para pegá-la: `ssh-keyscan -t ed25519 <ip>`, e
  confira a impressão digital contra o console da VPS antes de gravar.
- **Pacotes:** depois do primeiro push das imagens, em *Packages* › cada imagem ›
  *Package settings*, mude a visibilidade para **Public**. Assim a VPS baixa sem token.

### Primeira subida da homologação

1. Faça um push na `dev` (merge de um PR) e acompanhe o job **Deploy** no Actions.
2. Na primeira vez, carregue o seed fictício. **Só na homologação, nunca na produção:**
   ```sh
   docker exec -i dayspot-homologacao-db-1 \
     sh -c 'psql -U "$POSTGRES_USER" -d "$POSTGRES_DB"' < /srv/dayspot/homologacao/repo/db/seed.sql
   ```

## Deploy

- **Homologação:** é sozinho, a cada push na `dev`.
- **Produção:** em **Actions › CI › Run workflow**, escolha a `main`. Os testes rodam de
  novo antes.
- **À mão, no servidor**, se o CI estiver fora: `dayspot-deploy homologacao <sha>`.

## Voltar uma versão

No Actions, abra a execução da versão boa e use **Re-run jobs**: ela publica de novo
aquele sha. O banco **não volta**: as migrations só andam para a frente. Por isso
vale a regra abaixo.

## Regra das migrations

Durante o deploy, por alguns segundos, a versão anterior da API roda sobre o banco já
migrado, e a volta de versão faz o mesmo por mais tempo. Toda migration precisa
funcionar com a versão anterior da API:

- **Pode num deploy só:** criar tabela, coluna que aceita nulo ou tem default, índice.
- **Precisa de dois deploys:** renomear ou apagar coluna, ou tornar uma coluna
  obrigatória. No primeiro, a API para de usar a coluna; no segundo, a migration mexe
  nela.

Migration que falha para o deploy antes da troca: a versão no ar continua no ar.

## IPs da homologação

Os IPs ficam no `/srv/dayspot/borda.env`, nunca no repositório (o repo é público). A
lista é separada por espaço, com IPv4 e IPv6 em notação CIDR:

```sh
HOMOLOG_IPS=203.0.113.7/32 2001:db8:1234:5678::/64
```

Depois de mudar, como `deploy`:
`cd /srv/dayspot/producao/repo/deploy/borda && docker compose --env-file /srv/dayspot/borda.env up -d`.
Com a lista vazia, ninguém entra. IP de casa muda com frequência, e com CGNAT o IPv4
é compartilhado com outros clientes da operadora. Quando o seu IPv6 existir, libere o
bloco /64 dele.

## Logs e banco

```sh
docker logs --tail 100 dayspot-producao-api-1
docker exec -it dayspot-producao-db-1 sh -c 'psql -U "$POSTGRES_USER" -d "$POSTGRES_DB"'
```

O banco não tem porta aberta para fora. Para usar um cliente gráfico, faça um túnel
SSH para o IP do contêiner, sem publicar porta.

**Nunca rode `down -v`:** apaga o banco do ambiente e, na borda, os certificados (o
Let's Encrypt limita quantos se pede por semana).

## Pendente

- **Backup:** ainda não existe. Plano: `pg_dump` diário, criptografado, com uma cópia
  na VPS e outra na Cloudflare. Precisa existir antes de a empresa piloto ter dado
  real (#174).
- **Monitor externo** do `/api/saude` (#172).
