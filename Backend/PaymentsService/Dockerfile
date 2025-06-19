FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /Backend

COPY ["Backend/PaymentsService/PaymentsService.csproj", "Backend/PaymentsService/"]
COPY ["Backend/Common/Common.csproj", "Backend/Common/"]
RUN dotnet restore "Backend/PaymentsService/PaymentsService.csproj"

COPY . .
WORKDIR "/Backend/Backend/PaymentsService"
RUN dotnet build "PaymentsService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PaymentsService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PaymentsService.dll"]