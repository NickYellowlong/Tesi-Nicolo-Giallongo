import logging
import pickle
import requests
import numpy as np
from typing import List, Dict, Optional
from app import config

class InformationExtractor:
    """
        Information extractor class that interacts with the extraction model to extract relevant information from queries
    """

    def __init__(self):
        self.system_prompt_extractor = None
        self.extractor_preset = None
        self.extractor_temperature = 0.9
        self.extractor_top_p = 0.9

        self.system_prompt_pathfinder = None
        self.pathfinder_preset = None
        self.pathfinder_temperature = 0.9
        self.pathfinder_top_p = 0.9

        self.system_prompt_has_joined = None
        self.has_joined_preset = None
        self.has_joined_temperature = 0.9
        self.has_joined_top_p = 0.9
        self.logger = self._setup_logger()
    
    def set_estractor_system_info(self, info):
        """Set the system prompt for the extraction model"""
        self.system_prompt_extractor = info.get("system_prompt")
        self.extractor_preset =  info.get("preset")
        self.extractor_temperature = info.get("temperature")
        self.extractor_top_p = info.get("top-p")

    def set_pathfinder_system_info(self, info):
        self.system_prompt_pathfinder = info.get("system_prompt")
        self.pathfinder_preset =  info.get("preset")
        self.pathfinder_temperature = info.get("temperature")
        self.pathfinder_top_p = info.get("top-p")

    def set_has_joined_system_info(self, info):
        self.system_prompt_has_joined = info.get("system_prompt")
        self.has_joined_preset =  info.get("preset")
        self.has_joined_temperature = info.get("temperature")
        self.has_joined_top_p = info.get("top-p")

    def _setup_logger(self):
        """Configure logger"""
        logging.basicConfig(level=logging.INFO)
        return logging.getLogger(__name__)

    def initialize_model(self):
        """Test the extraction model to ensure it's reachable"""
        try:
            self.test_model()
            logging.info("Information extractor model reachable and ready")
        except Exception as e:
            logging.error(f"Failed to initialize information extractor: {e}")
            raise e
    
    def extract(self, query: str) -> Dict[str, str]:
        """Extract relevant information from the query"""
        try:
            logging.info("Estract method called. Calling the extraction model.")
            response = requests.post(
                config.EXTRACTION_URL,
                json={
                    "model": config.EXTRACTION_MODEL,
                    "messages": [
                        {"role": "system", "content": self.system_prompt_extractor}, 
                        {"role": "user", "content": query}
                        ],
                    "preset": self.extractor_preset,           # Per Structured Output
                    "temperature": self.extractor_temperature,                # Specificato esplicitamente
                    "top_p": self.extractor_top_p,
                },
                timeout=120
            )
            response.raise_for_status()
            extracted_info = response.json().get("choices", [{}])[0].get("message", {}).get("content", "")
            logging.info("Information extraction successful")
        except Exception as e:
            logging.error(f"Information extraction failed: {e}")
            raise e
        logging.info(f"Extracted info:\n{extracted_info}")
        return extracted_info
    
    def find_path(self, query: str) -> int:
        """Extract relevant information from the query"""
        try:
            logging.info(f"Sending pathfinding request")
            response = requests.post(
                config.EXTRACTION_URL,
                json={
                    "model": config.PATHFINDER_MODEL,
                    "messages": [
                        {"role": "system", "content": self.system_prompt_pathfinder}, 
                        {"role": "user", "content": query}
                        ],
                    "preset": self.pathfinder_preset,
                    "temperature": self.pathfinder_temperature,
                    "top_p": self.pathfinder_top_p,
                },
                timeout=120
            )
            response.raise_for_status()
            logging.info(f"Reasoning: {response.json().get("choices", [{}])[0].get("message", {}).get("reasoning", "")}")
            extracted_info = response.json().get("choices", [{}])[0].get("message", {}).get("content", "")
            logging.info(f"Information extraction successful: {extracted_info}")
        except Exception as e:
            logging.error(f"Information extraction failed: {e}")
            raise e
        
        return extracted_info
    
    def test_model(self):
        """Test if the extraction model is reachable"""
        try:
            response = requests.post(
                config.EXTRACTION_URL,
                json={
                    "model": config.EXTRACTION_MODEL,
                    "messages": [
                                 {"role": "user", "content": "Test content"}
                                 ]
                },
                timeout=5
            )
            response.raise_for_status()
            logging.info("Extraction model test successful")
        except Exception as e:
            logging.error(f"Extraction model test failed: {e}")
            raise e
    
    def get_status(self) -> Dict[str, str]:
        """Get status of the information extractor"""
        try:
            self.test_model()
            return {"initialized": True}
        except Exception as e:
            logging.error(f"Information extractor status check failed: {e}")
            return {"initialized": False}
    
    def has_joined(self, narration: str):
        """Check if the user has joined the game based on the narration"""
        try:
            logging.info(f"Sending has_joined request")
            json = {
                    "model": config.PATHFINDER_MODEL,
                    "messages": [
                        {"role": "system", "content": self.system_prompt_has_joined}, 
                        {"role": "user", "content": narration}
                        ],
                    "preset": self.has_joined_preset,
                    "temperature": self.has_joined_temperature,
                    "top_p": self.has_joined_top_p,
                }
            response = requests.post(
                config.EXTRACTION_URL,
                json=json,
                timeout=120
            )
            logging.info(f"json: {json}")
            response.raise_for_status()
            has_joined = response.json().get("choices", [{}])[0].get("message", {}).get("content", {})
            logging.info(f"has_joined response: {has_joined}")
        except Exception as e:
            logging.error(f"has_joined extraction failed: {e}")
            raise e
        
        return has_joined