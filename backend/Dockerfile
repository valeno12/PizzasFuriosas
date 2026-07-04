# syntax=docker/dockerfile:1

# ---------- build ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiamos primero los .csproj y la solución para aprovechar el cache del restore:
# si no cambian las dependencias, Docker reusa esta capa.
COPY PizzasFuriosas.sln ./
COPY src/PizzasFuriosas.Core/PizzasFuriosas.Core.csproj                     src/PizzasFuriosas.Core/
COPY src/PizzasFuriosas.Infrastructure/PizzasFuriosas.Infrastructure.csproj src/PizzasFuriosas.Infrastructure/
COPY src/PizzasFuriosas.Api/PizzasFuriosas.Api.csproj                       src/PizzasFuriosas.Api/
RUN dotnet restore src/PizzasFuriosas.Api/PizzasFuriosas.Api.csproj

# Copiamos el resto del código y publicamos en Release.
COPY . .
RUN dotnet publish src/PizzasFuriosas.Api/PizzasFuriosas.Api.csproj \
    -c Release -o /app/publish /p:UseAppHost=false

# ---------- runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Render inyecta la variable PORT en tiempo de ejecución; Program.cs la lee y escucha ahí.
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

ENTRYPOINT ["dotnet", "PizzasFuriosas.Api.dll"]
