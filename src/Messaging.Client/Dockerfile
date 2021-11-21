FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine AS build-env
WORKDIR /app

# Copy everything and build
COPY . ./
RUN dotnet pack -c Debug -o out

FROM scratch AS export-stage

COPY --from=build-env /app/out /