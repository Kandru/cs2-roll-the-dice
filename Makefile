PROJECT   := src/RollTheDice.csproj
SDK_IMAGE := mcr.microsoft.com/dotnet/sdk:10.0
UID       := $(shell id -u)
GID       := $(shell id -g)
DEBUG_OUTPUT := $(shell sed -n 's/.*<OutputPath>\(.*\)<\/OutputPath>.*/\1/p' $(PROJECT))

# Optional absolute OutputPath in the csproj (for example a local game-server
# plugins folder). When unset, build output stays under the repo bind mount.
ifneq ($(DEBUG_OUTPUT),)
OUTPUT_VOLUME := -v "$(DEBUG_OUTPUT)":"$(DEBUG_OUTPUT)"
endif

# Run the SDK as the host user so files written to bind mounts are not root-owned.
DOCKER_RUN = docker run --rm \
	--user $(UID):$(GID) \
	-e HOME=/tmp \
	-e DOTNET_CLI_HOME=/tmp \
	-e DOTNET_CLI_TELEMETRY_OPTOUT=1 \
	-v "$(CURDIR)":/src \
	-v "$(HOME)/.nuget":/tmp/.nuget \
	$(OUTPUT_VOLUME) \
	-w /src \
	$(SDK_IMAGE)

.DEFAULT_GOAL := help

.PHONY: help debug release restore clean dirs panorama panorama-gen preview

help: ## Show this help
	@echo "CS2 Roll The Dice"
	@echo ""
	@echo "Usage: make <target>"
	@echo ""
	@awk 'BEGIN {FS = ":.*?## "} /^[a-zA-Z_-]+:.*?## / {printf "  \033[36m%-12s\033[0m %s\n", $$1, $$2}' $(MAKEFILE_LIST)

dirs:
	@mkdir -p "$(HOME)/.nuget"
ifneq ($(DEBUG_OUTPUT),)
	@mkdir -p "$(DEBUG_OUTPUT)"
endif

debug: dirs ## Build Debug (development game server)
	$(DOCKER_RUN) dotnet build $(PROJECT) -c Debug

release: dirs ## Publish Release (production game server)
	$(DOCKER_RUN) dotnet publish $(PROJECT) -c Release

restore: dirs ## Restore NuGet packages
	$(DOCKER_RUN) dotnet restore $(PROJECT)

clean: dirs ## Remove build artifacts
	$(DOCKER_RUN) dotnet clean $(PROJECT)

panorama: ## Validate HUD Panorama XML and CSS
	python3 tools/validate_panorama.py

panorama-gen: ## Regenerate the reel rows (banner.xml), the info menu (info_v4.xml) and their theme rules
	python3 tools/gen_reel_panorama.py
	python3 tools/gen_info_panorama.py

preview: ## Serve HUD browser preview (workshop/preview)
	@echo "Open http://127.0.0.1:8765/banner.html"
	@echo "Open http://127.0.0.1:8765/reel.html"
	python3 -m http.server 8765 --directory workshop/preview
