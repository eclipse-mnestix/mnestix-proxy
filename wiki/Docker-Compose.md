# Docker Compose Setup for mnestix-proxy

This guide explains how to use Docker Compose to run the full Mnestix Proxy stack, including the proxy, API, AAS environment, discovery, registry, and MongoDB.

---

## Compose Files

- **compose.yml**: For production or standard usage. Uses pre-built images.
- **compose.dev.yml**: For development. Builds the `mnestix-proxy` image from your local source.

---

## Prerequisites

- [Docker](https://docs.docker.com/get-docker/) installed
- [Docker Compose](https://docs.docker.com/compose/install/) (or use `docker compose` command with Docker Desktop)

---

## Usage

### 1. Standard Setup

From the project root, run:

```sh
docker compose up
```

This will start all services using pre-built images.

### 2. Development Setup

To build and run the proxy from your local source (using `compose.dev.yml`):

```sh
docker compose -f compose.yml -f compose.dev.yml up --build
```

This will build the `mnestix-proxy` image from your local Dockerfile and start all services. The development compose file also exposes all service ports for easier debugging.

---

## Services Overview

- **mnestix-proxy**: Main reverse proxy gateway (`5065:5065`)
- **mnestix-api**: Backend API service (`5064:5064`)
- **mongodb**: MongoDB database for AAS and discovery services (`27017:27017`)
- **aas-environment**: Eclipse BaSyx AAS environment (repository) (`8081:8081`)
- **aas-discovery**: Eclipse BaSyx AAS discovery service (`8082:8081`)
- **aas-registry**: Eclipse BaSyx AAS registry (`8083:8080`)
- **submodel-registry**: Eclipse BaSyx submodel registry (`8084:8080`)

All services are connected via the `mnestix-network` Docker network.

---

## Accessing Services

- **Proxy**: [http://localhost:5065](http://localhost:5065)
- **API**: [http://localhost:5064](http://localhost:5064)
- **AAS Environment**: [http://localhost:8081](http://localhost:8081)
- **AAS Discovery**: [http://localhost:8082](http://localhost:8082)
- **AAS Registry**: [http://localhost:8083](http://localhost:8083)
- **Submodel Registry**: [http://localhost:8084](http://localhost:8084)
- **MongoDB**: [localhost:27017](http://localhost:27017)

---

## Environment Variables

You can override default settings using environment variables, e.g.:
- `MNESTIX_BACKEND_API_KEY`: API key for secured endpoints

See `compose.yml` and `compose.dev.yml` for all configurable variables.

---

## Stopping the Stack

To stop and remove all containers:

```sh
docker compose down
```

---

## Troubleshooting

- View logs: `docker compose logs`
- Check health status: Services use health checks to ensure readiness.
- Make sure required ports are available and not blocked.

---

## Customization

- Edit `compose.yml` or `compose.dev.yml` to change ports, images, or environment variables.
- Update `mnestix-proxy/appsettings.json` for proxy configuration.

---

## References

- [compose.yml](../compose.yml)
- [compose.dev.yml](../compose.dev.yml)
- [mnestix-proxy/appsettings.json](../mnestix-proxy/appsettings.json)