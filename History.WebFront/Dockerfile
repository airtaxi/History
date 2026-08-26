FROM nginx:alpine

COPY default.conf.template /etc/nginx/templates/default.conf.template
COPY public /usr/share/nginx/html
COPY assets /usr/share/nginx/html/assets

# Expose the default nginx port
EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
