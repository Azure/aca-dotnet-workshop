build-docs:
	@$(MAKE) build-docs-website

build-docs-website:
	mkdir -p dist
	docker pull squidfunk/mkdocs-material:latest
	docker build -t squidfunk/mkdocs-material:latest ./docs/
	docker run --rm -t -v ${PWD}:/docs squidfunk/mkdocs-material build
	cp -R site/* dist/

docs-local:
	docker pull squidfunk/mkdocs-material:latest
	docker build -t squidfunk/mkdocs-material:latest ./docs/
	docker run --rm -it -p 8000:8000 -v ${PWD}:/docs squidfunk/mkdocs-material