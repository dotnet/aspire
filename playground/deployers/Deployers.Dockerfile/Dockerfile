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

# Use secret token during build (for demonstration - normally this would be used for auth during build)
# The secret is mounted as a file and not exposed in the final image
RUN --mount=type=secret,id=BUILD_SECRET,target=/run/secrets/token \
    if [ -f /run/secrets/token ]; then \
        echo "Secret token available during build process"; \
    else \
        echo "No secret token provided for build"; \
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

