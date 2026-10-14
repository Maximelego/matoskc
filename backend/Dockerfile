# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/MatosKC.Domain/MatosKC.Domain.csproj src/MatosKC.Domain/
COPY src/MatosKC.Application/MatosKC.Application.csproj src/MatosKC.Application/
COPY src/MatosKC.Infrastructure/MatosKC.Infrastructure.csproj src/MatosKC.Infrastructure/
COPY src/MatosKC.Api/MatosKC.Api.csproj src/MatosKC.Api/
RUN dotnet restore src/MatosKC.Api/MatosKC.Api.csproj
COPY src/ src/
RUN dotnet publish src/MatosKC.Api/MatosKC.Api.csproj \
    -c Release --no-restore -o /out /p:UseAppHost=false

FROM build AS migrations
RUN dotnet tool install dotnet-ef --tool-path /tools --version 10.0.12
ENTRYPOINT ["/tools/dotnet-ef"]
CMD ["database", "update", "--project", "src/MatosKC.Infrastructure/MatosKC.Infrastructure.csproj", "--startup-project", "src/MatosKC.Api/MatosKC.Api.csproj", "--configuration", "Release", "--no-build"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /out/ ./
RUN mkdir /app/.auth-keys && chown "$APP_UID:$APP_UID" /app/.auth-keys
USER $APP_UID
ENV ASPNETCORE_URLS=http://+:8000
EXPOSE 8000
ENTRYPOINT ["dotnet", "MatosKC.Api.dll"]
