# Getting Started with Developing mnestix-proxy

## Prerequisites

Before you start developing with **mnestix-proxy**, ensure you have the following installed:

- **.NET 8 SDK** (required for building and running the project)
- **Git** (for version control)
- **Docker** (optional, for containerized development and testing)

## Project Structure

The main source code is located in the `mnestix-proxy` folder. Key directories and files include:

- `Program.cs` — Application entry point
- `Authentication/` — Authentication logic and middleware
- `Configuration/` — Configuration options and classes
- `Exceptions/` — Custom exception types
- `Middleware/` — Custom ASP.NET Core middleware
- `Services/` — Service implementations and interfaces
- `appsettings.json` / `appsettings.Development.json` — Configuration files
- `.github/workflows/` — CI/CD workflows

## Setup Instructions

1. **Clone the repository:**
   ```sh
   git clone https://github.com/eclipse-mnestix/mnestix-proxy.git
   cd mnestix-proxy/mnestix-proxy
   ```

2. **Install dependencies:**
   - Make sure you have the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) installed.

3. **Configure environment variables:**
   - Update `appsettings.Development.json` or `appsettings.json` as needed for your environment.

4. **Run the development server:**
   ```sh
   dotnet run
   ```

5. **Run tests:**
   ```sh
   dotnet test
   ```

## Docker Development (Optional)

- Build and run the project using Docker:
  ```sh
  docker build -t mnestix-proxy .
  docker run -p 5000:5000 mnestix-proxy
  ```
- See `.dockerignore` and `docker-compose.yml` (if present) for details.

## Additional Notes

- Use `dotnet restore` if you encounter missing dependencies.
- Pull the latest changes from the main branch before starting new work.

## Useful Links

- [.NET 8 SDK Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Visual Studio Code](https://code.visualstudio.com/)
- [JetBrains Rider](https://www.jetbrains.com/rider/)
- [Git Download](https://git-scm.com/)
