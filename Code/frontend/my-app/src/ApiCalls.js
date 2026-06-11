// Base URL della tua API
const API_BASE_URL = 'http://localhost:5266';

// Configurazione comune per le richieste
const defaultHeaders = {
  'Content-Type': 'application/json',
};

// Funzioni base
export const api = {
  get: async (endpoint) => {
    const response = await fetch(`${API_BASE_URL}${endpoint}`);
    if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
    const text = await response.text();
    if (!text) return null;  // Risposta vuota
    return JSON.parse(text);
  },
  
  post: async (endpoint, data) => {
    console.log("Body: " + JSON.stringify(data))
    const response = await fetch(`${API_BASE_URL}${endpoint}`, {
      method: 'POST',
      headers: defaultHeaders,
      body: JSON.stringify(data),
    });
    if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
    const text = await response.text();
    console.log("Text: " + text)
    if (!text) return null;  // Risposta vuota
    console.table("JsonOBJ: " , JSON.parse(text))
    return JSON.parse(text);
  },
  
  put: async (endpoint, data) => {
    const response = await fetch(`${API_BASE_URL}${endpoint}`, {
      method: 'PUT',
      headers: defaultHeaders,
      body: JSON.stringify(data),
    });
    if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
    const text = await response.text();
    if (!text) return null;  // Risposta vuota
    return JSON.parse(text);
  },
  
  delete: async (endpoint) => {
    const response = await fetch(`${API_BASE_URL}${endpoint}`, {
      method: 'DELETE',
    });
    if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
    const text = await response.text();
    if (!text) return null;  // Risposta vuota
    return JSON.parse(text);
  },
};
