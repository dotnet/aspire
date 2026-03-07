#!/usr/bin/env bash

# Dev setup script for {{cookiecutter.project_slug}} agent

if ! command -v uv &> /dev/null
then
    echo "uv could not be found, try to install uv..."
    if command -v curl &> /dev/null
    then
        curl -LsSf https://astral.sh/uv/install.sh | sh
    else
        if command -v wget &> /dev/null
        then
            wget -qO- https://astral.sh/uv/install.sh | sh
        else
            echo "Neither curl nor wget could be found, please install one of them first."
            exit 1
        fi
    fi
fi

uv sync --frozen --locked