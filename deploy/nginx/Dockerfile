# Web tier: builds the Angular SPA and serves it, reverse-proxying /api to the
# API container. Build context is the REPO ROOT (compose sets `context: .`).

# ---- Build stage: compile the Angular production bundle ----
FROM node:22-alpine AS build
WORKDIR /app
COPY frontend/package*.json ./
RUN npm ci
COPY frontend/ ./
RUN npm run build -- --configuration production

# ---- Runtime stage: nginx serving the compiled SPA ----
FROM nginx:1.27-alpine
COPY deploy/nginx/default.conf /etc/nginx/conf.d/default.conf
# The application builder emits the browser bundle under dist/<project>/browser.
COPY --from=build /app/dist/ticketing-frontend/browser/ /usr/share/nginx/html/
