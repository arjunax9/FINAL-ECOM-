FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["ECommerce.Models/ECommerce.Models.csproj", "ECommerce.Models/"]
COPY ["Services/ECommerce.ProductService/ECommerce.ProductService.csproj", "Services/ECommerce.ProductService/"]
RUN dotnet restore "Services/ECommerce.ProductService/ECommerce.ProductService.csproj"

COPY . .
WORKDIR "/src/Services/ECommerce.ProductService"
RUN dotnet publish "ECommerce.ProductService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 5002
ENV ASPNETCORE_URLS=http://+:5002
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ECommerce.ProductService.dll"]
