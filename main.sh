#!/bin/sh

set -eu

COMMAND="${1:-}"
ENVIRONMENT="${2:-dev}"

SCRIPT_DIR="$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)"

case "${ENVIRONMENT}" in
    dev)
        COMPOSE_FILE="${SCRIPT_DIR}/docker-compose.dev.yml"
        REMOVE_VOLUMES=true
        NO_CACHE=true
        ;;

    prod)
        COMPOSE_FILE="${SCRIPT_DIR}/docker-compose.prod.yml"
        REMOVE_VOLUMES=false
        NO_CACHE=false
        ;;

    *)
        echo "Unknown environment: ${ENVIRONMENT}"
        echo "Expected: dev or prod"
        exit 1
        ;;
esac

if [ ! -f "${COMPOSE_FILE}" ]; then
    echo "Compose file not found: ${COMPOSE_FILE}"
    exit 1
fi

start_services() {
    echo "Starting MatosKC in ${ENVIRONMENT} mode..."

    if [ "${NO_CACHE}" = true ]; then
        docker compose \
            -f "${COMPOSE_FILE}" \
            build --no-cache

        docker compose \
            -f "${COMPOSE_FILE}" \
            up --detach
    else
        docker compose \
            -f "${COMPOSE_FILE}" \
            up --detach --build
    fi
}

stop_services() {
    echo "Stopping MatosKC in ${ENVIRONMENT} mode..."

    if [ "${REMOVE_VOLUMES}" = true ]; then
        docker compose \
            -f "${COMPOSE_FILE}" \
            down --volumes
    else
        docker compose \
            -f "${COMPOSE_FILE}" \
            down
    fi
}

case "${COMMAND}" in
    start)
        start_services
        ;;

    stop)
        stop_services
        ;;

    restart)
        stop_services
        start_services
        ;;

    *)
        echo "Usage: $0 {start|stop|restart} {dev|prod}"
        echo
        echo "Examples:"
        echo "  $0 start dev"
        echo "  $0 restart dev"
        echo "  $0 start prod"
        echo "  $0 stop prod"
        exit 1
        ;;
esac