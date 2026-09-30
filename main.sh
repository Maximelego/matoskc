#!/bin/sh

set -eu

COMMAND="${1:-}"
ENVIRONMENT="${2:-dev}"

SCRIPT_DIR="$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)"

case "${ENVIRONMENT}" in
    dev)
        COMPOSE_FILE="${SCRIPT_DIR}/docker-compose.dev.yml"
        DEV_DATABASE_VOLUME="matoskc_postgres_data_dev"
        ;;

    prod)
        COMPOSE_FILE="${SCRIPT_DIR}/docker-compose.prod.yml"
        DEV_DATABASE_VOLUME=""
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

    docker compose \
        -f "${COMPOSE_FILE}" \
        up --detach --build
}

stop_services() {
    echo "Stopping MatosKC in ${ENVIRONMENT} mode..."

    docker compose \
        -f "${COMPOSE_FILE}" \
        down
}

reset_dev_database() {
    if [ "${ENVIRONMENT}" != "dev" ]; then
        echo "Database reset is only available in dev mode."
        exit 1
    fi

    echo "Removing development database volume ${DEV_DATABASE_VOLUME}..."

    if docker volume inspect "${DEV_DATABASE_VOLUME}" >/dev/null 2>&1; then
        docker volume rm "${DEV_DATABASE_VOLUME}"
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

        if [ "${ENVIRONMENT}" = "dev" ]; then
            reset_dev_database
        fi

        start_services
        ;;

    reset-db)
        if [ "${ENVIRONMENT}" != "dev" ]; then
            echo "Database reset is only available in dev mode."
            exit 1
        fi

        stop_services
        reset_dev_database
        start_services
        ;;

    *)
        echo "Usage: $0 {start|stop|restart|reset-db} {dev|prod}"
        echo
        echo "Examples:"
        echo "  $0 start dev"
        echo "  $0 restart dev"
        echo "  $0 reset-db dev"
        echo "  $0 start prod"
        echo "  $0 stop prod"
        exit 1
        ;;
esac
