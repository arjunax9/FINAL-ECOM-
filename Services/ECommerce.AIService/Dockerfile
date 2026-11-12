FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["ECommerce.Models/ECommerce.Models.csproj", "ECommerce.Models/"]
COPY ["Services/ECommerce.AIService/ECommerce.AIService.csproj", "Services/ECommerce.AIService/"]
RUN dotnet restore "Services/ECommerce.AIService/ECommerce.AIService.csproj"

COPY . .
WORKDIR "/src/Services/ECommerce.AIService"
RUN dotnet publish "ECommerce.AIService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 5004
ENV ASPNETCORE_URLS=http://+:5004
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ECommerce.AIService.dll"]
