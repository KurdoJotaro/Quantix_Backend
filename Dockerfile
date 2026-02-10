# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["src/Quantix.API/Quantix.API.csproj", "Quantix.API/"]
COPY ["src/Quantix.Core/Quantix.Core.csproj", "Quantix.Core/"]
COPY ["src/Quantix.Data/Quantix.Data.csproj", "Quantix.Data/"]
COPY ["src/Quantix.Service/Quantix.Service.csproj", "Quantix.Service/"]
RUN dotnet restore "Quantix.API/Quantix.API.csproj"

COPY src/ .
WORKDIR "/src/Quantix.API"
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Quantix.API.dll"]
