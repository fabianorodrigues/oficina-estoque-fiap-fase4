ARG DOTNET_VERSION=10.0

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
WORKDIR /src

COPY global.json ./
COPY src/Oficina.Estoque.Domain/Oficina.Estoque.Domain.csproj src/Oficina.Estoque.Domain/
COPY src/Oficina.Estoque.Application/Oficina.Estoque.Application.csproj src/Oficina.Estoque.Application/
COPY src/Oficina.Estoque.Infrastructure/Oficina.Estoque.Infrastructure.csproj src/Oficina.Estoque.Infrastructure/
COPY src/Oficina.Estoque.Api/Oficina.Estoque.Api.csproj src/Oficina.Estoque.Api/
RUN dotnet restore src/Oficina.Estoque.Api/Oficina.Estoque.Api.csproj

COPY . .
RUN dotnet publish src/Oficina.Estoque.Api/Oficina.Estoque.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION}
WORKDIR /app
USER root
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
ADD https://truststore.pki.rds.amazonaws.com/global/global-bundle.pem /tmp/aws-rds-global-bundle.pem
RUN cat /tmp/aws-rds-global-bundle.pem >> /etc/ssl/certs/ca-certificates.crt \
    && rm /tmp/aws-rds-global-bundle.pem
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "Oficina.Estoque.Api.dll"]
