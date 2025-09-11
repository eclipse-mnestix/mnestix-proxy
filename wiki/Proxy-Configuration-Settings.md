# YARP Proxy Configuration Documentation
The features of Mnestix-Proxy are configured in the `appsettings.json` file. This configuration sets up the reverse proxy using [YARP](https://microsoft.github.io/reverse-proxy/), defining routing, clusters, authentication, and feature flags.

## Sections Overview

### CustomerEndpointsSecurity
Defines the API key required for custom endpoint security.

### Authentication
- **AzureAd**: Settings for Azure EntraId authentication.
- **OpenId**: Settings for OpenID authentication.
- If both are enabled, OpenID takes precedence.

### Features
- `AllowRetrievingAllShellsAndSubmodels`: Enables retrieval of all shells and submodels.
- `AasDiscoveryMiddleware`: Enables AAS discovery middleware.

---

## ReverseProxy

### Routes

Defines how incoming requests are matched and routed to clusters:

- **MnestixApiRoute** - This is connection to Mnestix API (Templates)
  - Path: `api/{**catch-all}`
  - Cluster: `mnestixApiCluster`
  - Authorization: `customApiKeyToModifyValuesPolicy`

- **EnvironmentRoute** - This route is configuration to aas repository.
  - Path: `repo/{**catch-all}`
  - Cluster: `aasRepoCluster`
  - Authorization: `customApiKeyToModifyValuesPolicy`
  - Transforms: Path pattern and CORS header

- **SubmodelRepositoryRoute** - This route is configuration to submodel repository.
  - Path: `repo/submodels/{**remainder}`
  - Cluster: `submodelRepoCluster`
  - Authorization: `customApiKeyToModifyValuesPolicy`
  - Transforms: Path pattern and CORS header

- **DiscoveryRoute** - This route is configuration to discovery service (required for 'AasDiscoveryMiddleware').
  - Path: `discovery/{**catch-all}`
  - Cluster: `discoveryCluster`
  - Authorization: `customApiKeyToModifyValuesPolicy`
  - Transforms: Path pattern and CORS header

- **InfluxRoute** - This route is configuration (for time series influx db). Hides influx settings from browser entirely.
  - Path: `influx/{**catch-all}`
  - Cluster: `influxCluster`
  - CORS Policy: `allowAnything`
  - Transforms: Path pattern and sets Authorization header for InfluxDB

### Clusters

Defines backend destinations for each route:

- **mnestixApiCluster**: `http://localhost:5064/`
- **aasRepoCluster**: `http://localhost:8081/`
- **submodelRepoCluster**: `http://localhost:8081/`
- **discoveryCluster**: `http://localhost:8082/`
- **influxCluster**: `http://<your-domain>:<port>`

---

## Security

- API key required for modifying values via custom policy.
- CORS headers are set for certain routes to allow cross-origin requests.
- InfluxDB route sets a specific Authorization token.

---

## Usage

- Modify cluster addresses to point to your backend services.
- Adjust authentication settings as needed.
- Use feature flags to enable/disable specific proxy features.