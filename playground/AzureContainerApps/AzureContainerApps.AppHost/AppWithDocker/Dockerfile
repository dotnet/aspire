# Use an official Python runtime as a parent image
FROM netaspireci.azurecr.io/library/python:3.8-slim

# Set the working directory in the container
WORKDIR /app

# Copy the current directory contents into the container at /app
COPY . /app

# Run the command to execute app.py when the container starts
CMD ["python", "app.py"]
