import { useState, useRef  } from 'react'
import { useDroppable, useDraggable, DndContext, MouseSensor,
  TouchSensor, useSensor, useSensors, closestCenter, DragOverlay, PointerSensor  } from '@dnd-kit/core';
import json from './assets/program_data/data.json'
import './App.css'
import { GameManager } from './GameManager';
import {useGameState} from './GameState';
import { api_caller } from './ApiCaller';
import { createPortal } from 'react-dom';


// state = 0 : welcome
// state = 1 : character archetype selection
// state = 2 : character description
// state = 3 : goal and motivation selection
// state = 4 : introductive scene
// state = 5 : narrative scene

const characteristics = [
  {"name": "strength", "description": "Physical power and force."},
  {"name": "resistance", "description": "Ability to withstand damage and adversity."},
  {"name": "agility", "description": "Skill and agility in physical actions."},
  {"name": "knowledge", "description": "Understanding and awareness of the world."},
  {"name": "charisma", "description": "Ability to influence and inspire others."},
  {"name": "instinct", "description": "Natural intuition and reflexes."}
]

const data = JSON.parse(JSON.stringify(json));

const capitalize = (str) => {
  if (!str || typeof str !== 'string') return '';
  return str
    .replace(/_/g, ' ')  // sostituisce underscore con spazi
    .split(' ')
    .map(word => word.charAt(0).toUpperCase() + word.slice(1).toLowerCase())
    .join(' ');
};

const toSnakeCase = (str) => {
  if (!str || typeof str !== 'string') return '';
  return str
    .toLowerCase()                // tutto minuscolo
    .replace(/ /g, '_');          // spazi diventano underscore
};

const right_hand = {
  id: 'right_hand',
  name: 'Right Hand',
  type: 'right_hand'
}

  const head = {
  id: 'head',
  name: 'Head',
  type: 'head'
}

  const left_hand = {
  id: 'left_hand',
  name: 'Left Hand',
  type: 'left_hand'
}

const armor = {
  id: 'armor',
  name: 'Armor',
  type: 'armor'
}

const accessory = {
  id: 'accessory',
  name: 'Accessory',
  type: 'accessory'
}

var character = {}
const goalAndMotivation = {}
var narration = "";


function Test() {
  return null;
}

function App() {
  // ========== STATI DA STORE ==========
  const equippedItems = useGameState((state) => state.equippedItems);
  const inventory = useGameState((state) => state.inventory);
  const character = useGameState((state) => state.character);
  const archetype = useGameState((state) => state.archetype)

  // ========== LOGICA CHARACTER ==========
  const isCharacter = (character != null && Object.keys(character).length > 0);
  console.log("Personaggio:", character);
  console.log("isCharacter:", isCharacter);

  // ========== RENDER ==========
  console.log("Archetype: ", archetype )
  return (
      <div style={{
        display: 'flex',
        flexDirection: 'row',
        alignItems: 'center',
        justifyContent: 'center',
        margin: '50px',
        paddingLeft: '1600px',
        paddingTop: '50px',
      }}>
        <Book />
        <div style={{ display: isCharacter ? 'block' : 'none' , position: 'absolute', right: '400px', top: '10px', alignItems: 'center', justifyContent: 'center', zIndex: 10}}>
          <div>{isCharacter ? (<>
            <div className='title-box1' style={{ position: 'relative', zIndex: 3, alignItems : 'center', justifyContent : 'center', marginLeft: '20px', paddingTop: '60px' }}>
              {character.character.name}
            </div>
            <div className='title-box2' style={{ marginTop: '-80px', position: 'relative', zIndex: 2 , marginLeft: '90px' }}>
              {archetype ?? 'Warrior'}
            </div></>
            ) : (<></>)}
            </div>
            <div style={{ marginTop: '-50px',  position: 'relative', zIndex: 1 }}>
            <CharacterSheet
            archetype={archetype ?? 'Warrior'}
            equippedItems={equippedItems}
            inventory={inventory}
            complete={true}
            character={isCharacter ? character : null}
          />
          </div>
          </div>
        </div>
  );
}

function Book() {
  return (
  <div className = "book">
    <div className = "page_left">
        <PageLeftContent/>
    </div>
    <div className = "page_right">
        <PageRightContent/>
    </div>
  </div>);
}

