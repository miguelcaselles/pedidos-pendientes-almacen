FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY PedidosPendientesAlmacen.sln ./
COPY src/PedidosPendientes.Core/PedidosPendientes.Core.csproj src/PedidosPendientes.Core/
COPY src/PedidosPendientes.Infrastructure/PedidosPendientes.Infrastructure.csproj src/PedidosPendientes.Infrastructure/
COPY src/PedidosPendientes.Web/PedidosPendientes.Web.csproj src/PedidosPendientes.Web/
RUN dotnet restore PedidosPendientesAlmacen.sln

COPY . .
RUN dotnet publish src/PedidosPendientes.Web/PedidosPendientes.Web.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=10000
ENV ASPNETCORE_ENVIRONMENT=Production
# Hora española: los sellos de carga/reclamación y los cortes de día se muestran
# y calculan en la zona del hospital, no en UTC.
ENV TZ=Europe/Madrid
EXPOSE 10000

COPY --from=build /app/publish .
USER app
ENTRYPOINT ["dotnet", "PedidosPendientes.Web.dll"]
