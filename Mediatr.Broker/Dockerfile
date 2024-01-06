FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Mediatr.Broker/Mediatr.Broker.csproj", "Mediatr.Broker/"]
RUN dotnet restore "Mediatr.Broker/Mediatr.Broker.csproj"
COPY . .
WORKDIR "/src/Mediatr.Broker"
RUN dotnet build "Mediatr.Broker.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Mediatr.Broker.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Mediatr.Broker.dll"]