function PageLeftContent() {
  const state = useGameState((state) => state.app_state);
  const loading = useGameState((state) => state.loading);

  if (loading)
    return (
      <div style={{display : 'flex', flexDirection : 'column', alignItems : 'center', justifyContent : 'center', height: '100%'}}>
        <h2>Loading...</h2>
        <p>Please wait while we prepare your adventure.</p>
      </div>
    );
  switch(state) {
    case 0: return <WelcomeScreen/>;
    case 1: return(<SelectCharacterArchetype/>);
    case 2: return(<CharacterDescriptionPresentation/>);
    case 3: return(<GoalSelection/>);
    case 4: return(<NarrationText/>);
    case 5: return(<NarrationText/>);
    case 6: return(<NarrationText/>);
    default: return (<h1>Invalid State : {state}</h1>);
  }
}

function PageRightContent() {
  const app_state = useGameState((state) => state.app_state);
  const loading = useGameState((state) => state.loading);
   if (loading)
    return (
      <div style={{display : 'flex', flexDirection : 'column', alignItems : 'center', justifyContent : 'center', height: '100%'}}>
        <h2>Loading...</h2>
        <p>Please wait while we prepare your adventure.</p>
      </div>
    );
   switch(app_state) {
    case 0: return <StoryIntroductionScreen/>;
    case 1: return(<VisualizeCharacterArchetype/>);
    case 2: return(<CharacterDescriptionInsertion/>);
    case 3: return(<MotivationInsertion/>);
    case 4: return(<IntroductiveImageScreen/>);
    case 5: return(<GameInterface/>);
    case 6: return(<IDPage/>);
    default: return (<h1>Invalid State : {state}</h1>);
  }
}

function WelcomeScreen() {
  const setState = useGameState((state) => state.setAppState);
  const startLoading = useGameState((state) => state.startLoading);
  const stopLoading = useGameState((state) => state.stopLoading);
  const setUserId = useGameState((state) => state.setUserId);
  const user_id = useGameState((state) => state.user_id);
  const [isChecked, setIsChecked] = useState(false);
  return (      
        <>
          <h1>Welcome to QuesTale!</h1>
          <br />
          <h2>An Interactive Fantasy Role Playing Application</h2>
          <br />
          <br />
          <p>This appliaction uses the generative power of LLM to create immersive storytelling experiences.</p>
          <br />
          <br />
          <p>Click the button below to start your adventure!</p>
          <br />
          <br />
          <br />
          <br />
          <br />
          <div style={{display: 'flex', flexDirection: 'column', alignItems: 'center'}}>
            <button className='app_button' onClick={async() => {
              startLoading()
              const init = await api_caller.init(isChecked);
              console.log("Init data:", init);
              setUserId(init.user_id);
              const nuovoId = useGameState.getState().user_id;
              console.log("User ID dopo set:", nuovoId);
              stopLoading();
            setState(1)
            }}>
              Start
          </button>
          <br />
          <br />
        <input 
          type="checkbox" 
          checked={isChecked}
          onChange={(e) => setIsChecked(e.target.checked)}
          style={{ width: '30px', height: '30px' }}
        /></div>
        </>
      )
}

function StoryIntroductionScreen() {
  return (
    <div style={{display : 'flex', flexDirection : 'column', alignItems : 'center', justifyContent : 'center', height: '100%', padding: '20px', boxSizing: 'border-box', margin: '10px', fontSize: '30px', fontStyle: 'italic', textAlign: 'center'}}>
    <br />
    <p style={{fontSize: '30px', fontStyle: 'italic', textAlign: 'center'}}>Within the depths of the <strong>Ancient Forest</strong>, lies an old, crumbling fortress, known as the <strong>Ruined Castle</strong>.</p>
    <br />
    <p style={{fontSize: '30px', fontStyle: 'italic', textAlign: 'center'}}>Once the heart of the reign of a great king, now only rembered as the <strong>Fallen King</strong>, the castle now attracts adventurers and scavengers alike, with promises of treasures and glory.</p>
    <br />
    <p style={{fontSize: '30px', fontStyle: 'italic', textAlign: 'center'}}>Voices of immense treasures, artefacts that can grants whishes, as well as of a terrible evil that lurks within its walls.</p>
    <br />
    <p style={{fontSize: '30px', fontStyle: 'italic', textAlign: 'center'}}>Whatever your goal or motivations may be, your destination is the legendary <strong>Ruined Castle</strong>.</p>
    <br />
    </div>
  )
}

function SelectCharacterArchetype() {
  return (
  <>
    <h1>Select a character archetype</h1>
    <p>The archetype will influence the capabilities of your character.</p>
    <div className='button_list'>
      {data.archetypes.map(arch => (
        <SelectArchetypeButton key={arch.name} name={arch.name} fontSize={'30px'} />
      ))}
    </div>
    </>
  )
}

