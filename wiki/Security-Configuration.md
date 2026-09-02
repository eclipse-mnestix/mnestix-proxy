# Security Configuration

This document describes the available security configuration options for the mnestix-proxy project, focusing on integration with **Keycloak** and **Azure Entra ID**.

## API Key (Custom Endpoint Security)

The proxy protects modifying requests (POST, PUT, PATCH, DELETE) with an API key supplied via the `X-API-KEY`
header. GET/HEAD/OPTIONS requests do not require a key.

> **⚠️ SECURITY WARNING — CHANGE THE API KEY BEFORE DEPLOYING**
>
> The API key is **no longer set** in `appsettings.json` (it ships empty) so that no predictable secret is
> committed to the repository. The development Docker Compose file defaults to the well-known placeholder
> `verySecureApiKey`. **Do not deploy with an empty key or this placeholder** — anyone who knows the value can
> issue write/delete requests against every proxied repository (AAS shells, submodels, discovery, and the Mnestix
> API), which run without authentication of their own.
>
> At startup the application logs a **critical warning** whenever the key is empty or equals a known default.
>
> Set your own secret via the `CustomerEndpointsSecurity__ApiKey` environment variable (recommended), or configure
> the `CustomerEndpointsSecurity` section in `appsettings.json`:
>
> ```bash
> # Linux / macOS / Docker Compose
> export CustomerEndpointsSecurity__ApiKey='generate-a-long-random-secret-here'
>
> # Windows PowerShell
> $env:CustomerEndpointsSecurity__ApiKey='generate-a-long-random-secret-here'
> ```

## Keycloak

Keycloak is an open-source identity and access management solution. To enable Keycloak authentication in mnestix-proxy:

- **Configuration**:  
  Update your `appsettings.json` with the following section:
  ```json
  "OpenId": {
    "EnableOpenIdAuth": "true",
    "Issuer": "https://<keycloak-server>/realms/<realm-name>",
    "ClientID": "<client-id>",
    "RequireHttpsMetadata": "false"
  },
  ```
- **Usage**:  
  The proxy will validate JWT tokens issued by Keycloak. Ensure your clients obtain tokens from Keycloak and include them in the `Authorization: Bearer <token>` header.


## Azure Entra ID

Azure Entra ID provides cloud-based identity management. To enable Azure Entra ID authentication:

- **Configuration**:  
  Update your `appsettings.json` with the following section:
  ```json
  "AzureAd": {
    "EnableAzureAdAuth": "true",
    "Instance": "https://login.microsoftonline.com/",
    "ClientId": "<client-id>",
    "Domain": "<your-domain>", 
    "TenantId": "<tenant-id>"
  }
  ```
- **Usage**:  
  The proxy will validate JWT tokens issued by Azure Entra ID. Clients must authenticate with Azure Entra ID and include the token in the `Authorization` header.


## Additional Notes

- Both Keycloak and Azure Entra ID configurations rely on the standard ASP.NET Core authentication middleware.
- Ensure the `Audience` matches your application's client ID.
- For development, you may set `RequireHttpsMetadata` to `false`, but it is recommended to use `true` in production.

For more details, refer to the authentication setup in `Program.cs` and the `Authentication` folder.