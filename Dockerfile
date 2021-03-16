FROM mcr.microsoft.com/dotnet/aspnet:3.1-focal AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:3.1-focal AS build
WORKDIR /src
COPY ["ShoeStore.Identity.IdentityServer/ShoeStore.Identity.IdentityServer.csproj", "ShoeStore.Identity.IdentityServer/"]
RUN dotnet restore "ShoeStore.Identity.IdentityServer/ShoeStore.Identity.IdentityServer.csproj"
COPY . .
WORKDIR "/src/ShoeStore.Identity.IdentityServer"
RUN dotnet build "ShoeStore.Identity.IdentityServer.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "ShoeStore.Identity.IdentityServer.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "ShoeStore.Identity.IdentityServer.dll"]