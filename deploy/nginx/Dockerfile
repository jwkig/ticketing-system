# Web tier: serves the SPA and reverse-proxies /api to the API container.
# Build context is the REPO ROOT (compose sets `context: .`).
FROM nginx:1.27-alpine

COPY deploy/nginx/default.conf /etc/nginx/conf.d/default.conf

# Placeholder site root until the Angular app is implemented. When the frontend
# lands, replace the COPY below with a multi-stage Node build that emits the
# Angular production bundle, e.g.:
#
#   FROM node:22-alpine AS build
#   WORKDIR /app
#   COPY frontend/package*.json ./
#   RUN npm ci
#   COPY frontend/ ./
#   RUN npm run build -- --configuration production
#   # then in this final stage:
#   COPY --from=build /app/dist/ /usr/share/nginx/html/
#
COPY deploy/nginx/placeholder/ /usr/share/nginx/html/
