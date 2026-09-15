SHELL := /usr/bin/env bash

API_DIR := apps/api
WEB_DIR := apps/web

.DEFAULT_GOAL := help
.PHONY: help setup db-up db-down db-logs db-migrate migration api-restore api-build api web-install web-build web-lint web check dev clean

help:
	@printf '%s\n' \
		'make setup        Install dependencies, start PostgreSQL, and apply migrations' \
		'make dev          Start PostgreSQL, API, and web app together' \
		'make db-up        Start the PostgreSQL container' \
		'make db-down      Stop the PostgreSQL container' \
		'make db-logs      Follow PostgreSQL logs' \
		'make db-migrate   Apply committed EF Core migrations' \
		'make migration name=AddSomething  Create a new EF Core migration' \
		'make api          Run the ASP.NET Core API' \
		'make web          Run the Vite development server' \
		'make check        Build API and run frontend lint/build' \
		'make clean        Remove generated API and frontend build output'

setup: web-install api-restore db-up db-migrate
	@if [ ! -f $(WEB_DIR)/.env ]; then cp $(WEB_DIR)/.env.example $(WEB_DIR)/.env; printf '%s\n' 'Created apps/web/.env from .env.example'; fi

db-up:
	docker compose up -d

db-down:
	docker compose down

db-logs:
	docker compose logs -f postgres

db-migrate:
	dotnet ef database update --project $(API_DIR) --startup-project $(API_DIR)

migration:
	@test -n "$(name)" || (printf '%s\n' 'Usage: make migration name=AddMeaningfulName' >&2; exit 1)
	dotnet ef migrations add "$(name)" --project $(API_DIR) --startup-project $(API_DIR)

api-restore:
	dotnet restore $(API_DIR)

api-build:
	dotnet build $(API_DIR)

api:
	dotnet run --project $(API_DIR)

web-install:
	cd $(WEB_DIR) && pnpm install

web-build:
	cd $(WEB_DIR) && pnpm build

web-lint:
	cd $(WEB_DIR) && pnpm lint

web:
	cd $(WEB_DIR) && pnpm dev

check: api-build web-lint web-build

dev: db-up db-migrate
	@trap 'kill 0' INT TERM EXIT; \
	(dotnet run --project $(API_DIR)) & \
	(cd $(WEB_DIR) && pnpm dev) & \
	wait

clean:
	dotnet clean $(API_DIR)
	rm -rf $(WEB_DIR)/dist
