from pydantic import BaseModel, Field
from typing import List, Optional, Dict, Any

class SearchQuery(BaseModel):
    """
    Model for search request
    """
    query: str = Field(..., min_length=1, max_length=500, description="Search query text")
    top_k: Optional[int] = Field(7, ge=1, le=50, description="Number of results to return")
    alpha: Optional[float] = Field(0.7, ge=0.0, le=1.0, description="Weight for sparse/dense fusion")

class DocumentResponse(BaseModel):
    """
    Model for document in search response
    """
    id: str
    text: str
    score: float = Field(..., ge=0.0, le=1.0)

class SearchResponse(BaseModel):
    """
    Model for search response
    """
    query: str
    results: List[DocumentResponse]
    total_found: int
    processing_time_ms: float

class StatusResponse(BaseModel):
    """
    Model for system status
    """
    status: str
    retrieval_documents_count: int
    retrieval_indexes_loaded: bool
    retrieval_models_initialized: bool
    extraction_models_initialized: bool
    version: str

class AddDocumentsRequest(BaseModel):
    """
    Model for adding documents
    """
    documents: Dict[str, str] = Field(..., description="Dictionary of document IDs and texts")
    rebuild_index: bool = Field(True, description="Whether to rebuild index after adding")

class AddDocumentsResponse(BaseModel):
    """
    Response after adding documents
    """
    added_count: int
    total_documents: int
    message: str

class ExtractQuery(BaseModel):
    """
    Model for extraction request
    """
    query: str = Field(..., min_length=1, max_length=1000, description="Query text for information extraction")

class FormattedActions(BaseModel):
    """
    Model for formatted actions in extraction response
    """
    action: str
    target: str
    information: Optional[str] = None

class ExtractedInfo(BaseModel):
    """
    Model for extracted information
    """
    action_list: List[str] = Field(default_factory=list, description="List of actions extracted from the query")
    specific_actions: List[FormattedActions] = Field(default_factory=list, description="List of specific actions extracted from the query")

class ExtractResponse(BaseModel):
    """
    Model for extraction response
    """
    extracted_info: ExtractedInfo
    processing_time_ms: float

class GenerateQuery(BaseModel):
    """
    Model for generation request
    """
    query: str = Field(..., min_length=1, max_length=10000, description="Query text for generation")

class GeneratedContent(BaseModel):
    """
    Model for generated content in generation response
    """
    narration: str

class GenerateResponse(BaseModel):
    """
    Model for generation response
    """
    content: GeneratedContent
    processing_time_ms: float

class GenerateSummaryQuery(BaseModel):
    """
    Model for summary generation request
    """
    query: str = Field(..., min_length=1, max_length=5000, description="Query text for summary generation")

class GenerateSummaryResponse(BaseModel):
    """
    Model for summary generation response
    """
    content: str
    processing_time_ms: float

class PathFinderQuery(BaseModel):
    """
    Model for path finding request
    """
    query: str = Field(..., min_length=1, max_length=500, description="Query text for path finding")

class PathFinderResponse(BaseModel):
    """
    Model for path finding response
    """
    value: int = Field(..., description="Value of the path finding result")
    processing_time_ms: float

class HasJoinedQuery(BaseModel):
    """
    Model for has joined query
    """
    narration: str = Field(..., min_length=1, max_length=5000, description="Narration to evaluate")

class HasJoinedResponse(BaseModel):
    """
    Model for has joined response
    """
    joined: bool = Field( ..., description="If a character has joined")
    target: str = Field( ..., min_length=0, max_length=500, description="The character that has joined")