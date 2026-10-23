# Imagen de la web (Flutter Web ya compilada) con nginx sin privilegios.
# 1) cd app && flutter build web --release --dart-define=API_BASE_URL=/       (la API se sirve bajo el mismo origen, /api)
# 2) docker build -f deploy/web/Dockerfile -t medicalhub-web .                 (contexto: raíz del repositorio)
FROM nginxinc/nginx-unprivileged:stable-alpine
COPY deploy/web/nginx.conf /etc/nginx/conf.d/default.conf
COPY app/build/web /usr/share/nginx/html
EXPOSE 8080
