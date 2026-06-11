import time
from fastapi import APIRouter, HTTPException, BackgroundTasks
from typing import List
import json
import logging

from app.models import (
    ExtractQuery, ExtractResponse, PathFinderQuery, PathFinderResponse, HasJoinedQuery, HasJoinedResponse
)
from app.info_extractor import InformationExtractor
from app import config

# Create router
router = APIRouter(prefix="/api", tags=["extract"])

extractor: InformationExtractor = None

@router.post("/extract", response_model=ExtractResponse)
async def extract(query: ExtractQuery):
    """
    Extract relevant information from the query
    
    Returns a json with the extracted information
    """
    logging.info(f"Ricevuto: {query}")
    logging.info(f"Query dict: {query.dict()}")
    try:
        print("Extraction started")
        start_time = time.time()
        
        # Perform extraction
        extracted_info = extractor.extract(query.query)

        end_time = time.time()
        print("Extraction ended")

        try:
            extracted_info = json.loads(extracted_info)
        except json.JSONDecodeError as e:
    # Fallback in caso di JSON non valido
            print(f"Errore parsing JSON: {e}")
            extracted_info = {
                "action_list": [],
                "specific_actions": [],
                "error": str(extracted_info)
            }


        return ExtractResponse(
            extracted_info={
                "action_list": extracted_info.get("action_list", []),
                "specific_actions": extracted_info.get("specific_actions", []),
            },
            processing_time_ms= (end_time - start_time) * 1000
        )
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@router.post("/find_path", response_model=PathFinderResponse)
async def find_path(query: PathFinderQuery):
    """
    Find a path based on the query
    
    Returns a json with the path finding result
    """
    logging.info("Pathfinding")
    logging.info(f"Ricevuto: {query}")
    logging.info(f"Query dict: {query.dict()}")
    try:
        logging.info(f"Pathfinding start")
        start_time = time.time()
        
        # Perform path finding
        path_value = extractor.find_path(query.query)
        try:
            path_value = json.loads(path_value)
        except json.JSONDecodeError as e:
    # Fallback in caso di JSON non valido
            print(f"Errore parsing JSON: {e}")

        logging.info(f"Path value: {path_value}")
        end_time = time.time()
        value = path_value.get("content", [])
        logging.info(path_value.get("content", []))
        logging.info(f"Pathfinding end")
        return PathFinderResponse(
            value=value,
            processing_time_ms=(end_time - start_time) * 1000
        )
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e.with_traceback))
    
@router.post("/has_joined", response_model=HasJoinedResponse)
async def has_joined(query: HasJoinedQuery):
    """
    Find out if in the narration a character joins the main character
    """
    try:
        start_time = time.time()
        
        # Perform path finding
        logging.info(f"Checking if joined")
        has_joined = extractor.has_joined(query.narration)
        logging.info(f"has_joined result: {has_joined}")
        try:
            has_joined = json.loads(has_joined)
        except json.JSONDecodeError as e:
    # Fallback in caso di JSON non valido
            print(f"Errore parsing JSON: {e}")

        end_time = time.time()
        value = has_joined.get("content", [])
        return HasJoinedResponse(
            joined= has_joined.get("joined", []),
            target= has_joined.get("target", []),
            processing_time_ms=(end_time - start_time) * 1000
        )
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e.with_traceback))