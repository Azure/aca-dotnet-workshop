---
title: Variables
parent: Appendix
has_children: false
nav_order: 2
canonical_url: 'https://azure.github.io/aca-dotnet-workshop'
---

# Inner loop, testing your changes locally or using GitHub Codespaces

- `docs/aca` folder , contains all the mark-down documentation files for all the modules
- `docs/assets` folder, contains all the images, slides, and files used in the lab
- This site uses, [Material for MkDocs](https://squidfunk.github.io/mkdocs-material/getting-started/){target=_blank}.
Take some time to familiarize yourself with the theme and the features it provides.

### Locally

Checkout the repo locally using below command:

```bash
git clone  https://github.com/Azure/aca-dotnet-workshop.git
```

Using bash terminal or wsl terminal, navigate to the repo root folder and run the below command to build and run the website locally:

```bash
make docs-local
```

### Using GitHub Codespaces

This repo has a github codespaces dev container defined. This container is based on ubuntu 20.04 and contains all the libraries and components to run github pages locally in Github Codespaces. To test your changes follow these steps:

- Enable [GitHub codespaces](https://github.com/features/codespaces){target=_blank} for your account
- Fork this repo
- Open the repo in github codespaces
- Wait for the container to build and connect to it
- Run the website in github codespaces using below command

  ```bash
  make docs-local
  ```

![Enabling Codespace](../../assets/gifs/codespace.gif)
