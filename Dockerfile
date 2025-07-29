FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG TARGETARCH
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["mnestix-proxy/mnestix-proxy.csproj", "mnestix-proxy/"]
RUN dotnet restore "./mnestix-proxy/mnestix-proxy.csproj"
COPY . .
WORKDIR "/src/mnestix-proxy"
RUN dotnet build "./mnestix-proxy.csproj" -c $BUILD_CONFIGURATION -o /app/build -a $TARGETARCH

# arm64 not supported by ephemeral-mongo
# https://github.com/asimmon/ephemeral-mongo/issues/3
RUN ARCH=$(uname -m); \
    if [ "$ARCH" = "x86_64" ]; then \
        # manually install libssl1.1 in the Docker container to ensure that our tests can run successfully.
        # The .NET SDK 8.0 Docker image has been updated to Debian 12 (Bookworm), which no longer includes libssl1.1 by default.
        # libssl1.1 is required for EphemeralMongo tests to run in the Docker container, as it is needed to establish secure communication with MongoDB.
        # Therefore, we need to manually install libssl1.1 in the Docker container to ensure that our tests can run successfully.
        wget http://security.ubuntu.com/ubuntu/pool/main/o/openssl/libssl1.1_1.1.1f-1ubuntu2.24_amd64.deb; \
        dpkg -i libssl1.1_1.1.1f-1ubuntu2.24_amd64.deb && rm libssl1.1_1.1.1f-1ubuntu2.24_amd64.deb; \
        dotnet test --logger "trx;LogFileName=TestResults.trx"; \
    fi

FROM build AS publish
ARG TARGETARCH
ARG BUILD_CONFIGURATION=Release
WORKDIR "/src/mnestix-proxy"
RUN dotnet publish "./mnestix-proxy.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false -a $TARGETARCH

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "mnestix-proxy.dll"]