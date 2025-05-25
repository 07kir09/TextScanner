FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["AntiPlagiarism.ApiGateway/AntiPlagiarism.ApiGateway.csproj", "AntiPlagiarism.ApiGateway/"]
COPY ["AntiPlagiarism.Common/AntiPlagiarism.Common.csproj", "AntiPlagiarism.Common/"]
RUN dotnet restore "AntiPlagiarism.ApiGateway/AntiPlagiarism.ApiGateway.csproj"
COPY . .
WORKDIR "/src/AntiPlagiarism.ApiGateway"
RUN dotnet build "AntiPlagiarism.ApiGateway.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AntiPlagiarism.ApiGateway.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AntiPlagiarism.ApiGateway.dll"]