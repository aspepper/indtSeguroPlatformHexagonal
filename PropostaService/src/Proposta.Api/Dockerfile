FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["PropostaService/src/Proposta.Domain/Proposta.Domain.csproj", "PropostaService/src/Proposta.Domain/"]
COPY ["PropostaService/src/Proposta.Application/Proposta.Application.csproj", "PropostaService/src/Proposta.Application/"]
COPY ["PropostaService/src/Proposta.Infrastructure/Proposta.Infrastructure.csproj", "PropostaService/src/Proposta.Infrastructure/"]
COPY ["PropostaService/src/Proposta.Api/Proposta.Api.csproj", "PropostaService/src/Proposta.Api/"]
COPY ["src/Shared/IndtSeguro.Contracts/IndtSeguro.Contracts.csproj", "src/Shared/IndtSeguro.Contracts/"]

RUN dotnet restore "PropostaService/src/Proposta.Api/Proposta.Api.csproj"

COPY PropostaService/src/ PropostaService/src/
COPY src/Shared/ src/Shared/

WORKDIR "/src/PropostaService/src/Proposta.Api"
RUN dotnet publish "Proposta.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Proposta.Api.dll"]
