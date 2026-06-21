VERSION     := $(shell cat VERSION)
VERSION_NUM := $(shell sed 's/^v//' VERSION)

.PHONY: build run release

build:
	sed -i "s/__VERSION__/$(VERSION_NUM)/" manifest.json
	dotnet build -c Release || { sed -i "s/$(VERSION_NUM)/__VERSION__/" manifest.json; exit 1; }
	sed -i "s/$(VERSION_NUM)/__VERSION__/" manifest.json
	mkdir -p dist/LudoTrace
	cp bin/Release/net6.0/LudoTrace.dll dist/LudoTrace/
	sed "s/__VERSION__/$(VERSION_NUM)/" manifest.json > dist/LudoTrace/manifest.json

run:
	@echo "Install dist/LudoTrace/ into Stardew Valley/Mods/LudoTrace/ and launch via SMAPI."

release:
	@if ! git diff --quiet || ! git diff --cached --quiet; then \
		echo "Error: uncommitted changes — commit or stash before releasing"; exit 1; \
	fi
	@if [ "$$(git rev-parse --abbrev-ref HEAD)" != "main" ]; then \
		echo "Error: releases must be tagged from main"; exit 1; \
	fi
	git pull
	git tag $(VERSION)
	git push origin $(VERSION)
	@echo "Tagged $(VERSION) and pushed — GitHub Actions will build zip and upload to NexusMods"
