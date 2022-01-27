FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /source

# copy csproj and restore as distinct layers
COPY ./*.csproj ./
RUN dotnet restore ./

# copy and publish app and libraries
COPY ./Program.cs ./
RUN dotnet publish ./ -c release -o /wrapperpublish --self-contained -r linux-x64 /p:PublishSingleFile=true /p:PublishTrimmed=true /p:PublishReadyToRun=true

# final stage/image
FROM ubuntu:18.04
WORKDIR /app
COPY --from=build /wrapperpublish .
RUN chmod +x wrapper
# https://github.com/dotnet/core/issues/2186#issuecomment-671105420
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 
CMD ["ls"]