function SelectArchetypeButton({name, fontSize}) {
  const archetype = useGameState((state) => state.archetype);
  const setArchetype = useGameState((state) => state.setArchetype);

  const isSelected = archetype === name;

  return (
    <button 
      className={`selector_button ${isSelected ? 'selected' : ''}`} 
      onClick={() => setArchetype(name)}
      style={{ fontSize: fontSize, fontWeight: 'bold' }}
    >
      {name}
    </button>
  );
}

function VisualizeCharacterArchetype() {
  const handleStateChange = useGameState((state) => state.setAppState);
  const archetype = useGameState((state) => state.archetype);
  const equippedItems = useGameState((state) => state.equippedItems);
  return (
    <div style={{display : 'flex', flexDirection : 'column', alignItems : 'center', justifyContent : 'flex-start', gap: '0px', margin: 0, padding: 0}}>
    <ArchetypePreview archetype={archetype} equippedItems={equippedItems}/>
    <p>{data.archetypes.find(a => a.name === archetype)?.description || ''}</p>
    <br/>
    <br/>
    {(archetype && archetype !== '') ?
      (
      <>
        <button className='app_button' onClick={() => {
          handleStateChange(2);
          character.characteristics = data.archetypes.find(a => a.name === archetype)?.characteristics || {};
          character.archetype =  data.archetypes.find(a => a.name === archetype)?.id || 0;
          character.features = data.archetypes.find(a => a.name === archetype)?.features.map(f => f.name) || [];
          character.equipment = {
            right_hand: equippedItems[right_hand.id]?.name || "",
            left_hand: equippedItems[left_hand.id]?.name || "",
            head: equippedItems[head.id]?.name || "",
            armor: equippedItems[armor.id]?.name || "",
            accessory: equippedItems[accessory.id]?.name || ""
          };
          console.log("Character after archetype selection:", character);
          }} style={{fontSize: '25px'}}>Continue</button>
      </>) : (<></>)
    }
    </div>
  );
}

function CharacterDescriptionPresentation() {
  return (
    <>
      <h1>Describe your character</h1>
      <p style={{ fontSize: '25px' }}>Provide a description for your character. You can describe your character both physically and character-wise, and also include some bits of backstory.</p>
      <CharacterNameRequester/>
    </>
  )
}

function CharacterDescriptionInsertion() {
  const handleStateChange = useGameState((state) => state.setAppState);
  const characterName = useGameState((state) => state.characterName);
  const startLoading = useGameState((state) => state.startLoading);
  const stopLoading = useGameState((state) => state.stopLoading);
  const [characterDescription, setCharacterDescription] = useState("");
  return (
    <div style={{display: 'flex', flexDirection: 'column', alignItems: 'center'}}>
      <textarea
      style={{margin: '60px'}}
        className='description-input'
        placeholder="Describe your character..." 
        value={characterDescription} 
        onChange={(e) => setCharacterDescription(e.target.value)} 
      />
      {(characterName && characterName !== '' && characterDescription && characterDescription !== '') ?
      (
      <>
        <button className='app_button' onClick={async () => {
          character.name = characterName;
          character.description = characterDescription;
          console.log("Character after Naming and Description:", character);
            startLoading();
              try {
                console.log("CHARACTER RIGHT BEFORE POSTING IT")
                console.table(character)
                character = await GameManager.setCharacter(character);
                console.log("Task completata in background:", character);
              } catch (error) {
                console.error("Errore task:", error);
              } finally {
              stopLoading();
              }
              handleStateChange(3);
          }} style={{fontSize: '25px'}}>Continue</button>
      </>) : (<></>)
    }
    </div>
  );
}

function GoalSelection() {
  return (
  <>
    <h1>Select a Goal</h1>
    <p>The goal is the objective of your character’s journey. Chose one of the followings.</p>
    <div className='button_list'>
      {data.goals.map(g => (
        <SelectGoalButton key={g.name} name={g.description} fontSize={'22px'} />
      ))}
    </div>
    </> 
  )
}
function SelectGoalButton({name, fontSize}) {
  const goal = useGameState((state) => state.goal);
  const setGoal = useGameState((state) => state.setGoal);
  const isSelected = goal === name;

  return (
    <button 
      className={`selector_button ${isSelected ? 'selected' : ''}`} 
      onClick={() => {setGoal(data.goals.find(g => g.name === name)?.description || name); console.log("Selected goal:", goal); console.log("isSelected:", isSelected);}}
      style={{ fontSize: fontSize, fontWeight: 'bold' }}
    >
      {data.goals.find(g => g.name === name)?.description || name}
    </button>
  );
}

