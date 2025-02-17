# Use official Node.js image
FROM node:18-alpine as build

# Set working directory
WORKDIR /app

# Copy package.json and install dependencies
COPY package.json package-lock.json ./
RUN npm install

# Copy project files
COPY . .

# Inject API URL from build argument
ARG REACT_APP_API_URL=http://4.254.53.10
ENV REACT_APP_API_URL=$REACT_APP_API_URL

# Build the project with environment variables
RUN npm run build

# Serve the frontend using Nginx
FROM nginx:alpine

# Copy built frontend files to Nginx HTML directory
COPY --from=build /app/dist /usr/share/nginx/html

# Copy Nginx configuration
COPY ./nginx.conf /etc/nginx/nginx.conf

# Expose port 80
EXPOSE 80

# Start Nginx
CMD ["nginx", "-g", "daemon off;"]
