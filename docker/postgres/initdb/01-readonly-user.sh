#!/bin/bash
# Creates the login used by text-to-SQL. Runs once, when the PostgreSQL volume is first created.
#
# - analytics_reader: NOLOGIN group role; the EF Core migration grants it SELECT on the business tables.
# - copilot_readonly: the login the API uses for LLM-generated SQL. It inherits analytics_reader, and
#   every session is read-only with a statement timeout, even if the application forgot to set them.
set -euo pipefail

psql -v ON_ERROR_STOP=1 \
    --username "$POSTGRES_USER" \
    --dbname "$POSTGRES_DB" \
    -v readonly_password="${POSTGRES_READONLY_PASSWORD:-copilot_readonly}" <<'EOSQL'
CREATE ROLE analytics_reader NOLOGIN;
CREATE ROLE copilot_readonly LOGIN PASSWORD :'readonly_password' IN ROLE analytics_reader;
ALTER ROLE copilot_readonly SET default_transaction_read_only = on;
ALTER ROLE copilot_readonly SET statement_timeout = '5s';
EOSQL
