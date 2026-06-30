# Convenience targets for the dev / test / prod Docker stacks.
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
# Each environment reads env/<env>.env (copied from env/<env>.env.example).

ENVS := dev test prod
ENV  ?= dev

# docker compose invocation for a given environment ($(1) = dev|test|prod)
compose = docker compose -f docker-compose.yml -f docker-compose.$(1).yml -p ticketing-$(1) --env-file env/$(1).env

# Fail early with a friendly message if the env file is missing.
ensure_env = @test -f env/$(1).env || { echo "Missing env/$(1).env — copy env/$(1).env.example to it and fill in values."; exit 1; }

.DEFAULT_GOAL := help

.PHONY: help
help: ## Show this help
	@echo "Targets:"
	@echo "  make <env>-up | <env>-down | <env>-down-v | <env>-logs | <env>-build | <env>-ps"
	@echo "  make up | down | logs | build | ps   (ENV=dev|test|prod, default dev)"
	@echo ""
	@echo "Environments: $(ENVS)"

# ---- Generic, parameterised by ENV -------------------------------------------
.PHONY: up down down-v logs build ps
up:      ; $(call ensure_env,$(ENV)) ; $(call compose,$(ENV)) up --build -d
down:    ; $(call compose,$(ENV)) down
down-v:  ; $(call compose,$(ENV)) down -v
logs:    ; $(call compose,$(ENV)) logs -f
build:   ; $(call ensure_env,$(ENV)) ; $(call compose,$(ENV)) build
ps:      ; $(call compose,$(ENV)) ps

# ---- Explicit per-environment targets ----------------------------------------
define ENV_TARGETS
.PHONY: $(1)-up $(1)-down $(1)-down-v $(1)-logs $(1)-build $(1)-ps
$(1)-up:     ; $$(call ensure_env,$(1)) ; $$(call compose,$(1)) up --build -d
$(1)-down:   ; $$(call compose,$(1)) down
$(1)-down-v: ; $$(call compose,$(1)) down -v
$(1)-logs:   ; $$(call compose,$(1)) logs -f
$(1)-build:  ; $$(call ensure_env,$(1)) ; $$(call compose,$(1)) build
$(1)-ps:     ; $$(call compose,$(1)) ps
endef

$(foreach e,$(ENVS),$(eval $(call ENV_TARGETS,$(e))))
