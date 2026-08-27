FROM node:24-alpine

WORKDIR /app

# Install dependencies separately so Docker can reuse this layer.
COPY package*.json ./
RUN npm install

# The application source can be mounted as a shared volume at runtime.
COPY . .

EXPOSE 5173

CMD ["npm", "run", "dev", "--", "--host", "0.0.0.0"]
