import {create} from 'zustand';
import json from './assets/program_data/data.json'

const data = JSON.parse(JSON.stringify(json));

const useGameState = create((set,get) => ({
  // Stato
  loading: false,
  loadingBg: false,

  user_id: null,

  app_state: 0,

  archetype: "",
  equippedItems: {},
  inventory: [],
  characterName: "",
  goal: "",
  character: {},
  party: [],

  
  setUserId: (id) => set({ user_id: id }),

  // Azioni
  setLoading: (value) => set({ loading: value }),
  setLoadingBg: (value) => set({ loadingBg: value }),
  
  // Azioni utili per operazioni comuni
  startLoading: () => set({ loading: true }),
  stopLoading: () => set({ loading: false }),
  
  startBgLoading: () => set({ loadingBg: true }),
  stopBgLoading: () => set({ loadingBg: false }),

  setAppState: (value) => {
    console.log("current app_state:", get().app_state); // Debug log
    console.log("Setting app_state to:", value); // Debug log
    set({ app_state: value })
    console.log("new app_state:", get().app_state); // Debug log
},

  setArchetype: (newArchetype) => {
    set({ archetype: newArchetype })
    const selectedArchetype = data.archetypes.find((a) => a.name === newArchetype);
  
  // ✅ Verifica che selectedArchetype esista
    if (!selectedArchetype) {
        console.error("Archetype not found:", newArchetype);
        return;
    }
    
    // ✅ Verifica che equippedItems esista e non sia vuoto
    if (!selectedArchetype.equippedItems || selectedArchetype.equippedItems.length === 0) {
        console.warn("No equipped items for archetype:", newArchetype);
        setEquippedItems({});
        return;
    }
    
    // ✅ Costruisci equippedItems dinamicamente
    const newEquippedItems = {};
    
    selectedArchetype.equippedItems.forEach((item) => {
        newEquippedItems[item.equippedInSlot] = {
        id: item.name,
        name: item.name,
        description: item.description,
        bonuses: item.bonuses,
        compatibleSlots: item.compatibleSlots
        };
    });
    
     set({ 
    archetype: newArchetype,
    equippedItems: newEquippedItems 
  })
},

setEquippedItems: (newEquippedItems) => set({ equippedItems: newEquippedItems }),

setInventory: (newInventory) => 
{
  const safeInventory = newInventory.filter(item => item != null)
  set({ inventory: safeInventory })
},

setCharacterName: (newCharacterName) => set({ characterName: newCharacterName }),

setGoal: (newGoal) => set({ goal: newGoal }),

setCharacter: (newCharacter) => set({character: newCharacter}),

addPartyMember: (newPartyMember) => {
  if (get().party.find(member => member.name === newPartyMember.name)) {
    console.warn("Party member already exists:", newPartyMember.name);
    return;
  }
  set({party: [...get().party, newPartyMember]})
},
setUserId: (id) => {console.log("Setting user ID to:", id); set({ user_id: id })}
}));




export { useGameState };


