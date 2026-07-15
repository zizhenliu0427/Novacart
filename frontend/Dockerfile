# Build stage
FROM node:20-alpine AS build
WORKDIR /app

# Baked into next.config rewrites at build time (see next.config.mjs).
ARG BACKEND_ORIGIN=http://gateway:8080
ENV BACKEND_ORIGIN=${BACKEND_ORIGIN}

COPY package.json package-lock.json* ./
RUN npm install

COPY . .
RUN npm run build

# Runtime stage
FROM node:20-alpine
WORKDIR /app

COPY --from=build /app/.next ./.next
COPY --from=build /app/node_modules ./node_modules
COPY --from=build /app/package.json ./
COPY --from=build /app/public ./public

EXPOSE 3000

ENV NODE_ENV=production

CMD ["npm", "start"]
