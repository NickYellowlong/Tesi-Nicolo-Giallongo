from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import JSONResponse
import logging

from app import config
from app.info_extractor import InformationExtractor
from app.routes import extract, general, search, generate
from app.retriever import DocumentRetriever
from app.routes.generate import TextGenerator

import json
from pathlib import Path

# Setup logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

# Create FastAPI app
app = FastAPI(
    title=config.API_TITLE,
    description=config.API_DESCRIPTION,
    version=config.API_VERSION
)

# Add CORS middleware (allows frontend to connect)
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],  # In production, specify your frontend URL
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Global retriever instance
retriever = DocumentRetriever()
extractor = InformationExtractor()
text_generator = TextGenerator()

# Load system prompts JSON and configure extractor prompt
retriever.load_docs()  # Load documents before setting system info, in case it relies on docs
requests_info_path = config.DATA_DIR / "requests_information.json"
try:
    with open(requests_info_path, "r", encoding="utf-8") as f:
        requests_info = json.load(f)
except FileNotFoundError:
    logger.error(f"System prompts file not found: {requests_info_path}")
    requests_info = {}
except json.JSONDecodeError as e:
    logger.error(f"Failed to parse system prompts JSON: {e}")
    requests_info = {}

extractor_system_info = requests_info.get("extract_information")
if not extractor_system_info:
    logger.warning("extract_information prompt not found in requests_information.json; using fallback from config if available")
    extractor_system_info = getattr(config, "SYSTEM_PROMPT", None)

if extractor_system_info:
    extractor.set_estractor_system_info(extractor_system_info)
else:
    logger.error("No system info set for extractor. Extraction requests may fail.")

has_joined_system_info = requests_info.get("has_joined")
if not has_joined_system_info:
    logger.warning("has_joined prompt not found in requests_information.json; using fallback from config if available")
    has_joined_system_info = getattr(config, "HAS_JOINED_SYSTEM_PROMPT", None)

if has_joined_system_info:
    extractor.set_has_joined_system_info(has_joined_system_info)
else:
    logger.error("No system info set for has_joined. Extraction requests may fail.")

pathfinder_system_info = requests_info.get("pathfinding")
if not pathfinder_system_info:
    logger.warning("pathfinding prompt not found in requests_information.json; using fallback from config if available")
    pathfinder_system_info = getattr(config, "PATHFINDER_SYSTEM_PROMPT", None)
if pathfinder_system_info:
    extractor.set_pathfinder_system_info(pathfinder_system_info)

generator_system_info = requests_info.get("generate_narration")
if not generator_system_info:
    logger.warning("generate_narration prompt not found in requests_information.json; using fallback from config if available")
    generator_system_info = getattr(config, "GENERATION_SYSTEM_PROMPT", None)

if generator_system_info:
    text_generator.set_generator_system_info(generator_system_info)
else:
    logger.error("No system prompt set for generator. Generation requests may fail.")

summary_system_info = requests_info.get("generate_summary")
if summary_system_info:
    text_generator.set_summary_system_info(summary_system_info)
else:
    logger.error("No system prompt set for summary generator. Generation requests may fail.")


# Pass retriever to routes
search.retriever = retriever
extract.extractor = extractor
generate.generator = text_generator
general.retriever = retriever
general.extractor = extractor
general.generator = text_generator

# Include routers
app.include_router(search.router)
app.include_router(extract.router)
app.include_router(general.router)
app.include_router(generate.router)

@app.on_event("startup")
async def startup_event():
    """Initialize models and load indexes on startup"""
    logger.info("Starting up API...")
    
    # Initialize models first (required for loading indexes)
    try:
        results = await general.init_models()
    except Exception as e:
        logger.error(f"Failed to initialize models: {e}")
        return
    for r in results:
        logger.info(r)
    try:
        search.retriever.initialize_models(load_reranker=True)
        general.retriever.initialize_models(load_reranker=True)
        logger.info("Retriever models initialized")
    except Exception as e:
        logger.error(f"Failed to initialize retriever models: {e}")
        return  # Don't proceed if models failed to initialize
    
    try:
        # Try to load existing indexes
        retriever.load_indexes()
        logger.info("Loaded existing indexes")
    except FileNotFoundError:
        logger.warning("No existing indexes found. You'll need to build them.")

    try:
        text_generator.initialize_model()
        logger.info("Generator model initialized")
    except Exception as e:
        logger.error(f"Failed to initialize generator model: {e}")
        return  # Don't proceed if models failed to initialize
    

@app.on_event("shutdown")
async def shutdown_event():
    """Cleanup on shutdown"""
    logger.info("Shutting down API...")

@app.get("/")
async def root():
    """Root endpoint with API info"""
    return {
        "name": config.API_TITLE,
        "version": config.API_VERSION,
        "description": config.API_DESCRIPTION,
        "endpoints": {
            "health": "/api/health",
            "search": "/api/search",
            "documents": "/api/documents",
            "extract": "/api/extract",
            "docs": "/docs"  # FastAPI automatic documentation
        }
    }

# Error handler
@app.exception_handler(Exception)
async def global_exception_handler(request, exc):
    logger.error(f"Global error: {exc}")
    return JSONResponse(
        status_code=500,
        content={"detail": "Internal server error"}
    )