function MotivationInsertion() {
  const handleStateChange = useGameState((state) => state.setAppState);
  const goal = useGameState((state) => state.goal);
  const setCharacter =  useGameState((state) => state.setCharacter);
  const startLoading = useGameState((state) => state.startLoading);
  const stopLoading = useGameState((state) => state.stopLoading);
  const [motivation, setMotivation] = useState("");
  return (
    <div style={{display : 'flex', flexDirection : 'column', alignItems: 'center'}}>
      <textarea
        className='description-input'
        placeholder="Explain the motivation why your character is pursuing their goal..." 
        value={motivation} 
        onChange={(e) => setMotivation(e.target.value)} 
      />
      {(goal && goal !== '' && motivation && motivation !== '') ?
      (
      <>
        <button className='app_button' onClick={async () => {
          goalAndMotivation.goal = goal;
          goalAndMotivation.motivation = motivation;
          console.log("Goal and Motivation:", goalAndMotivation);
          startLoading();
          character = await GameManager.getCharacter();
          setCharacter(character)
          console.log("Final Character:\n")
          console.table(character);
          await GameManager.setObjective(goalAndMotivation);
          narration = (JSON.parse(JSON.stringify(await GameManager.startGame()))).narration;
          console.log(narration)
          stopLoading();
          handleStateChange(4);
          }} style={{fontSize: '25px'}}>Continue</button>
      </>) : (<></>)
    }

    </div>
  )
}

function CharacteristicValue({characteristic, value}) {
  const path = `/assets/characteristics/${characteristic}.png`;
  return (
    <><div className='characteristic_value'>
        <img src={path} alt={path} width="35" height="35" />
        {value}
      </div>
    </>
  )
}

function CharacteristicsList({ archetype, character=null }) {
  console.log("0 " + character)
  var selectedArchetype = data.archetypes.find((a) => a.name === archetype);
  if (!selectedArchetype && character==null) return (<div>Archetype not found</div>);
  if (character != null && character.hasOwnProperty("character")){
    console.log("1 character:", character);
    console.log("2 character.character:", character.character);
    console.log("3 character.character.characteristics:", character.character.characteristics);
    console.log("4 Chiavi di character.character:", Object.keys(character.character || {}));
    return (
      <div className='characteristics_list'>
        {Object.entries(character.character.characteristics).map(([key, value]) => {
           const descriptionLength = characteristics.find(c => c.name === key)?.description.length || 0;
           console.log(`Rendering characteristic: ${key} with value: ${value} and description length: ${descriptionLength}`);
           return (<HoverContainer topPositionOffset={100 + (descriptionLength * 1)} key={key} tooltipContent={
            <>
              <p style={{ fontWeight: 'bold', margin: '0 0 4px 0' }}>{capitalize(key)}</p>
              <p style={{ margin: 0 }}>{characteristics.find(c => c.name === key)?.description || 'No description available.'}</p>
            </>
          }>
            <CharacteristicValue characteristic={key} value={value} />
          </HoverContainer>);
        })}
      </div>
    );
  }
  return (
    <div className='characteristics_list'>
      {Object.entries(selectedArchetype.characteristics).map(([key, value]) => {
        const descriptionLength = characteristics.find(c => c.name === key)?.description.length || 0;
        return (
          <HoverContainer topPositionOffset={100 + (descriptionLength * 1)} key={key} tooltipContent={
            <>
              <p style={{ fontWeight: 'bold', margin: '0 0 4px 0' }}>{capitalize(key)}</p>
              <p style={{ margin: 0 }}>{characteristics.find(c => c.name === key)?.description || 'No description available.'}</p>
            </>
          }>
            <CharacteristicValue characteristic={key} value={value} />
          </HoverContainer>
        );
      })}
    </div>
  );
}

function Feature({feature}) {
  const fontsize = (20 - feature.name.length  + 7) + 'px';
  return (
    <HoverContainer topPositionOffset={10 + feature.description.length} tooltipContent={
      <p style={{ margin: 0 }}>{feature.description || 'No description available.'}</p>
    }>
      <div className="feature" style={{fontSize: fontsize}}>
          {capitalize(feature.name)}
      </div>
    </HoverContainer>
  )
}

