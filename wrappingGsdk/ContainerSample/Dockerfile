FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /source

# copy csproj and restore as distinct layers
COPY fakegame/*.csproj ./fakegame/
COPY wrapper/*.csproj ./wrapper/
RUN dotnet restore ./fakegame && dotnet restore ./wrapper

# copy and publish app and libraries
COPY fakegame/ ./fakegame/
COPY wrapper/ ./wrapper/
RUN dotnet publish ./fakegame -c release -o /fakegamepublish --self-contained -r linux-x64 /p:PublishSingleFile=true /p:PublishTrimmed=true /p:PublishReadyToRun=true
RUN dotnet publish ./wrapper -c release -o /wrapperpublish --self-contained -r linux-x64 /p:PublishSingleFile=true /p:PublishTrimmed=true /p:PublishReadyToRun=true

# final stage/image
FROM ubuntu:18.04
WORKDIR /app
COPY --from=build /fakegamepublish .
COPY --from=build /wrapperpublish .
EXPOSE 80/tcp
RUN chmod +x wrapper fakegame
# https://github.com/dotnet/core/issues/2186#issuecomment-671105420
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 
CMD ["./wrapper", "-g", "/app/fakegame"]