# Stage 1 — Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
#take build tools also 
WORKDIR /src

# Copy only csproj files first (to cache restore step)
COPY ["API/MyApp.API.csproj", "API/"]
COPY ["Application/MyApp.Application.csproj", "Application/"]
COPY ["Domain/MyApp.Domain.csproj", "Domain/"]
COPY ["Infrastructure/MyApp.Infrastructure.csproj", "Infrastructure/"]

# Restore all NuGet dependencies
RUN dotnet restore "API/MyApp.API.csproj"
#restoring dependenies before copying the entire source code helps in leveraging docker cache and speeds up build time.

# Copy the rest of the source code
COPY . . 
# . . means - > current directory, and destination in container
#above code has to take the folder where docker file exist 
#but i give context as src in yml file 

# Build and publish
WORKDIR "/src/API"
RUN dotnet publish "MyApp.API.csproj" -c Release -o /app/publish /p:UseAppHost=false
#dotnet try to generate platform specific executable file
#but in container we dont need that so we use UseAppHost=false
# so that it generates dll file only
# so platform specific dependencies are not included in the build output

# Stage 2 — Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Copy published output from build stage
COPY --from=build /app/publish .

# Configure ports
ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000

ENTRYPOINT ["dotnet", "MyApp.API.dll"]