function FeaturesContainer({ features, layout = 'horizontal' }) {
  return (
      <ElementContainer title="Features" content = {
        <div className={`features_list ${layout}`}>
          {features.map((feature) => (
            <Feature key={feature} feature={feature} />
          ))}
        </div>
      } layout={layout} style={{ height: "120px"}}/>
    
  )
}
  function ElementContainer({ title, content, layout = 'horizontal', style = {} }) {
    return (
      <div className={`element_container ${layout}`} style={style}>
        <h3 >{title}</h3>
        {content}
      </div>
    )
  }

function ItemCard({ item, draggable = false }) {
  console.log("Item Card: " , item)
  const { attributes, listeners, setNodeRef, transform, isDragging } = useDraggable({
    id: item.id ?? item.name,
    data: {
      name: item.name,
      description: item.description,
      bonuses: item.bonuses ?? (item.characteristic ? { [item.characteristic]: item.bonus } : {}),
      compatibleSlots: item.compatibleSlots ?? item.compatibilities
    }
  });

  const path = `/assets/items/${capitalize(item.name)}.png`;
  
  const style = transform ? {
    transform: `translate3d(${transform.x}px, ${transform.y}px, 0)`,
    cursor: 'grabbing',
    opacity: isDragging ? 0.5 : 1,
    zIndex: isDragging ? 9999 : 'auto',
    position: isDragging ? 'relative' : 'static'
  } : { cursor: 'grab' };

  const content = (
    <>
      <img src={path} alt={item.name} width={85} height={85} draggable={false}/>
    </>
  );

  const tooltipContent = (
    <div className="tooltip">
      <p style={{ textAlign: 'left', fontSize: '24px' }}>{capitalize(item.name)}</p>
      <p style={{ margin: 0, fontWeight: '300', fontStyle: 'italic', textAlign: 'left' }}>
        {(item.compatibleSlots ?? item.compatibilities)?.map((slot) => capitalize(slot)).join(', ')}
      </p>
      <br/>
      <p style={{ margin: 0, textAlign: 'left', fontWeight: '500' }}>
        {item.description || 'No description available.'}
      </p>
      <br/>
      <div style={{ display: 'flex', flexFlow: 'column', flexWrap: 'wrap', gap: '8px', marginTop: '8px', alignItems: 'flex-start' }}>
        {Object.entries((item.bonuses ?? (item.characteristic ? { [item.characteristic]: item.bonus } : {})) || {}).map(([key, value]) => (
          <CharacteristicValue key={key} characteristic={key} value={"+" + value} />
        ))}
      </div>
    </div>
  );

 if (draggable) {
    return (
      <div
        ref={setNodeRef}
        {...listeners}
        {...attributes}
        style={style}
        className="equipment-card"
      >
        <HoverContainer topPositionOffset={150 + (item.description?.length*2 || 0)} tooltipContent={tooltipContent}>
          <div className='card'>
            {content}
          </div>
        </HoverContainer>
      </div>
    );
  }

  return (
    <HoverContainer topPositionOffset={150 + (item.description?.length*2 || 0)} tooltipContent={tooltipContent}>
      <div className='card'>
        {content}
      </div>
    </HoverContainer>
  );
}

function ItemSlot({slot, equippedItem, active = false }) {
  const { setNodeRef, isOver } = useDroppable({
    id: slot.id,
    data: { slotType: slot.type }  // Memorizza il tipo di slot (es. "weapon", "armor")
  });

  const path = `/assets/slots/${slot.id.toLowerCase()}.png`;

  // Contenuto comune
  const content = (
    <>
      <img src={path} alt={slot.name} width="70" height="70" />
    </>
  );
  
  const equippedItemData = equippedItem ? {
  id: equippedItem.id ?? equippedItem.name,
  name: equippedItem.name,
  description: equippedItem.description || 'No description',
  bonuses: equippedItem.bonuses || {},
  compatibleSlots: equippedItem.compatibleSlots || []
} : null;

  return (
    <div 
      ref={setNodeRef}
      className={`equipment-slot ${isOver ? 'drag-over' : ''}`}
    >
      {equippedItemData ? (
        <ItemCard item = {equippedItemData} draggable = {active}/>
      ) : (
        <div className="empty-slot">{content}</div>
      )}
      
    </div>
  );
}

