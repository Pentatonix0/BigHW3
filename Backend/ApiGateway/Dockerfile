FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /Backend

COPY ["Backend/ApiGateway/ApiGateway.csproj", "Backend/ApiGateway/"]
COPY ["Backend/Common/Common.csproj", "Backend/Common/"]
RUN dotnet restore "Backend/ApiGateway/ApiGateway.csproj"

COPY . .
WORKDIR "/Backend/Backend/ApiGateway"
RUN dotnet build "ApiGateway.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ApiGateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=publish /app/publish .

COPY --from=build /Backend/Backend/ApiGateway/ocelot.json .

ENTRYPOINT ["dotnet", "ApiGateway.dll"]