# Stage 1 — Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy only csproj files first (to cache restore step)
COPY ["API/MyApp.API.csproj", "API/"]
COPY ["Application/MyApp.Application.csproj", "Application/"]
COPY ["Domain/MyApp.Domain.csproj", "Domain/"]
COPY ["Infrastructure/MyApp.Infrastructure.csproj", "Infrastructure/"]

# Restore all NuGet dependencies
RUN dotnet restore "API/MyApp.API.csproj"

# Copy the rest of the source code
COPY . .

# Build and publish
WORKDIR "/src/API"
RUN dotnet publish "MyApp.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2 — Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Create non-root user (security)
# RUN addgroup -S appgroup && adduser -S appuser -G appgroup
# USER appuser

# Copy published output from build stage
COPY --from=build /app/publish .

# Configure ports
ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000

ENTRYPOINT ["dotnet", "MyApp.API.dll"]