function EquipmentContainer({ equippedItems, active }) {
  return (
    <ElementContainer title = "Equipment" content={
          <>
          
          <div style={{ display: 'flex', flexDirection: 'row', gap: '20px', paddingLeft: '20px', paddingRight: '20px', paddingTop: '10px' , paddingBottom: '5px' }}>
            <ItemSlot slot={right_hand} equippedItem = {equippedItems[right_hand.id]} active = {active}/>
            <ItemSlot slot={head} equippedItem = {equippedItems[head.id]} active = {active}/>
            <ItemSlot slot={left_hand} equippedItem = {equippedItems[left_hand.id]} active = {active}/>
          </div>
          <div style={{ display: 'flex', flexDirection: 'row', gap: '20px', paddingLeft: '40px', paddingRight: '40px', paddingTop: '5px' , paddingBottom: '10px'}}>
            <ItemSlot slot={armor} equippedItem = {equippedItems[armor.id]} active = {active}/>
            <ItemSlot slot={accessory} equippedItem = {equippedItems[accessory.id]} active = {active}/>
          </div>
        </>
      } style={{ width: '400px', height: '400px', display: 'flex' , flexDirection: 'column', gap: '0px' }}/>
    )
}

function InventoryContainer({ inventory }) {
  
  // Se non è un array, trasformalo
  console.log("inventory: ", inventory)
  const safeInventory = Array.isArray(inventory) ? inventory : [];
  console.log("safeInventory: ", safeInventory)
  return (
    <ElementContainer title = "Inventory" content={
      <div className='inventory'>
        {safeInventory.map((item) => (<ItemCard key={item.name} item={item} draggable={true}/>))}
      </div>
    } style={{ width: '400px', height: '220px', padding: '0px' }}/>
  )
}

function CharacterSheet( {archetype, equippedItems, inventory = [], complete = false, character = null} ) {

  const setEquippedItems = useGameState((state) => state.setEquippedItems);
  const setInventory = useGameState((state) => state.setInventory);
  const party = useGameState((state) => state.party);
  console.log("equippedItems: ", equippedItems)
  console.log("inventory: ", inventory)
  console.log("party: ", party)
  // ========== DRAG & DROP SENSORS ==========
const pointerSensor = useSensor(PointerSensor, {
  activationConstraint: {
    distance: 5,  // Inizia il drag dopo 5px di movimento
  },
});
const sensors = useSensors(pointerSensor);

  // ========== STATI LOCALI ==========
  const [activeCard, setActiveCard] = useState(null);

  // ========== HANDLERS ==========
  const handleDragStart = (event) => {
    console.log("🟢 DRAG START - Evento:", event);
    console.log("Active data:", event.active.data.current);
    setActiveCard(event.active.data.current);
  };

  const handleDragEnd = (event) => {
  const { active, over } = event;
  setActiveCard(null);

  console.group("🔴 DRAG END");
  console.log("Active:", active?.id);
  console.log("Over:", over?.id);

  if (!over) {
    console.log("❌ Nessun target di drop");
    console.groupEnd();
    return;
  }

  const cardData = active.data.current;
  const slotData = over.data.current;

  // Verifica compatibilità
  if (!cardData?.compatibleSlots?.includes(slotData?.slotType)) {
    console.log('❌ Carta non compatibile con questo slot!');
    console.groupEnd();
    return;
  }

  // Prepara i nuovi stati
  const newEquippedItems = { ...equippedItems };
  let newInventory = [...inventory];

  // 1. Rimuovi la carta dalla sua posizione attuale
  //    (se proviene dall'inventario o da un altro slot)
  let isFromInventory = false;
  let sourceSlotId = null;

  // Controlla se proviene da un altro slot
  for (const [slotId, item] of Object.entries(equippedItems)) {
    if (item?.id === active.id) {
      sourceSlotId = slotId;
      break;
    }
  }

  if (sourceSlotId === over.id) {
    // L'item è stato dratto sullo stesso slot
    return;
  }

  // Controlla se proviene dall'inventario
  const inventoryItem = newInventory.find(item => {
  console.log("Comparing inventory item:", item.name, "with active item:", cardData.name);
  return capitalize(item.name) === capitalize(cardData.name);
  });

console.log("Inventory item trovato:", inventoryItem);
  // 2. Rimuovi dalla fonte originale
  if (sourceSlotId) {
    console.log("inside the wrong if")
    delete newEquippedItems[sourceSlotId];
  } else if (inventoryItem) {
  isFromInventory = true;
  
  // Rimuovi l'item usando lo stesso criterio di matching
  newInventory = newInventory.filter(item => {
    return capitalize(item.name) !== capitalize(cardData.name);
  });
  
  console.log("Inventory dopo rimozione:", newInventory);
}

  // 3. Se lo slot di destinazione era occupato, rimetti l'item in inventario
  const existingItem = newEquippedItems[over.id];
  if (existingItem) {
    newInventory.push(existingItem);
  }

  // 4. Equipaggia la nuova carta nello slot
  newEquippedItems[over.id] = {
    id: active.id,
    name: cardData.name,
    description: cardData.description,
    bonuses: cardData.bonuses,
    compatibleSlots: cardData.compatibleSlots
  };

  // 5. Aggiorna gli stati
  setEquippedItems(newEquippedItems);
  setInventory(newInventory);

  console.log("✅ Aggiornato - equippedItems:", newEquippedItems);
  console.log("✅ Aggiornato - inventory:", newInventory);

  // Chiamata API
  if (cardData.name && over.id) {
    GameManager.switchEquipment(cardData.name, over.id).catch(console.error);
  }

  console.groupEnd();
};

  const style = complete ? {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    gap: '20px',
    padding: '20px',
    height: '1200px',
    width: '440px',
  } : {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    gap: '20px',
    padding: '20px',
    height: '450px',
    width: '700px',
  };
  const content = complete ? (
    <div style = {{display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '10px'}}>
      <DndContext 
      sensors={sensors} 
      collisionDetection={closestCenter} 
      onDragStart={handleDragStart}
      onDragEnd={handleDragEnd}
    >
      <CharacteristicsList archetype={archetype} character={character} />
      <EquipmentContainer equippedItems={equippedItems} active={true} />
      <InventoryContainer inventory={inventory} />
      <FeaturesContainer features={data.archetypes.find(a => a.name === archetype)?.features || []} layout="vertical" />
      <PartyContainer party={party} />
    </DndContext>
    </div>
  ) : (
    <div style = {{display: 'flex', flexDirection: 'row', alignItems: 'center', gap: '10px'}}>
      <div style = {{display: 'flex', flexDirection: 'column'}}>
        <CharacteristicsList archetype={archetype} />
        <FeaturesContainer features={data.archetypes.find(a => a.name === archetype)?.features || []} layout="vertical" />
      </div>
      <EquipmentContainer equippedItems={equippedItems} active={false} />
    </div>
  );
  return (
    <div className='character-sheet' style={style}>
        {content}
    </div>
  )
}

