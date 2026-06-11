import {api} from './ApiCalls';

// Funzioni base
export const api_caller = {

  init: async (architecture) => {
    const params = new URLSearchParams({ architecture }).toString();
    return await api.get(`/init?${params}`);
  },
  
  post_character: async (characterData) => {
    return await api.post('/character', characterData);
  },
  
  get_character: async () => {
    return await api.get('/character');
  },
  
  post_objective: async (data) => {
    const params = new URLSearchParams(data).toString();
    return await api.post(`/objective?${params}`);
  },

  start: async () => {
    return await api.post('/start');
  },

  first_scene: async () => {
    return await api.post('/first_scene');
  },

  action: async (content) => {
     return await api.post('/action', { content: content });
  },
  
  switch_equip: async(equip, slot) => {
    console.log("Calling the function: " + equip)
    console.log("Calling the function: " + slot)
    return (await api.post(`/equipment/switch`, { equipName: equip, position: slot })).character;
  },

  get_inventory: async () => {
    try{
      return await api.get('/inventory');
    }
    catch(e){
      console.log("Error: ", e);
      return null;
    }
    
  }

};
