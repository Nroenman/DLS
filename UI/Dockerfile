FROM nginx:alpine

COPY . /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf

ENV KEYCLOAK_URL=http://localhost:8080

EXPOSE 80

CMD ["/bin/sh", "-c", \
  "sed -i \"s|http://localhost:8080|${KEYCLOAK_URL}|g\" \
    /usr/share/nginx/html/pages/login.html \
    /usr/share/nginx/html/pages/callback.html && \
  exec nginx -g 'daemon off;'"]
