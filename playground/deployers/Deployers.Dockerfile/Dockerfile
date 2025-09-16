# Use an official Python runtime as a parent image
FROM netaspireci.azurecr.io/library/python:3.8-slim

# Build arguments for customization
ARG BUILD_VERSION=unknown
ARG CUSTOM_MESSAGE=Hello from Aspire!

# Set the working directory in the container
WORKDIR /app

# Use build arguments to create environment variables
ENV BUILD_VERSION=${BUILD_VERSION}
ENV CUSTOM_MESSAGE=${CUSTOM_MESSAGE}

RUN --mount=type=secret,id=BUILD_SECRET \
    if [ -f /run/secrets/BUILD_SECRET ] && [ -s /run/secrets/BUILD_SECRET ]; then \
        echo "Secret token available during build process"; \
        echo "BUILD_SECRET_AVAILABLE=true" > /app/build_metadata.txt; \
        SECRET_CONTENT=$(cat /run/secrets/BUILD_SECRET); \
        SECRET_LENGTH=${#SECRET_CONTENT}; \
        echo "BUILD_SECRET_LENGTH=$SECRET_LENGTH" >> /app/build_metadata.txt; \
        echo "BUILD_TIMESTAMP=$(date -u +%Y-%m-%dT%H:%M:%SZ)" >> /app/build_metadata.txt; \
    else \
        echo "BUILD_SECRET_AVAILABLE=false" > /app/build_metadata.txt; \
        echo "BUILD_SECRET_LENGTH=0" >> /app/build_metadata.txt; \
        echo "BUILD_TIMESTAMP=$(date -u +%Y-%m-%dT%H:%M:%SZ)" >> /app/build_metadata.txt; \
    fi

# Copy requirements.txt first to leverage Docker cache layers
COPY requirements.txt /app/

# Install Python dependencies
RUN pip install --no-cache-dir -r requirements.txt

# Copy the rest of the application code
COPY . /app

# Expose the port the app runs on (Azure Container Apps expects port 80)
EXPOSE 80

# Run the command to execute app.py when the container starts
CMD ["python", "app.py"]

