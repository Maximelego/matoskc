#!/bin/sh

set -eu

COMMAND="${1:-}"
ENVIRONMENT="${2:-dev}"

SCRIPT_DIR="$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)"

# Quality commands use the second argument as a target, rather than an environment.
run_quality_command() {
    TARGET="${2:-}"

    case "${TARGET}" in
        frontend)
            cd "${SCRIPT_DIR}/frontend/app"
            case "${COMMAND}" in
                build) npm run build ;;
                test) npm run test ;;
                format) npm run format ;;
                format-check) npm run format:check ;;
                lint) npm run lint ;;
            esac
            ;;

        backend)
            cd "${SCRIPT_DIR}/backend"
            case "${COMMAND}" in
                build) dotnet build MatosKC.slnx --configuration Release ;;
                test) dotnet test MatosKC.slnx --configuration Release ;;
                format)
                    dotnet format MatosKC.slnx \
                        --exclude src/MatosKC.Infrastructure/Persistence/Migrations
                    ;;
                format-check|lint)
                    dotnet format MatosKC.slnx --verify-no-changes --severity warn \
                        --exclude src/MatosKC.Infrastructure/Persistence/Migrations
                    ;;
            esac
            ;;

        *)
            echo "Unknown target: ${TARGET}"
            echo "Expected: frontend or backend"
            exit 1
            ;;
    esac
}

case "${COMMAND}" in
    build|test|format|format-check|lint)
        run_quality_command "$@"
        exit 0
        ;;
esac

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
        echo "       $0 {build|test|format|format-check|lint} {frontend|backend}"
        echo
        echo "Examples:"
        echo "  $0 start dev"
        echo "  $0 restart dev"
        echo "  $0 start prod"
        echo "  $0 stop prod"
        echo "  $0 format-check frontend"
        echo "  $0 test backend"
        exit 1
        ;;
esac
