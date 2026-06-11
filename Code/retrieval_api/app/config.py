import os
from pathlib import Path
from dotenv import load_dotenv

# Load environment variables
load_dotenv()

# Base paths
BASE_DIR = Path(__file__).parent.parent
DATA_DIR = BASE_DIR / "data"
INDEX_DIR = BASE_DIR / "indexes"

# Create directories if they don't exist
DATA_DIR.mkdir(exist_ok=True)
INDEX_DIR.mkdir(exist_ok=True)

# API settings
API_TITLE = "Retrieval API"
API_DESCRIPTION = "API for hybrid document retrieval using BM25, embeddings, and reranking"
API_VERSION = "1.0.0"

# Retrieval settings
TOP_N = int(os.getenv("TOP_N", "100"))
TOP_M = int(os.getenv("TOP_M", "20"))
TOP_K = int(os.getenv("TOP_K", "7"))
N_TREES = int(os.getenv("N_TREES", "10"))

# Retrieval Model settings
EMBEDDINGS_URL = os.getenv("EMBEDDINGS_URL", "http://127.0.0.1:1234/v1/embeddings")
EMBEDDING_MODEL = os.getenv("EMBEDDING_MODEL", "text-embedding-embeddinggemma-300m")
RERANKER_MODEL = os.getenv("RERANKER_MODEL", "jinaai/jina-reranker-v3")
SPACY_MODEL = os.getenv("SPACY_MODEL", "en_core_web_md")

# Information Extraction settings
EXTRACTION_URL = os.getenv("EXTRACTION_URL", "http://127.0.0.1:1234/v1/chat/completions")
EXTRACTION_MODEL = os.getenv("EXTRACTION_MODEL", "llama-3.1-8b-instruct")
PATHFINDER_MODEL = os.getenv("PATHFINDER_MODEL", "llama-3.1-8b-instruct")

# Generator settings
GENERATION_URL = os.getenv("GENERATION_URL", "http://127.0.0.1:1234/v1/chat/completions")
GENERATION_MODEL = os.getenv("GENERATION_MODEL", "llama-3.1-8b-instruct")

# Paths for saved indexes
ANNOY_INDEX_PATH = INDEX_DIR / "annoy_index.ann"
METADATA_PATH = INDEX_DIR / "metadata.pkl"

# Test models reachability on startup
TEST_URL = os.getenv("TEST_URL", "http://127.0.0.1:1234/v1/models")