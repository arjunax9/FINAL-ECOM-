FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["ECommerce.Models/ECommerce.Models.csproj", "ECommerce.Models/"]
COPY ["Services/ECommerce.OrderService/ECommerce.OrderService.csproj", "Services/ECommerce.OrderService/"]
RUN dotnet restore "Services/ECommerce.OrderService/ECommerce.OrderService.csproj"

COPY . .
WORKDIR "/src/Services/ECommerce.OrderService"
RUN dotnet publish "ECommerce.OrderService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 5003
ENV ASPNETCORE_URLS=http://+:5003
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ECommerce.OrderService.dll"]
