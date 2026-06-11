import logging
import pickle
import requests
import numpy as np
from typing import List, Dict, Optional
from app import config

class TextGenerator:
    def __init__(self):
        self.system_prompt = None
        self.generator_preset = None
        self.generator_temperature = 0.9
        self.generator_top_p = 0.9
        self.system_prompt_summary = None
        self.summary_preset = None
        self.summary_temperature = 0.9
        self.summary_top_p = 0.9
        self.logger = self._setup_logger()

    def set_generator_system_info(self, info):
        """Set the system prompt for the generation model"""
        self.system_prompt = info.get("system_prompt")
        self.generator_preset =  info.get("preset")
        self.generator_temperature = info.get("temperature")
        self.generator_top_p = info.get("top-p")

    def set_summary_system_info(self, info):
        """Set the system prompt for the generation model"""
        self.system_prompt_summary = info.get("system_prompt")
        self.summary_preset =  info.get("preset")
        self.summary_temperature = info.get("temperature")
        self.summary_top_p = info.get("top-p")

    def _setup_logger(self):
        """Configure logger"""
        logging.basicConfig(level=logging.INFO)
        return logging.getLogger(__name__)

    def initialize_model(self):
        """Test the generation model to ensure it's reachable"""
        try:
            self.test_model()
            logging.info("Generation model reachable and ready")
        except Exception as e:
            logging.error(f"Failed to initialize generation model: {e}")
            raise e
    
    def generate(self, query: str) -> Dict[str, str]:
        """Generate the response to the query"""
        logging.info("Starting generation with query: " + query)
        try:
            response = requests.post(
                config.GENERATION_URL,
                json={
                    "model": config.GENERATION_MODEL,
                    "messages": [
                        {"role": "system", "content": self.system_prompt}, 
                        {"role": "user", "content": query}
                        ],
                    "preset": self.generator_preset,
                    "temperature": self.generator_temperature,
                    "top_p": self.generator_top_p,
                },
                timeout=300
            )
            response.raise_for_status()
            generated_text = response.json().get("choices", [{}])[0].get("message", {}).get("content", "")
            logging.info("Generated text: " + generated_text)
            logging.info("Generation successful")
        except Exception as e:
            logging.error(f"Generation failed: {e}")
            raise e
        
        return {"content": generated_text}

    def test_model(self):
        """Test if the generation model is reachable"""
        try:
            response = requests.post(
                config.GENERATION_URL,
                json={
                    "model": config.GENERATION_MODEL,
                    "messages": [
        
                                 {"role": "user", "content": "Test content"}
                                 ]
                },
                timeout=5
            )
            response.raise_for_status()
            logging.info("Generation model test successful")
        except Exception as e:
            logging.error(f"Generation model test failed: {e}")
            raise e
    
    def get_status(self) -> Dict[str, str]:
        """Get status of the generator"""
        try:
            self.test_model()
            return {"initialized": True}
        except Exception as e:
            logging.error(f"Generation model status check failed: {e}")
            return {"initialized": False}
    
    def generate_summary(self, query: str) -> Dict[str, str]:
        """Generate a summary based on the query"""
        try:
            response = requests.post(
                config.GENERATION_URL,
                json={
                    "model": config.GENERATION_MODEL,
                    "messages": [
                        {"role": "system", "content": self.system_prompt_summary}, 
                        {"role": "user", "content": query}
                        ],
                    "preset": self.summary_preset,
                    "temperature": self.summary_temperature,
                    "top_p": self.summary_top_p,
                },
                timeout=300
            )
            response.raise_for_status()
            generated_text = response.json().get("choices", [{}])[0].get("message", {}).get("content", "")
            logging.info("Generation successful")
        except Exception as e:
            logging.error(f"Generation model test failed: {e}")
            raise e
        return {"content": generated_text}