function ArchetypePreview({ archetype, equippedItems }) {
  if (!archetype || archetype === '') return null;
return(
  <div style={{display : 'flex', flexDirection : 'column', alignItems : 'center', justifyContent : 'center', padding: '100px', paddingTop: '130px', gap: '00px'}}>
  <div className='title-box1' style={{ position: 'relative', zIndex: 2}}>
    {archetype}
  </div>
  <div style={{ marginTop: '-70px',  position: 'relative', zIndex: 1 }}>
    <CharacterSheet archetype={archetype} equippedItems={equippedItems} complete={false} />
  </div>
  </div>
)
}

function CharacterNameRequester() {
  const setCharacterName = useGameState((state) => state.setCharacterName);
  const characterName = useGameState((state) => state.characterName);
  const archetype = useGameState((state) => state.archetype);
  const equippedItems = useGameState((state) => state.equippedItems);
  if (!archetype || archetype === '') return null;
return(
  <div style={{display : 'flex', flexDirection : 'column', alignItems : 'center', justifyContent : 'center', padding: '100px', paddingTop: '70px', gap: '00px'}}>
  <div className='title-box1' style={{ position: 'relative', zIndex: 3, alignItems : 'center', justifyContent : 'center'}}>
    <input 
      style={{ alignItems : 'center', justifyContent : 'center'}}
      className='character-name-requester'
      type="text"
      value={characterName}
      onChange={(e) => setCharacterName(e.target.value)}
      maxLength={25}  // ← massimo 25 caratteri
      placeholder="Enter character name..."
/>
  </div>
  <div className='title-box2' style={{ marginTop: '-70px', position: 'relative', zIndex: 2  }}>
    {archetype}
  </div>
  <div style={{ marginTop: '-70px',  position: 'relative', zIndex: 1 }}>
  <CharacterSheet archetype={archetype} equippedItems={equippedItems} complete={false} />
  </div>
  </div>
)
}

function NarrationText() {
  return (
    <div className='narration_text'>{narration}</div>
  )
}

function IntroductiveImageScreen() {
  const handleStateChange = useGameState((state) => state.setAppState);
  const startLoading = useGameState((state) => state.startLoading);
  const stopLoading = useGameState((state) => state.stopLoading);
  return (<>
        <button className='app_button' onClick={async () => {
          startLoading();
          narration = (JSON.parse(JSON.stringify(await GameManager.firstScene()))).narration;
          console.log(narration)
          stopLoading();
          handleStateChange(5);
          }} style={{fontSize: '25px'}}>Continue</button>
      </>)
}

