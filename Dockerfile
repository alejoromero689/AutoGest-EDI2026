FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY AutoGest/AutoGest/AutoGest.csproj AutoGest/AutoGest/
RUN dotnet restore AutoGest/AutoGest/AutoGest.csproj
COPY AutoGest/AutoGest/ AutoGest/AutoGest/
RUN dotnet publish AutoGest/AutoGest/AutoGest.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "AutoGest.dll"]
