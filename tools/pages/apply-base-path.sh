#!/usr/bin/env bash
# Prepara la aplicación publicada para servirla bajo una ruta base (GitHub Pages:
# /HackMasterCompanion/). Lo usan pages.yml (publicación real) y ci.yml (pruebas del sitio
# publicado, F013), así la prueba ve exactamente lo que se publica.
#
#   bash tools/pages/apply-base-path.sh publish/wwwroot /HackMasterCompanion/
set -euo pipefail

site="${1:?Falta la carpeta publicada (wwwroot)}"
base="${2:?Falta la ruta base, por ejemplo /HackMasterCompanion/}"
index="$site/index.html"

sed -i "s|<base href=\"/\" />|<base href=\"${base}\" />|" "$index"
grep -q "<base href=\"${base}\" />" "$index"
# Las copias comprimidas se generaron antes del cambio y quedarían obsoletas.
rm -f "$index.br" "$index.gz"

# El trabajador de servicio comprueba el resumen de cada archivo al guardarlo (F013, research R2):
# index.html acaba de cambiar, así que su resumen también.
assets="$site/service-worker-assets.js"
if [ -f "$assets" ]; then
  hash="sha256-$(openssl dgst -sha256 -binary "$index" | base64 -w0)"
  # El resumen está en la línea anterior a "url": "index.html".
  line=$(grep -n '"url": "index.html"' "$assets" | cut -d: -f1)
  [ -n "$line" ] || { echo "index.html no está en $assets" >&2; exit 1; }
  sed -i "$((line - 1))s|\"hash\": \"sha256-[^\"]*\"|\"hash\": \"${hash}\"|" "$assets"
  grep -q "\"hash\": \"${hash}\"" "$assets"
  rm -f "$assets.br" "$assets.gz"
fi

# Las direcciones internas (/habilidades/…) cargan la aplicación.
cp "$index" "$site/404.html"
