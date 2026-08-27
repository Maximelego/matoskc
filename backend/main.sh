#!/bin/bash

echo "Installing packages..."
pip install -r ./requirements.txt

echo "Launching API..."
exec uvicorn app.main:app --host 0.0.0.0 --port 8000 --reload --proxy-headers --log-level debug
