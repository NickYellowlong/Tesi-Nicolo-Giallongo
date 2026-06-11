import time
from fastapi import APIRouter, HTTPException, BackgroundTasks
from typing import List

from app.info_extractor import InformationExtractor
from app.models import (
    StatusResponse
)
from app.retriever import DocumentRetriever
from app import config
from app.routes.search import get_retriever
from app.generator import TextGenerator
import requests
import logging

# Create router
router = APIRouter(prefix="/api", tags=["general"])

# Global retriever instance (will be initialized in main)
retriever: DocumentRetriever = None
extractor: InformationExtractor = None
generator: TextGenerator = None

@router.get("/health", response_model=StatusResponse)
async def health_check():
    retriever = get_retriever()

    """Check if the API is healthy"""
    if retriever is None:
        raise HTTPException(status_code=503, detail="Retriever not initialized")
    if extractor is None:
        raise HTTPException(status_code=503, detail="Extractor not initialized")
    if generator is None:
        raise HTTPException(status_code=503, detail="Generator not initialized")
    
    ret_status = retriever.get_status() if retriever else {"initialized": False, "documents_count": 0, "indexes_loaded": False}
    ext_status = extractor.get_status() if extractor else {"initialized": False}
    gen_status = generator.get_status() if generator else {"initialized": False}
    return StatusResponse(
        status="healthy" if ret_status["initialized"] and ext_status["initialized"] and gen_status["initialized"] else "degraded",
        retrieval_documents_count=ret_status["documents_count"],
        retrieval_indexes_loaded=ret_status["indexes_loaded"],
        retrieval_models_initialized=ret_status["initialized"],
        extraction_models_initialized=ext_status["initialized"],
        generation_models_initialized=gen_status["initialized"],
        version=config.API_VERSION
    )

async def init_models():
    response = []
    logging.info(f"Generation model: {config.GENERATION_MODEL}")
    response.append(requests.post("http://localhost:1234/api/v1/models/load", json={
        "model": config.GENERATION_MODEL,
        "context_length": 4500
    }))
    response.append(requests.post("http://localhost:1234/api/v1/models/load", json={
        "model": config.EMBEDDING_MODEL,
        #"identifier": config.EMBEDDING_MODEL
    }))
    if config.GENERATION_MODEL != config.PATHFINDER_MODEL:
        response.append(requests.post("http://localhost:1234/api/v1/models/load", json={
            "model": config.PATHFINDER_MODEL,
            #"identifier": config.PATHFINDER_MODEL,
            "context_length": 1024 
        }))
    return response