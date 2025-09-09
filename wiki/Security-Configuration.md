# Security Configuration

This document describes the available security configuration options for the mnestix-proxy project, focusing on integration with **Keycloak** and **Azure Active Directory**.

## Keycloak

Keycloak is an open-source identity and access management solution. To enable Keycloak authentication in mnestix-proxy:

- **Configuration**:  
  Update your `appsettings.json` with the following section:
  ```json
  "OpenId": {
    "EnableOpenIdAuth": "false",
    "Issuer": "https://<keycloak-server>/realms/<realm-name>",
    "ClientID": "<client-id>",
    "RequireHttpsMetadata": "false"
  },
  ```
- **Usage**:  
  The proxy will validate JWT tokens issued by Keycloak. Ensure your clients obtain tokens from Keycloak and include them in the `Authorization: Bearer <token>` header.


## Azure Active Directory

Azure AD provides cloud-based identity management. To enable Azure AD authentication:

- **Configuration**:  
  Update your `appsettings.json` with the following section:
  ```json
  "AzureAd": {
    "EnableAzureAdAuth": "false",
    "Instance": "https://login.microsoftonline.com/",
    "ClientId": "<client-id>",
    "Domain": "<your-domain>", 
    "TenantId": "<tenant-id>"
  }
  ```
- **Usage**:  
  The proxy will validate JWT tokens issued by Azure AD. Clients must authenticate with Azure AD and include the token in the `Authorization` header.


## Additional Notes

- Both Keycloak and Azure AD configurations rely on the standard ASP.NET Core authentication middleware.
- Ensure the `Audience` matches your application's client ID.
- For development, you may set `RequireHttpsMetadata` to `false`, but it is recommended to use `true` in production.

For more details, refer to the authentication setup in `Program.cs` and the `Authentication` folder.