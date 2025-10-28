# Stage 1: build
FROM node:18-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci --silent
COPY . .
ARG REACT_APP_API_BASE_URL="/api"
ENV REACT_APP_API_BASE_URL=${REACT_APP_API_BASE_URL}
RUN npm run build

# Stage 2: serve with a small static server (serve)
FROM node:18-alpine AS runtime
WORKDIR /app
# install "serve" for a tiny runtime static server
RUN npm i -g serve
COPY --from=build /app/build /app/build
EXPOSE 3000
CMD ["serve", "-s", "build", "-l", "3000"]
