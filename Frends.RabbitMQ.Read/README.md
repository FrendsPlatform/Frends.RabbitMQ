# Frends.RabbitMQ.Read

Frends Task for reading message(s) from a RabbitMQ queue.

[![Build status](https://github.com/FrendsPlatform/Frends.RabbitMQ/actions/workflows/Read_main_release.yml/badge.svg)](https://github.com/FrendsPlatform/Frends.RabbitMQ/actions/workflows/Read_main_release.yml)
![Code Coverage](https://app-github-custom-badges.azurewebsites.net/Badge?key=FrendsPlatform/Frends.RabbitMQ/Frends.RabbitMQ.Read|main)
[![Repository License](https://img.shields.io/github/license/FrendsPlatform/Frends.RabbitMQ?label=License)](https://github.com/FrendsPlatform/Frends.RabbitMQ/blob/main/LICENSE)

## Installing

You can install the Task via Frends UI Task View.

## Building

### Clone a copy of the repository

`git clone https://github.com/FrendsPlatform/Frends.RabbitMQ.git`

### Build the project

`dotnet build`

### Run tests

Tests start the required RabbitMQ containers themselves with Testcontainers, so only a running Docker daemon is needed.

`dotnet test`

### Create a NuGet package

`dotnet pack --configuration Release`
