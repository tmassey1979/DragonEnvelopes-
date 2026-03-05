#!/bin/bash
set -e

APP_DB="${APP_DB_NAME:-dragonenvelopes_app}"
KC_DB="${KEYCLOAK_DB_NAME:-keycloak}"

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-EOSQL
  SELECT 'CREATE DATABASE "$APP_DB"'
  WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = '$APP_DB')\gexec
  SELECT 'CREATE DATABASE "$KC_DB"'
  WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = '$KC_DB')\gexec
EOSQL
