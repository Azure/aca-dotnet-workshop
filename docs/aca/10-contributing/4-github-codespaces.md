# Inner loop, testing your changes using Github Codespaces

This repo has a github codespaces dev container defined, this container is based on ubuntu 20.04 and contains all the libraries and components to run github pages locally in Github Codespaces. To test your changes locally do the following:

- Enable [GitHub codespaces](https://github.com/features/codespaces) for your account
- Fork this repo
- Open the repo in github codespaces
- Wait for the container to build and connect to it
- Understand the folder structure of the Repo:
    - "docs/aca" folder , contains all the mark-down documentation files for all the challenges
    - "docs/assets" folder, contains all the images, slides, and files used in the lab
- Understand the index, title, and child metadata used by [Material for MkDocs](https://squidfunk.github.io/mkdocs-material/getting-started/) 

- Run the website in github codespaces using below command
    
    ```bash
    make docs-local-docker
    ```
![Enabling Codespace](../../assets/gifs/codespace.gif)