FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["CSpider.csproj", "./"]
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

RUN mkdir /app/data
RUN useradd -u 8877 nonroot
RUN chown -R nonroot:nonroot /app && \
    chmod -R 644 /app && \
    find /app -type d -exec chmod 755 {} \; && \
    chmod -R 755 /app/data

USER nonroot

EXPOSE 80
ENTRYPOINT ["dotnet", "CSpider.dll"]