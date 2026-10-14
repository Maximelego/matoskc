FROM node:24-alpine AS build

WORKDIR /app

COPY app/package.json app/package-lock.json ./

RUN npm ci

COPY app/ ./

ARG VITE_API_BASE_URL=/backend
ARG VITE_PUBLIC_APP_URL

RUN test "$VITE_API_BASE_URL" = "/backend" \
    && test -n "$VITE_PUBLIC_APP_URL" \
    && VITE_API_BASE_URL="$VITE_API_BASE_URL" \
       VITE_PUBLIC_APP_URL="$VITE_PUBLIC_APP_URL" npm run build

FROM nginx:stable-alpine AS runtime

COPY nginx.prod.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/ /usr/share/nginx/html/

EXPOSE 80

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s \
    CMD wget -q -O /dev/null http://127.0.0.1/healthz || exit 1
