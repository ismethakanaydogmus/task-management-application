# =============================================================================
# Frontend Dockerfile — MULTI-STAGE BUILD
#
#   build : Node ile tip kontrolü + Vite production build → dist/ (statik HTML/JS/CSS)
#   final : nginx (root OLMAYAN) → dist/'i sunar + /api/* isteklerini API container'ına iletir
#
# Node, node_modules (~200 MB), kaynak kod son image'a GİRMEZ: tarayıcıya gidecek olan sadece dist/.
# =============================================================================

# ----------------------------------------------------------------------------- build
FROM node:24-alpine AS build
WORKDIR /app

# KATMAN ÖNBELLEĞİ: önce sadece paket tanımları → kod değişince "npm ci" önbellekten gelir.
COPY package.json package-lock.json ./
# npm ci (npm install DEĞİL): package-lock.json'daki BİREBİR sürümleri kurar, lock dosyasını değiştirmez.
# CI/Docker'da tekrarlanabilir build için doğru komut.
RUN npm ci

COPY . .
# .env.production → VITE_API_URL boş (aynı origin). "tsc -b" tip hatası varsa build'i durdurur.
RUN npm run build

# ----------------------------------------------------------------------------- final
# nginx-unprivileged: root OLMAYAN kullanıcıyla çalışan resmi nginx varyantı (8080'i dinler).
FROM nginxinc/nginx-unprivileged:1.28-alpine AS final

COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 8080
