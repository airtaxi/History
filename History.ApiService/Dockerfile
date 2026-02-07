FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY History.ApiService/ History.ApiService/
COPY History.Commons/ History.Commons/
COPY History.ServiceDefaults/ History.ServiceDefaults/
RUN dotnet restore "History.ApiService/History.ApiService.csproj"
RUN dotnet publish "History.ApiService/History.ApiService.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble AS final
WORKDIR /app
COPY --from=build /app/publish .

# Install Playwright dependencies and Chrome
RUN apt-get update \
    && apt-get install -y --no-install-recommends wget gnupg ca-certificates \
    && wget -q -O - https://dl.google.com/linux/linux_signing_key.pub | gpg --dearmor > /usr/share/keyrings/google-linux-signing-keyring.gpg \
    && echo "deb [arch=amd64 signed-by=/usr/share/keyrings/google-linux-signing-keyring.gpg] http://dl.google.com/linux/chrome/deb/ stable main" > /etc/apt/sources.list.d/google-chrome.list \
    && apt-get update \
    && apt-get install -y --no-install-recommends google-chrome-stable \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://0.0.0.0:31227
EXPOSE 31227
ENTRYPOINT ["dotnet", "History.ApiService.dll"]
