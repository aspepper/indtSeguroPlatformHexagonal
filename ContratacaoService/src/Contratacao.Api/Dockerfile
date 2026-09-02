FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["ContratacaoService/src/Contratacao.Domain/Contratacao.Domain.csproj", "ContratacaoService/src/Contratacao.Domain/"]
COPY ["ContratacaoService/src/Contratacao.Application/Contratacao.Application.csproj", "ContratacaoService/src/Contratacao.Application/"]
COPY ["ContratacaoService/src/Contratacao.Infrastructure/Contratacao.Infrastructure.csproj", "ContratacaoService/src/Contratacao.Infrastructure/"]
COPY ["ContratacaoService/src/Contratacao.Api/Contratacao.Api.csproj", "ContratacaoService/src/Contratacao.Api/"]
COPY ["src/Shared/IndtSeguro.Contracts/IndtSeguro.Contracts.csproj", "src/Shared/IndtSeguro.Contracts/"]

RUN dotnet restore "ContratacaoService/src/Contratacao.Api/Contratacao.Api.csproj"

COPY ContratacaoService/src/ ContratacaoService/src/
COPY src/Shared/ src/Shared/

WORKDIR "/src/ContratacaoService/src/Contratacao.Api"
RUN dotnet publish "Contratacao.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Contratacao.Api.dll"]
