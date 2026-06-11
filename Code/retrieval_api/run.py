#!/usr/bin/env python
"""
Run script for the Document Retrieval API
"""
import uvicorn
import argparse
from app import config

def main():
    parser = argparse.ArgumentParser(description="Run the Document Retrieval API")
    parser.add_argument("--host", type=str, default="127.0.0.1", help="Host to bind to")
    parser.add_argument("--port", type=int, default=8000, help="Port to bind to")
    parser.add_argument("--reload", action="store_true", help="Enable auto-reload for development")
    
    args = parser.parse_args()
    
    print(f"🚀 Starting {config.API_TITLE} v{config.API_VERSION}")
    print(f"📡 Server will run on http://{args.host}:{args.port}")
    print(f"📚 API documentation available at http://{args.host}:{args.port}/docs")
    print("Press Ctrl+C to stop")
    
    uvicorn.run(
        "app.main:app",
        host=args.host,
        port=args.port,
        reload=args.reload
    )

if __name__ == "__main__":
    main()