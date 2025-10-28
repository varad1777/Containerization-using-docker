# Stage 1 — build
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src

# copy csproj(s) first for better cache usage (adjust path if needed)
COPY ["MyBackend.Api/MyBackend.Api.csproj", "API/"]

RUN dotnet restore "MyBackend.Api/MyBackend.Api.csproj"

# copy rest
COPY . .
WORKDIR /src/MyBackend.Api
RUN dotnet publish

# Stage 2 — runtime
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS runtime
WORKDIR /app

# create non-root user
RUN addgroup -S appgroup && adduser -S appuser -G appgroup
USER appuser

COPY --from=build /app/publish ./

ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000

ENTRYPOINT ["dotnet", "MyBackend.Api.dll"]

