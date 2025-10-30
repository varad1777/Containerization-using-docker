# Stage 1 - build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# copy csproj and restore
COPY ["MyApp.AverageCalculator.csproj", "./"]
RUN dotnet restore "./MyApp.AverageCalculator.csproj"

# copy everything and build
COPY . .
RUN dotnet publish "MyApp.AverageCalculator.csproj" -c Release -o /app/publish /p:PublishTrimmed=false

# Stage 2 - runtime
FROM mcr.microsoft.com/dotnet/runtime:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MyApp.AverageCalculator.dll"]
