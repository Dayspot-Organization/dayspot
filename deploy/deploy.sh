#!/bin/sh
# Deploy de um ambiente da VPS: dayspot-deploy <producao|homologacao> <sha do commit>.
#
# É o comando forçado da chave do CI no authorized_keys do usuário deploy: pelo SSH, o
# CI só consegue pedir ambiente e sha (o pedido chega em SSH_ORIGINAL_COMMAND). Fica
# instalado à mão em /usr/local/bin/dayspot-deploy, fora do clone, para um push não
# trocar o script que ele mesmo roda (docs/deploy.md).
set -eu
set -f # o pedido vem de fora: nada de expandir * ou ?

# Sem aspas de propósito: separa ambiente e sha (o set -f acima já barra * e ?).
# shellcheck disable=SC2086
[ -n "${SSH_ORIGINAL_COMMAND:-}" ] && set -- $SSH_ORIGINAL_COMMAND
if [ $# -ne 2 ]; then
  echo "uso: dayspot-deploy <producao|homologacao> <sha do commit>" >&2
  exit 2
fi
ambiente=$1
sha=$2
case $ambiente in
  producao | homologacao) ;;
  *) echo "ambiente inválido: use producao ou homologacao" >&2; exit 2 ;;
esac
case $sha in
  *[!0-9a-f]*) echo "sha inválido: 40 caracteres hexadecimais" >&2; exit 2 ;;
esac
if [ ${#sha} -ne 40 ]; then
  echo "sha inválido: 40 caracteres hexadecimais" >&2
  exit 2
fi

raiz=/srv/dayspot
dir=$raiz/$ambiente
borda=dayspot-borda-caddy-1

echo "==> $ambiente: código do commit $sha"
git -C "$dir/repo" fetch -q origin
git -C "$dir/repo" checkout -q --detach "$sha"

export TAG="$sha"
dc() {
  docker compose -p "dayspot-$ambiente" --env-file "$dir/.env" -f "$dir/repo/deploy/compose.yml" "$@"
}

echo "==> imagens"
dc pull -q api web

# Antes de trocar a API: se a migration falhar, a versão no ar continua no ar.
echo "==> migrations"
dc run --rm migracoes

echo "==> contêineres"
dc up -d --remove-orphans

# Só a produção mexe na borda, que também atende a produção (docs/deploy.md).
if [ "$ambiente" = producao ]; then
  echo "==> borda"
  docker compose --env-file "$raiz/borda.env" -f "$dir/repo/deploy/borda/compose.yml" up -d
  # Caddyfile inválido: o reload falha, o Caddy segue com o anterior e o deploy acusa.
  docker exec "$borda" caddy reload --config /etc/caddy/Caddyfile
fi

echo "==> saúde"
for alvo in "$ambiente-api:8080/api/saude" "$ambiente-web:80/"; do
  tentativa=0
  until docker exec "$borda" wget -q -O /dev/null "http://$alvo"; do
    tentativa=$((tentativa + 1))
    if [ "$tentativa" -ge 30 ]; then
      echo "sem resposta de http://$alvo; logs: docker logs dayspot-$ambiente-api-1" >&2
      exit 1
    fi
    sleep 2
  done
done

# Só imagens sem contêiner e com mais de 7 dias: a do outro ambiente está em uso.
docker image prune -af --filter "until=168h" >/dev/null
echo "==> $ambiente no ar com $sha"
