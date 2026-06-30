# Convenience targets for the dev / test / prod Docker stacks.
#
# Shell-agnostic: every recipe is a single `docker compose` invocation, and the
# env-file check uses make's own $(wildcard)/$(error) (evaluated by make, not the
# shell), so these targets work whether make runs under cmd.exe (Windows), Git
# Bash, or a POSIX shell on Linux/macOS.
#
# Per-environment targets (recommended):
#   make dev-up        build + start the dev stack (detached)
#   make dev-down      stop the dev stack
#   make dev-down-v    stop the dev stack AND remove its database volume
#   make dev-logs      follow logs
#   make dev-build     build images only
#   make dev-ps        list containers
#   (same for test-* and prod-*)
#
# Generic form (ENV defaults to dev):
#   make up ENV=prod
#   make down ENV=test
#
# Each environment reads env/<env>.env (copy it from env/<env>.env.example first).

ENVS := dev test prod
ENV  ?= dev

# docker compose invocation for a given environment ($(1) = dev|test|prod)
compose = docker compose -f docker-compose.yml -f docker-compose.$(1).yml -p ticketing-$(1) --env-file env/$(1).env

# Abort (via make itself, before any shell runs) if the env file is missing.
# Expands to nothing when the file exists, so it can directly prefix a recipe.
ensure_env = $(if $(wildcard env/$(1).env),,$(error Missing env/$(1).env — run: cp env/$(1).env.example env/$(1).env))

.DEFAULT_GOAL := help

.PHONY: help
help: ; @echo Targets: dev/test/prod each with -up -down -down-v -logs -build -ps. Example: make dev-up or make up ENV=prod

# ---- Generic, parameterised by ENV -------------------------------------------
.PHONY: up down down-v logs build ps
up:      ; $(call ensure_env,$(ENV))$(call compose,$(ENV)) up --build -d
down:    ; $(call compose,$(ENV)) down
down-v:  ; $(call compose,$(ENV)) down -v
logs:    ; $(call compose,$(ENV)) logs -f
build:   ; $(call ensure_env,$(ENV))$(call compose,$(ENV)) build
ps:      ; $(call compose,$(ENV)) ps

# ---- Explicit per-environment targets ----------------------------------------
define ENV_TARGETS
.PHONY: $(1)-up $(1)-down $(1)-down-v $(1)-logs $(1)-build $(1)-ps
$(1)-up:     ; $$(call ensure_env,$(1))$$(call compose,$(1)) up --build -d
$(1)-down:   ; $$(call compose,$(1)) down
$(1)-down-v: ; $$(call compose,$(1)) down -v
$(1)-logs:   ; $$(call compose,$(1)) logs -f
$(1)-build:  ; $$(call ensure_env,$(1))$$(call compose,$(1)) build
$(1)-ps:     ; $$(call compose,$(1)) ps
endef

$(foreach e,$(ENVS),$(eval $(call ENV_TARGETS,$(e))))
