import {api_caller} from './ApiCaller';
import { useGameState } from './GameState';
var character = null;


export const GameManager = {

    setCharacter: async (newCharacter) => {
        await api_caller.post_character(newCharacter);
    },

    getCharacter: async () => {
        const character = await api_caller.get_character();
        useGameState.getState().setCharacter(character)
        return character;
    },

    setObjective: async (objectiveData) => {
        const data = {};
        data.objective = objectiveData.goal;
        data.motivation = objectiveData.motivation;
        return await api_caller.post_objective(data);
    },

    startGame: async (architecture) => {
        return await api_caller.start(architecture)
    },

    firstScene: async () => {
        return await api_caller.first_scene()
    },

    sendAction: async (action) => {
        return await api_caller.action(action);
    },
    
    switchEquipment: async (equip, slot) => {
        console.log("Inside the caller: " + equip)
        console.log("Inside the caller: " + slot)
        const character = await api_caller.switch_equip(equip, slot)
        console.table("Character: " , character)
        useGameState.getState().setCharacter(character)
    },

    getInventory: async () => {
        const inventory = await api_caller.get_inventory();
        console.log("new inventory: ", inventory)
        const setInventory = useGameState.getState().setInventory(inventory)
    }
};