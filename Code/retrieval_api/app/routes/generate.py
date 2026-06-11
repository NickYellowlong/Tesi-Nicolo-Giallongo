import time
from app.generator import TextGenerator
from fastapi import APIRouter, HTTPException, BackgroundTasks
from typing import List
import json

from app.models import (
   GenerateQuery, GenerateResponse, GenerateSummaryQuery, GenerateSummaryResponse
)
from app import config
import logging

# Create router
router = APIRouter(prefix="/api", tags=["generate"])

generator: TextGenerator = None

@router.post("/generate", response_model=GenerateResponse)
async def generate(query: GenerateQuery):
    """
    Generate text based on the query
    
    Returns a json with the generated text
    """
    
    try:
        start_time = time.time()
        
        # Perform extraction
        generated_text = generator.generate(query.query)
        print("Generated text:")
        print(generated_text)
        content_string = generated_text.get("content", "{}")
        print("Content string:")
        print(content_string)
        ##content_string = clean_json_string(content_string)
        ##print("Content string 2:")
        ##print(content_string)
        try:
            content_dict = json.loads(content_string)  # Converte stringa in dizionario
        except json.JSONDecodeError as e:
            # Fallback in caso di JSON non valido
            print(f"Errore parsing JSON: {e}")
            content_dict = {"narration": content_string}
        end_time = time.time()
        print("Generated dict:")
        print(content_dict)
        return GenerateResponse(
            content=content_dict,
            processing_time_ms=(end_time - start_time) * 1000
        )
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@router.post("/generate/summary", response_model=GenerateSummaryResponse)

async def generate_summary(query: GenerateSummaryQuery):
    """
    Generate a summary based on the query
    
    Returns a json with the generated summary
    """
    logging.info(f"Query: {query}")
    try:
        start_time = time.time()
    
    # Perform extraction
        generated_text = generator.generate_summary(query.query)
        end_time = time.time()
        return GenerateSummaryResponse(
            content=str(generated_text.get("content", "")),
            processing_time_ms=(end_time - start_time) * 1000
        )
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

def clean_json_string(raw_string: str) -> str:
    """
    Pulisce una stringa che dovrebbe essere JSON ma contiene newline letterali
    (\\n, \\r) o altri caratteri di controllo non escapati all'interno dei valori stringa.
    """
    # Divide la stringa sui doppi apici (preserva la struttura JSON)
    parti = raw_string.split('"')
    
    # Le parti con indice dispari sono i contenuti delle stringhe (valori tra virgolette)
    for i in range(1, len(parti), 2):
        # Escapa i caratteri di controllo più comuni
        parti[i] = (parti[i]
                    .replace('\\n', '\\\\n')   # evita doppio escape se già presente
                    .replace('\\r', '\\\\r')
                    .replace('\n', '\\n')
                    .replace('\r', '\\r')
                    .replace('\t', '\\t'))
    
    # Ricostruisce la stringa con i newline escapati
    return '"'.join(parti)