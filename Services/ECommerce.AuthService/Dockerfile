FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["ECommerce.Models/ECommerce.Models.csproj", "ECommerce.Models/"]
COPY ["Services/ECommerce.AuthService/ECommerce.AuthService.csproj", "Services/ECommerce.AuthService/"]
RUN dotnet restore "Services/ECommerce.AuthService/ECommerce.AuthService.csproj"

COPY . .
WORKDIR "/src/Services/ECommerce.AuthService"
RUN dotnet publish "ECommerce.AuthService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 5001
ENV ASPNETCORE_URLS=http://+:5001
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ECommerce.AuthService.dll"]
