# Docker Compose Setup for mnestix-proxy

This guide explains how to use Docker Compose to run the full Mnestix Proxy stack, including the proxy, the AAS Generator, and the Eclipse BaSyx AAS environment backed by PostgreSQL.

---

## Compose Files

- **compose.yml**: For production or standard usage. Uses pre-built images.
- **compose.dev.yml**: For development. Builds the `mnestix-proxy` image from your local `Dockerfile` and publishes the backend service ports to the host.

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

This builds the `mnestix-proxy` image from your local `Dockerfile` and starts all services. The development compose file also publishes the backend service ports (`mnestix-aas-generator` on `5064`, `aas-environment` on `8081`, `basyx-db` on `5432`) to the host for easier debugging.

### Profiles

The BaSyx backend services (`basyx-db`, `basyx-configuration`, `aas-environment`) are grouped under Compose profiles: `basyx` and `tests` (and the default profile). Select a profile with:

```sh
docker compose --profile basyx up
```

---

## Services Overview

- **mnestix-proxy**: Main reverse proxy gateway (`5065:5065`, image `mnestix/mnestix-proxy`)
- **mnestix-aas-generator**: AAS Generator service, listens on `5064` internally (image `mnestix/mnestix-aas-generator`)
- **basyx-db**: PostgreSQL database backing the AAS environment (image `postgres:16-alpine`)
- **basyx-configuration**: Eclipse BaSyx configuration service (runs once to initialise the database)
- **aas-environment**: Eclipse BaSyx AAS environment on `8081`, serving the AAS repository, discovery, AAS registry and submodel registry (image `eclipsebasyx/aasenvironment-go`)

All services are connected via the `mnestix-network` Docker network.

---

## Accessing Services

With the standard setup, only the proxy port is published to the host:

- **Proxy**: [http://localhost:5065](http://localhost:5065)

The proxy forwards to the backend services on the internal Docker network, e.g.:

- **AAS Generator**: `http://mnestix-aas-generator:5064/`
- **AAS Environment (repo / discovery / registries)**: `http://aas-environment:8081/`

With the development setup (`compose.dev.yml`), the backend ports are also published to the host:

- **AAS Generator**: [http://localhost:5064](http://localhost:5064)
- **AAS Environment**: [http://localhost:8081](http://localhost:8081)
- **PostgreSQL (basyx-db)**: `localhost:5432`

---

## Environment Variables

You can override default settings using environment variables, e.g.:
- `MNESTIX_BACKEND_API_KEY`: API key for secured endpoints (defaults to `verySecureApiKey`)

Cluster destinations are also set via environment variables on the `mnestix-proxy` service, e.g. `ReverseProxy__Clusters__mnestixApiCluster__Destinations__destination1__Address`. See `compose.yml` for all configurable variables.

---

## Stopping the Stack

To stop and remove all containers:

```sh
docker compose down
```

To also remove the database volume:

```sh
docker compose down -v
```

---

## Troubleshooting

- View logs: `docker compose logs`
- Check health status: `aas-environment` and `basyx-db` define health checks to ensure readiness.
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
