FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG TARGETARCH
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["mnestix-proxy/mnestix-proxy.csproj", "mnestix-proxy/"]
RUN dotnet restore "./mnestix-proxy/mnestix-proxy.csproj"
COPY . .
WORKDIR "/src/mnestix-proxy"
RUN dotnet build "./mnestix-proxy.csproj" -c $BUILD_CONFIGURATION -o /app/build -a $TARGETARCH

FROM build AS publish
ARG TARGETARCH
ARG BUILD_CONFIGURATION=Release
WORKDIR "/src/mnestix-proxy"
RUN dotnet publish "./mnestix-proxy.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false -a $TARGETARCH

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "mnestix-proxy.dll"]