function GameInterface(){
  const handleStateChange = useGameState((state) => state.setAppState);
  const startLoading = useGameState((state) => state.startLoading);
  const stopLoading = useGameState((state) => state.stopLoading);
  const addPartyMember = useGameState((state) => state.addPartyMember);
  const [action, setAction] = useState("");
  return (<div style={{display : 'flex', flexDirection : 'column', alignItems: 'center'}}>
      <textarea
        className='description-input'
        placeholder="Describe the actions of your character..." 
        value={action} 
        onChange={(e) => setAction(e.target.value)} 
      />
      {(action && action !== '') ?
      (
      <>
        <button className='app_button' onClick={async () => {
          startLoading();
          console.log(action)
          const narration_return = (JSON.parse(JSON.stringify(await GameManager.sendAction(action))));
          narration = narration_return.narration;
          if (narration_return.joined_character != null){
            addPartyMember(narration_return.joined_character);
          }
          GameManager.getInventory()
          console.log(narration)
          stopLoading();
          handleStateChange(narration_return.end? 6: (narration_return.change_act? 4 : 5));
          }} style={{fontSize: '25px'}}>Continue</button>
      </>) : (<></>)
    }

    </div>)
}

function PartyContainer({ party }) {
  return (
    <ElementContainer title="Party Members" content={
      <div className='party-container' style={{ display: 'flex', flexDirection: 'row', gap: '10px', padding: '10px', justifyContent: 'center' }}>
        {party.map((member) => (
          <NPCCard key={member.name} character={member} />
        ))}
      </div>
    } layout="horizontal" style={{ width: '400px', height: '200px' }}/>
  )
}

function NPCCard({ character }) {
  const path = `/assets/characters/${capitalize(character.name)}.png`;
  console.log("Rendering NPC Card for:", character.name);
  console.log("Image path:", path);
  const content = (
    <>
      <img src={path} alt={character.name} width={85} height={85} draggable={false}/>
    </>
  );

  const tooltipContent = (
    <div className="tooltip">
      <p style={{ textAlign: 'left', fontSize: '24px' }}>{capitalize(character.name)}</p>
      <br/>
      <p style={{ margin: 0, textAlign: 'left', fontWeight: '500' }}>
        {character.ability_description || 'No description available.'}
      </p>
    </div>
  );

    return (
    <HoverContainer topPositionOffset={150 + (character.ability_description?.length || 0)} tooltipContent={tooltipContent}>
      <div className='card'>
        {content}
      </div>
    </HoverContainer>
  );
}

function HoverContainer({ children, tooltipContent, topPositionOffset = 0 }) {
  const [showTooltip, setShowTooltip] = useState(false);
  const [position, setPosition] = useState({ top: 0, left: 0 });
  const triggerRef = useRef(null);

  const updatePosition = () => {
    if (triggerRef.current) {
      const rect = triggerRef.current.getBoundingClientRect();
      setPosition({
        top: rect.top - 10,  // sopra l'elemento
        left: rect.left + rect.width / 2
      });
    }
  };

  const handleMouseEnter = () => {
    updatePosition();
    setShowTooltip(true);
  };

  const handleMouseLeave = () => {
    setShowTooltip(false);
  };

  return (
    <>
      <div 
        ref={triggerRef}
        onMouseEnter={handleMouseEnter}
        onMouseLeave={handleMouseLeave}
        className="hover-trigger"
      >
        {children}
      </div>
      
      {showTooltip && createPortal(
        <div 
          className="tooltip-portal"
          style={{
            position: 'fixed',
            top: position.top - topPositionOffset,
            left: position.left,
            transform: 'translateX(-50%)',
            zIndex: 99999,
            background: 'var(--book-bg)',
            border: '1px solid var(--accent)',
            borderRadius: '8px',
            padding: '8px 12px',
            pointerEvents: 'none',
          }}
        >
          {tooltipContent}
        </div>,
        document.body
      )}
    </>
  );
}

function IDPage() {
  const userId = useGameState((state) => state.userId);
  console.log("Rendering IDPage with userId:", userId);
  return (
    <div style={{display : 'flex', flexDirection : 'column', alignItems : 'center', justifyContent : 'center', height: '100%'}}>
      <h1>Your adventure has ended!</h1>
      <br />
      <p>Save this ID for the questionnaire:</p>
      <br />
      <div className='id-box'>
        {useGameState.getState().user_id}
      </div>
    </div>
  )
}

export default App;