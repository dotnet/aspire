# DisableDockerDetector "Playground/demo application used for testing Aspire features"
FROM node:20
 
WORKDIR /app
 
COPY package.json package.json
COPY package-lock.json package-lock.json
 
RUN npm install
 
COPY . .
 
CMD [ "node", "app.js" ]