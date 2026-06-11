import logging
import pickle
import requests
import numpy as np
from typing import List, Dict, Optional
from rank_bm25 import BM25L
import spacy
from annoy import AnnoyIndex
from transformers import AutoModel
import math
import json

from app import config

class DocumentRetriever:
    """
    Document retrieval and reranking system combining:
    - Lexical search (BM25)
    - Semantic search (embeddings with Annoy)
    - Fusion (TM2C2)
    - Final reranking (Jina)
    """
    
    def __init__(self):
        """Initialize the retriever with empty state"""
        self.docs: Dict[str, str] = {}
        self.bm25 = None
        self.annoy_index = None
        self.annoy_id_to_docid = {}
        self.nlp = None
        self.jina_reranker = None
        self.embedding_dim = None
        self.logger = self._setup_logger()
        self.initialized = False
        
    def _setup_logger(self):
        """Configure logger"""
        logging.basicConfig(level=logging.INFO)
        return logging.getLogger(__name__)
    
    def initialize_models(self, load_reranker: bool = True):
        """Load NLP models"""
        self.logger.info("Loading spaCy model...")
        try:
            self.nlp = spacy.load(config.SPACY_MODEL)
        except Exception as e:
            self.logger.error(f"Error loading spaCy model: {e}")
            raise

        if load_reranker:
            self.logger.info(f"Loading reranker {config.RERANKER_MODEL}...")
            self.jina_reranker = AutoModel.from_pretrained(
                'jinaai/jina-reranker-v3',
                trust_remote_code=True,  # Essenziale per questo modello
                dtype="auto"
            )
            self.jina_reranker.eval()
            self.logger.info("✅ Reranker loaded and ready")  # ← AGGIUNGI
        else:
            self.jina_reranker = None
            self.logger.warning("⚠️ Reranker not loaded (load_reranker=False)")                
        self.initialized = True
        self.logger.info("Models initialized successfully")
    
    def tokenize(self, text: str) -> List[str]:
        """Tokenize and lemmatize text"""
        if not self.nlp:
            raise ValueError("Models not initialized")
        
        doc = self.nlp(text.lower())
        tokens = [
            token.lemma_ for token in doc
            if not token.is_stop and not token.is_punct and not token.is_space
        ]
        return tokens
    
    def _get_embeddings_batch(self, texts: List[str]) -> List[Dict]:
        """Get embeddings from server"""
        if not texts:
            return []
        
        data = {
            "model": config.EMBEDDING_MODEL,
            "input": texts
        }
        
        try:
            response = requests.post(config.EMBEDDINGS_URL, json=data, timeout=30)
            response.raise_for_status()
            return response.json()["data"]
        except Exception as e:
            self.logger.error(f"Error fetching embeddings: {e}")
            raise
    
    def _normalize_vector(self, v: List[float]) -> np.ndarray:
        """L2 normalize a vector"""
        v = np.array(v)
        norm = np.linalg.norm(v)
        return v / norm if norm > 0 else v
    
    def _min_max_normalize(self, scores: List[float]) -> List[float]:
        """Min-max normalize scores"""
        if not scores:
            return scores
        
        M, m = max(scores), min(scores)
        if M == m:
            return [0.0 for _ in scores]
        
        return [(s - m) / (M - m) for s in scores]
    
    def build_index(self, docs: Dict[str, str]):
        """
        Build indexes from documents
        
        Args:
            docs: Dictionary of document IDs and texts
        """
        self.docs = docs
        self.logger.info(f"Building indexes for {len(docs)} documents...")
        
        # Tokenize for BM25
        self.logger.info("Tokenizing documents...")
        tokenized_docs = [self.tokenize(text) for text in docs.values()]
        
        # Build BM25
        self.logger.info("Building BM25 index...")
        self.bm25 = BM25L(tokenized_docs)
        
        # Get embeddings
        self.logger.info("Generating embeddings...")
        embeddings_response = self._get_embeddings_batch(list(docs.values()))
        
        # Normalize embeddings
        doc_embeddings = {}
        for response, doc_id in zip(embeddings_response, docs.keys()):
            doc_embeddings[doc_id] = self._normalize_vector(response["embedding"])
        
        # Build Annoy index
        self.embedding_dim = len(next(iter(doc_embeddings.values())))
        self.logger.info(f"Building Annoy index (dim={self.embedding_dim})...")
        
        self.annoy_index = AnnoyIndex(self.embedding_dim, 'angular')
        
        for i, (doc_id, embedding) in enumerate(doc_embeddings.items()):
            self.annoy_index.add_item(i, embedding)
            self.annoy_id_to_docid[i] = doc_id
        
        self.annoy_index.build(config.N_TREES)
        
        # Save indexes
        self._save_indexes()
        self.logger.info("Indexes built and saved successfully")
    
    def _save_indexes(self):
        """Save indexes to disk"""
        # Save Annoy index
        self.annoy_index.save(str(config.ANNOY_INDEX_PATH))
        
        # Save metadata
        metadata = {
            'annoy_id_to_docid': self.annoy_id_to_docid,
            'docs': self.docs,
            'embedding_dim': self.embedding_dim
        }
        with open(config.METADATA_PATH, 'wb') as f:
            pickle.dump(metadata, f)
        
        self.logger.info(f"Indexes saved to {config.INDEX_DIR}")
    
    def load_indexes(self):
        """Load indexes from disk"""
        if not config.ANNOY_INDEX_PATH.exists():
            raise FileNotFoundError("No saved indexes found")
        
        # Load metadata
        with open(config.METADATA_PATH, 'rb') as f:
            metadata = pickle.load(f)
        
        self.annoy_id_to_docid = metadata['annoy_id_to_docid']
    
        temp_docs = metadata['docs']
        for doc_id, text in temp_docs.items():
            if doc_id not in self.docs:
                self.docs[doc_id] = text
        self.embedding_dim = metadata['embedding_dim']
        
        # Rebuild BM25
        self.logger.info("Rebuilding BM25 index...")
        tokenized_docs = [self.tokenize(text) for text in self.docs.values()]
        self.bm25 = BM25L(tokenized_docs)
        
        # Load Annoy
        self.annoy_index = AnnoyIndex(self.embedding_dim, 'angular')
        self.annoy_index.load(str(config.ANNOY_INDEX_PATH))
        
        self.logger.info(f"Loaded {len(self.docs)} documents from indexes")
    
    def search(self, query: str, top_k: int = config.TOP_K, alpha: float = 0.7) -> List[Dict]:
        """
        Complete search pipeline
        
        Args:
            query: Search query
            top_k: Number of results to return
            alpha: Fusion weight for sparse scores
            
        Returns:
            List of documents with scores
        """
        self.logger.info("Retrieval started")
        if not self.bm25 or not self.annoy_index:
            raise ValueError("Indexes not loaded. Call load_indexes() or build_index() first.")
        
        self.logger.info(f"Searching for: '{query}'")
        
        # Tokenize query
        tokenized_query = self.tokenize(query)
        self.logger.info("Query tokenized")
        # Get query embedding
        query_embedding_response = self._get_embeddings_batch([query])
        query_embedding = self._normalize_vector(query_embedding_response[0]['embedding'])
        self.logger.info("Query embedded")
        # BM25 retrieval
        sparse_scores = self.bm25.get_scores(tokenized_query)
        sparse_rank = [
            {'id': doc_id, 'score': score}
            for score, doc_id in zip(sparse_scores, self.docs.keys())
        ]
        self.logger.info("Sparse rank")
        # Annoy retrieval
        dense_idx, distances = self.annoy_index.get_nns_by_vector(
            query_embedding, config.TOP_N, include_distances=True
        )
        
        # Convert distances to scores
        dense_scores = [(1 - d / 2) for d in distances]
        dense_scores_norm = self._min_max_normalize(dense_scores)
        
        dense_rank = [
            {'id': self.annoy_id_to_docid[i], 'score': score}
            for i, score in zip(dense_idx, dense_scores_norm)
        ]
        self.logger.info("Dense rank")
        # Sort and truncate
        sparse_rank = sorted(sparse_rank, key=lambda x: x['score'], reverse=True)[:config.TOP_N]
        dense_rank = sorted(dense_rank, key=lambda x: x['score'], reverse=True)[:config.TOP_N]
        
        # Fuse rankings
        fused_rank = self._tm2c2_fusion(sparse_rank, dense_rank, alpha)
        self.logger.info("Fused rank")
        # Rerank if available
        if self.jina_reranker:
            fused_rank = self._rerank(query, fused_rank[:config.TOP_M], top_k)
        else:
            fused_rank = fused_rank[:top_k]
        
        return fused_rank
    
    def _tm2c2_fusion(self, sparse_rank: List[Dict], dense_rank: List[Dict], 
                      alpha: float) -> List[Dict]:
        """TM2C2 fusion of sparse and dense rankings"""
        sparse_dict = {d['id']: d['score'] for d in sparse_rank}
        dense_dict = {d['id']: d['score'] for d in dense_rank}
        
        all_ids = set(sparse_dict) | set(dense_dict)
        
        final_rank = []
        for doc_id in all_ids:
            sparse_score = sparse_dict.get(doc_id, 0.0)
            dense_score = dense_dict.get(doc_id, 0.0)
            fused_score = alpha * sparse_score + (1 - alpha) * dense_score
            
            final_rank.append({
                'id': doc_id,
                'score': fused_score,
                'text': self.docs.get(doc_id, '')
            })
        
        return sorted(final_rank, key=lambda x: x['score'], reverse=True)
    
    def _rerank(self, query: str, documents: List[Dict], top_k: int) -> List[Dict]:
        self.logger.info("Reranking started")
        if not self.jina_reranker or not documents:
            return documents
        
        doc_texts = [doc["text"] for doc in documents]
        doc_ids = [doc["id"] for doc in documents]
        
        # Esegui il reranking
        results = self.jina_reranker.rerank(
            query=query,
            documents=doc_texts,
            top_n=top_k,
        )
        
        reranked = []
        for result in results:
            # Normalizza il punteggio da logit (-inf, +inf) a probabilità (0, 1)
            raw_score = result['relevance_score']
            normalized_score = 1 / (1 + math.exp(-raw_score))  # Usa math.exp
            
            reranked.append({
                'id': doc_ids[result['index']],
                'text': result['document'],
                'score': normalized_score
            })
        logging.info(f"Retrieval Results: {str(reranked)}")
        reranked = [doc for doc in reranked if doc['score'] > 0.5]  # Filtra risultati con punteggio troppo basso
        self.logger.info(f"Reranking completed, normalized scores in [0,1]")
        return reranked
    
    def get_status(self) -> dict:
        """Get system status"""
        try:
            return {
            "initialized": self.initialized,
            "documents_count": len(self.docs),
            "indexes_loaded": self.bm25 is not None and self.annoy_index is not None,
            "reranker_available": self.jina_reranker is not None
        }
        except Exception as e:
            self.logger.error(f"Error occurred while fetching status: {e}")
            return {
                "initialized": False,
                "documents_count": 0,
                "indexes_loaded": False,
                "reranker_available": False
            }

    def write_docs(self):

        if not self.docs or len(self.docs) == 0:
            self.logger.warning("No documents to write")
            return

        list_for_json = [{"id": k, "text": v} for k, v in self.docs.items()]

        # Scrivi su file JSON
        try:
            with open(config.DATA_DIR / "documents.json", "w", encoding="utf-8") as f:
                    json.dump(list_for_json, f, ensure_ascii=False, indent=4)
            self.logger.info(f"Documents written to {config.DATA_DIR / 'documents.json'}")
        except Exception as e:
            self.logger.error(f"Error occurred while writing documents: {e}")

    def load_docs(self):
        try:
            with open(config.DATA_DIR / "documents.json", "r", encoding="utf-8") as f:
                data = json.load(f)
            self.docs = {doc["id"]: doc["text"] for doc in data}
            self.logger.info(f"Loaded {len(self.docs)} documents")
        except FileNotFoundError:
            self.logger.warning("Documents file not found")
        except Exception as e:
            self.logger.error(f"Error occurred while loading documents: {e}")