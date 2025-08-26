# Use an official Python runtime as a parent image
FROM python:3.9-slim-buster

# Set the working directory in the container
WORKDIR /app

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

