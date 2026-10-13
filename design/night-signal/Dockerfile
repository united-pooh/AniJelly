FROM nginxinc/nginx-unprivileged:stable-alpine@sha256:56b8272e43a0525f67aed68d1e0794a712d10e3e45a563e118e9a77c817e6f86

COPY nginx.conf /etc/nginx/nginx.conf
COPY ["夜航私人影院.html", "/srv/night-signal/index.html"]

USER 101:101
EXPOSE 8080
HEALTHCHECK --interval=15s --timeout=3s --start-period=5s --retries=3 \
    CMD wget --spider --quiet http://127.0.0.1:8080/ || exit 1

ENTRYPOINT ["nginx"]
CMD ["-g", "daemon off;"]
