# --------- build ----------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

ARG GH_OWNER
ARG GH_USER
ARG GH_TOKEN

# Add GitHub Packages NuGet source with credentials
RUN dotnet nuget add source https://nuget.pkg.github.com/${GH_OWNER}/index.json \
    --name github \
    --username "${GH_USER}" \
    --password "${GH_TOKEN}" \
    --store-password-in-clear-text

# Copy csproj
COPY SvcMessagingBridge.csproj .
RUN dotnet restore SvcMessagingBridge.csproj

# Copy the rest of the source
COPY . .

# Publish
RUN dotnet publish SvcMessagingBridge.csproj -c Release -o /out

# ---------- runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Install curl for container healthchecks
RUN apt-get update \
 && apt-get install -y --no-install-recommends curl \
 && rm -rf /var/lib/apt/lists/*

COPY --from=build /out .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "SvcMessagingBridge.dll"]