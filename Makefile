# Unified build and test entrypoint for the repository.
#
# Usage examples:
#   make                # show help
#   make restore        # restore NuGet + npm dependencies
#   make build          # Debug build (default CONFIG)
#   make run            # run game (Debug by default)
#   make test           # run all tests
#   make publish        # publish game (framework-dependent)
#   make publish-linux-x64
#   make publish-win-x64

.DEFAULT_GOAL := help

# Project paths.
GAME_PROJECT := isometric-magic.csproj
ENGINE_TEST_PROJECT := tests/IsometricMagic.Engine.Tests/IsometricMagic.Engine.Tests.csproj
EDITOR_TEST_PROJECT := tests/IsometricMagic.RuntimeEditor.Tests/IsometricMagic.RuntimeEditor.Tests.csproj
SPA_DIR := Editor/IsometricMagic.RuntimeEditor/Web/spa
PAK_TOOL_PROJECT := Tools/IsometricMagic.PakTool/IsometricMagic.PakTool.csproj
RESOURCES_DIR := resources
PAKS_DIR := resources/paks
PAK_OUT ?= $(PAKS_DIR)/base.pak
PAK_IN ?= $(PAK_OUT)
PAK_UNPACK_DIR ?= artifacts/pak-unpacked

# Build configuration (can be overridden: make build CONFIG=Release).
CONFIG ?= Debug

# Standard artifact layout (best practice): all generated distributables under artifacts/.
ARTIFACTS_DIR := artifacts
PUBLISH_DIR := $(ARTIFACTS_DIR)/publish
PUBLISH_DEFAULT_DIR := $(PUBLISH_DIR)/framework-dependent
PUBLISH_LINUX_X64_DIR := $(PUBLISH_DIR)/linux-x64
PUBLISH_WIN_X64_DIR := $(PUBLISH_DIR)/win-x64

.PHONY: help restore restore-dotnet restore-spa spa-build build run clean \
	test-engine test-editor test verify normals atlases assets pak pak-list pak-unpack publish publish-linux-x64 publish-win-x64

help: ## Show available Makefile targets
	@printf "Available targets:\n"
	@awk 'BEGIN {FS = ":.*## "} /^[a-zA-Z0-9_.-]+:.*## / {printf "  %-22s %s\n", $$1, $$2}' Makefile

restore: restore-dotnet restore-spa ## Restore NuGet and npm dependencies

restore-dotnet: ## Restore NuGet dependencies
	dotnet restore $(GAME_PROJECT)

restore-spa: ## Install SPA dependencies with npm ci
	npm ci --prefix "$(SPA_DIR)"

spa-build: ## Build runtime editor SPA bundle
	npm run build --prefix "$(SPA_DIR)"

build: ## Build game project (Debug by default)
	@if [ "$(CONFIG)" = "Debug" ]; then $(MAKE) spa-build; fi
	dotnet build -c $(CONFIG) $(GAME_PROJECT)

run: ## Run game project (Debug by default)
	$(MAKE) atlases
	@if [ "$(CONFIG)" = "Debug" ]; then $(MAKE) spa-build; fi
	dotnet run -c $(CONFIG) --project $(GAME_PROJECT)

clean: ## Clean project
	dotnet clean -c $(CONFIG) $(GAME_PROJECT)
	@rm -rf artifacts bin logs obj Editor/bin Editor/obj Editor/Web/dist Engine/bin Engine/obj

test-engine: ## Run engine tests
	dotnet test -c $(CONFIG) $(ENGINE_TEST_PROJECT)

test-editor: ## Run runtime editor tests
	$(MAKE) spa-build
	dotnet test -c $(CONFIG) $(EDITOR_TEST_PROJECT)

test: test-engine test-editor ## Run all tests

verify: build test ## Build and run all tests

normals: ## Generate normal maps from normalmap project
	dotnet run --project Tools/IsometricMagic.NormalMapGenerator -- --project resources/pipeline/normalmaps/normalmap.project.json

atlases: ## Pack atlases from atlas project
	dotnet run --project Tools/IsometricMagic.AtlasPacker -- --project resources/pipeline/atlases/pack.project.json

assets: normals atlases ## Generate normals then atlases

pak: ## Build pak from resources/data + resources/_gen (PAK_OUT=...)
	@mkdir -p "$(PAKS_DIR)"
	dotnet run --project "$(PAK_TOOL_PROJECT)" -- pack --input "$(RESOURCES_DIR)" --out "$(PAK_OUT)"

pak-list: ## List entries from pak file (PAK_IN=...)
	dotnet run --project "$(PAK_TOOL_PROJECT)" -- list --input "$(PAK_IN)"

pak-unpack: ## Unpack pak file to directory (PAK_IN=..., PAK_UNPACK_DIR=...)
	@mkdir -p "$(PAK_UNPACK_DIR)"
	dotnet run --project "$(PAK_TOOL_PROJECT)" -- unpack --input "$(PAK_IN)" --out "$(PAK_UNPACK_DIR)"

publish: ## Publish game (Release, framework-dependent)
	dotnet publish -c Release $(GAME_PROJECT) -o "$(PUBLISH_DEFAULT_DIR)"

publish-linux-x64: ## Publish game for linux-x64
	dotnet publish -c Release -r linux-x64 $(GAME_PROJECT) -o "$(PUBLISH_LINUX_X64_DIR)"

publish-win-x64: ## Publish game for win-x64
	dotnet publish -c Release -r win-x64 $(GAME_PROJECT) -o "$(PUBLISH_WIN_X64_DIR)"
