FROM python:3.11
LABEL authors="TOUZET Stanislas"

# Copy the current directory contents into the container at /app
COPY . /app

# Set the working directory to /app
WORKDIR /app

# Install necessary packages
RUN pip install -r requirements.txt

# Expose the port the app runs on
EXPOSE 8000

# Run the application
CMD python app.py
