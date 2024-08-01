# Stage 1: Build using the .NET 7.0 SDK
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS builder
WORKDIR /usr/src/canyon/

# Set argument defaults
ENV CANYON_BUILD_CONFIG "release"
ARG CANYON_BUILD_CONFIG=$CANYON_BUILD_CONFIG

# Copy and build servers and dependencies
COPY . ./
RUN dotnet restore
RUN dotnet publish ./src/Canyon.Login/Canyon.Login.csproj -c $CANYON_BUILD_CONFIG -o out/Canyon.Login
RUN dotnet publish ./src/Canyon.Game/Canyon.Game.csproj -c $CANYON_BUILD_CONFIG -o out/Canyon.Game
RUN dotnet publish ./src/Canyon.Ai/Canyon.Ai.csproj -c $CANYON_BUILD_CONFIG -o out/Canyon.Ai
# RUN dotnet publish ./src/Canyon.GM.Server/Canyon.GM.Server.csproj -c $CANYON_BUILD_CONFIG -o out/Canyon.GM.Server


# Stage 2: Setup the runtime image
# Use the ASP.NET Core runtime image for applications that use ASP.NET Core
FROM mcr.microsoft.com/dotnet/aspnet:7.0

WORKDIR /usr/bin/canyon/
COPY --from=builder /usr/src/canyon/out .

# Copy the ini and map directories
# Adjust the paths according to where these directories are located in your source code
COPY ./src/Canyon.Game/ini /usr/bin/canyon/ini
COPY ./src/Canyon.Game/map /usr/bin/canyon/map
COPY ./src/Canyon.Game/lua /usr/bin/canyon/lua

# Copy the wait-for-it script and give it execute permissions
COPY wait-for-it.sh /wait-for-it.sh
RUN chmod +x /wait-for-it.sh

# Set the entrypoint to the wait-for-it script
ENTRYPOINT ["/wait-for-it.sh"]
