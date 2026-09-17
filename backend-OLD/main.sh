#!/bin/sh

set -eu

VENV_PATH="/opt/venv"

echo "Preparing virtual environment..."

if [ ! -x "${VENV_PATH}/bin/python" ]; then
    python -m venv "${VENV_PATH}"
fi

"${VENV_PATH}/bin/python" -m pip install --disable-pip-version-check \
    -r /app/requirements.txt

echo "Launching API..."

exec "${VENV_PATH}/bin/python" -m uvicorn \
    app.main:app \
    --host 0.0.0.0 \
    --port 8000 \
    --reload \
    --proxy-headers \
    --log-level debug