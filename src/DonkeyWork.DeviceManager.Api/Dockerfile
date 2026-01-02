FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/DonkeyWork.DeviceManager.Api/DonkeyWork.DeviceManager.Api.csproj", "src/DonkeyWork.DeviceManager.Api/"]
COPY ["src/DonkeyWork.DeviceManager.Persistence/DonkeyWork.DeviceManager.Persistence.csproj", "src/DonkeyWork.DeviceManager.Persistence/"]
COPY ["src/DonkeyWork.DeviceManager.Common/DonkeyWork.DeviceManager.Common.csproj", "src/DonkeyWork.DeviceManager.Common/"]
COPY ["src/DonkeyWork.DeviceManager.Backend.Common/DonkeyWork.DeviceManager.Backend.Common.csproj", "src/DonkeyWork.DeviceManager.Backend.Common/"]
RUN dotnet restore "src/DonkeyWork.DeviceManager.Api/DonkeyWork.DeviceManager.Api.csproj"
COPY . .
WORKDIR "/src/src/DonkeyWork.DeviceManager.Api"
RUN dotnet build "./DonkeyWork.DeviceManager.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./DonkeyWork.DeviceManager.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DonkeyWork.DeviceManager.Api.dll"]
