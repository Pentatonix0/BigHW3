FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /Backend

COPY ["Backend/OrdersService/OrdersService.csproj", "Backend/OrdersService/"]
COPY ["Backend/Common/Common.csproj", "Backend/Common/"]
RUN dotnet restore "Backend/OrdersService/OrdersService.csproj"

COPY . .
WORKDIR "/Backend/Backend/OrdersService"
RUN dotnet build "OrdersService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "OrdersService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OrdersService.dll"]