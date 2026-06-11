import time
from fastapi import APIRouter, HTTPException, BackgroundTasks
from typing import List

from app.models import (
    SearchQuery, SearchResponse, DocumentResponse, 
    StatusResponse, AddDocumentsRequest, AddDocumentsResponse
)
from app.retriever import DocumentRetriever
from app import config
import logging

# Create router
router = APIRouter(prefix="/api", tags=["search"])

# Global retriever instance (will be initialized in main)
retriever: DocumentRetriever = None

def get_retriever() -> DocumentRetriever:
    """Dependency to get retriever instance"""
    if retriever is None:
        raise HTTPException(status_code=503, detail="Retriever not initialized")
    return retriever

@router.post("/search", response_model=SearchResponse)
async def search(query: SearchQuery):
    """
    Search for documents matching the query
    
    Returns ranked and reranked documents
    """
    retriever = get_retriever()
    logging.info(f"Reranker available: {retriever.jina_reranker is not None}")
    logging.info("Retriever gotten")
    try:
        start_time = time.time()
        
        # Perform search
        results = retriever.search(
            query=query.query,
            top_k=query.top_k,
            alpha=query.alpha
        )
        logging.info(f"Retrieval Results: {str(results)}")
        # Calculate processing time
        processing_time = (time.time() - start_time) * 1000  # Convert to ms
        
        # Format response
        return SearchResponse(
            query=query.query,
            results=[
                DocumentResponse(
                    id=doc["id"],
                    text=doc["text"],
                    score=float(doc["score"])
                )
                for doc in results
            ],
            total_found=len(results),
            processing_time_ms=round(processing_time, 2)
        )
        
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@router.post("/documents", response_model=AddDocumentsResponse)
async def add_documents(
    request: AddDocumentsRequest,
    background_tasks: BackgroundTasks
):
    """
    Add new documents to the index
    
    This will rebuild the index in the background
    """
    retriever = get_retriever()
    
    # Validate documents
    if not request.documents:
        raise HTTPException(status_code=400, detail="No documents provided")
    
    # Add documents (will rebuild index in background if requested)
    if request.rebuild_index:
        # Run in background to avoid timeout
        background_tasks.add_task(
            retriever.build_index,
            {**retriever.docs, **request.documents}
        )
        message = f"Adding {len(request.documents)} documents and rebuilding index in background"
    else:
        # Just update docs without rebuilding
        retriever.docs.update(request.documents)
        message = f"Added {len(request.documents)} documents (index not rebuilt)"
    logging.info("Writing documents to file")
    retriever.write_docs()  # Save updated docs to file
    logging.info("Documents written to file")

    return AddDocumentsResponse(
        added_count=len(request.documents),
        total_documents=len(retriever.docs),
        message=message
    )

@router.get("/documents/{doc_id}", response_model=DocumentResponse)
async def get_document(doc_id: str):
    """Get a specific document by ID"""
    retriever = get_retriever()
    
    if doc_id not in retriever.docs:
        raise HTTPException(status_code=404, detail="Document not found")
    
    return DocumentResponse(
        id=doc_id,
        text=retriever.docs[doc_id],
        score=1.0  # Full score for exact match
    )

@router.get("/documents", response_model=List[str])
async def list_documents():
    """List all document IDs"""
    retriever = get_retriever()
    return list(retriever.docs.keys())

@router.post("/indexes/rebuild")
async def rebuild_indexes(background_tasks: BackgroundTasks):
    """Rebuild search indexes from current documents"""
    retriever = get_retriever()
    
    if not retriever.docs:
        raise HTTPException(status_code=400, detail="No documents to index")
    
    background_tasks.add_task(retriever.build_index, retriever.docs)
    
    return {"message": "Index rebuild started in background"}