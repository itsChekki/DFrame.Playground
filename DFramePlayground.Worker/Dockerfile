FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["DFramePlayground.Worker/DFramePlayground.Worker.csproj", "DFramePlayground.Worker/"]
RUN dotnet restore "DFramePlayground.Worker/DFramePlayground.Worker.csproj"
COPY . .
WORKDIR "/src/DFramePlayground.Worker"
RUN dotnet build "DFramePlayground.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "DFramePlayground.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DFramePlayground.Worker.dll"]
