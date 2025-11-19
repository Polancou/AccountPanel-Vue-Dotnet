# Etapa 1: Construcción
FROM node:20-alpine AS build
WORKDIR /app

# Copiar dependencias
COPY package*.json ./
RUN npm install

# Copiar código fuente
COPY . .

# Argumento de construcción para la URL de la API
# Nota: Vite incrusta esto en el HTML/JS al momento de construir.
ARG VITE_API_BASE_URL
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL
# La URL de uploads suele ser la misma base de la API si sirves estáticos desde ahí
ARG VITE_UPLOADS_BASE_URL
ENV VITE_UPLOADS_BASE_URL=$VITE_UPLOADS_BASE_URL
ARG VITE_GOOGLE_CLIENT_ID
ENV VITE_GOOGLE_CLIENT_ID=$VITE_GOOGLE_CLIENT_ID

# Construir para producción
RUN npm run build

# Etapa 2: Servidor Web (Nginx)
FROM nginx:alpine
WORKDIR /usr/share/nginx/html

# Limpiar archivos por defecto de Nginx
RUN rm -rf ./*

# Copiar los archivos construidos (dist) de la etapa anterior
COPY --from=build /app/dist .

# Copiar configuración personalizada de Nginx
COPY nginx.conf /etc/nginx/nginx.conf

ENTRYPOINT ["nginx", "-g", "daemon off;"]