# Usar una imagen base de Nginx con soporte para Brotli
FROM nginx:alpine

# Reemplazar la configuración por defecto de Nginx para servir WebGL correctamente
RUN rm /etc/nginx/conf.d/default.conf
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Copiar los archivos de la build generada por GitHub Actions
COPY build/WebGL/swordofshadows /usr/share/nginx/html/swordofshadows

# Exponer el puerto 80 para servir el juego
EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
