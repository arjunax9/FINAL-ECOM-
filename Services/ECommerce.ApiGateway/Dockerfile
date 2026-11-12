FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Services/ECommerce.ApiGateway/ECommerce.ApiGateway.csproj", "Services/ECommerce.ApiGateway/"]
RUN dotnet restore "Services/ECommerce.ApiGateway/ECommerce.ApiGateway.csproj"

COPY . .
WORKDIR "/src/Services/ECommerce.ApiGateway"
RUN dotnet publish "ECommerce.ApiGateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ECommerce.ApiGateway.dll"]
