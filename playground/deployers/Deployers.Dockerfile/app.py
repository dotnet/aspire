from flask import Flask, jsonify
import datetime
import os

app = Flask(__name__)

@app.route('/')
def home():
    """Home endpoint that returns a welcome message"""
    return jsonify({
        "message": "Welcome to the Simple Python REST API",
        "timestamp": datetime.datetime.now().isoformat(),
        "status": "success"
    })

@app.route('/api/data')
def get_data():
    """Returns sample data as JSON"""
    return jsonify({
        "data": [
            {"id": 1, "name": "Alice", "role": "Developer"},
            {"id": 2, "name": "Bob", "role": "Designer"},
            {"id": 3, "name": "Charlie", "role": "Manager"}
        ],
        "total": 3,
        "timestamp": datetime.datetime.now().isoformat()
    })

@app.route('/api/status')
def get_status():
    """Returns API status information"""
    return jsonify({
        "api": "Simple Python REST API",
        "version": "1.0.0",
        "status": "healthy",
        "uptime": "running",
        "timestamp": datetime.datetime.now().isoformat()
    })

@app.route('/api/health')
def health_check():
    """Health check endpoint"""
    return jsonify({
        "status": "healthy",
        "message": "API is running successfully"
    }), 200

@app.route('/api/build-info')
def get_build_info():
    """Returns build arguments passed during container build"""
    return jsonify({
        "build_version": os.environ.get("BUILD_VERSION", "unknown"),
        "custom_message": os.environ.get("CUSTOM_MESSAGE", "default message"),
        "test_scenario": os.environ.get("TEST_SCENARIO", "none"),
        "timestamp": datetime.datetime.now().isoformat()
    })

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=80, debug=False)
