const path = require('path');
const fs = require('fs').promises;
const dataManager = require('./dataManager'); // Importar el dataManager

async function updateInventoryAndPulls(userId, userName, characters, pullCount) {
    let userInventory = await dataManager.loadUserInventory();

    if (!userInventory[userId]) {
        userInventory[userId] = {
            "userName": userName, // Guardar el nombre de usuario
            "4_star": [],
            "5_star": [],
            "6_star": [],
            "total_pulls": 0,
            "keys": 0, // Inicializar keys a 0
            "pulls_until_guaranteed_5_star": dataManager.pityData.pity_thresholds['5_star'].hard_pity
        };
    } else {
        // Asegurarse de que el userName esté actualizado en caso de cambio
        userInventory[userId].userName = userName;
        // Asegurarse de que 'keys' exista para usuarios existentes
        if (userInventory[userId].keys === undefined) {
            userInventory[userId].keys = 0;
        }
    }

    characters.forEach(character => {
        const { rarity, name: characterName } = character;
        if (['4_star', '5_star', '6_star'].includes(rarity)) {
            const userCharacters = userInventory[userId];
            if (userCharacters[rarity] && !userCharacters[rarity].includes(characterName)) {
                userCharacters[rarity].push(characterName);
                console.log(`[Inventory] Added ${characterName} (${rarity}) to ${userName}'s inventory.`);
            }
        }
    });
    
    userInventory[userId].total_pulls += pullCount;
    
    const userPity = dataManager.userData.pity_counters[userId]; // Usar userId para pity
    const hardPity5Star = dataManager.pityData.pity_thresholds['5_star'].hard_pity;
    userInventory[userId].pulls_until_guaranteed_5_star = hardPity5Star - userPity['5_star'];

    console.log(`[Inventory] ${userName}'s total pulls is now: ${userInventory[userId].total_pulls}`);
    console.log(`[Inventory] ${userName}'s 5-star pity counter: ${userPity['5_star']}`);
    console.log(`[Inventory] ${userName}'s pulls until guaranteed 5-star: ${userInventory[userId].pulls_until_guaranteed_5_star}`);

    await dataManager.saveUserInventory(userInventory);
    return userInventory[userId]; // Devolver los datos actualizados del usuario
}

function updateUserPity(userPity, obtainedRarity) {
    if (obtainedRarity === '5_star') {
        userPity['5_star'] = 0;
        userPity['4_star']++;
    } else if (obtainedRarity === '4_star') {
        userPity['4_star'] = 0;
        userPity['5_star']++;
    } else {
        userPity['4_star']++;
        userPity['5_star']++;
    }
    userPity.total_pulls++;
}

function selectRarity(userPity) {
    if (userPity['5_star'] >= dataManager.pityData.pity_thresholds['5_star'].hard_pity) return '5_star';
    if (userPity['4_star'] >= dataManager.pityData.pity_thresholds['4_star'].hard_pity) return '4_star';

    const rand = Math.random();
    let cumulative = 0;
    const probabilities = { ...dataManager.gachaConfig.gacha_rules.rarity_probabilities };

    if (userPity['5_star'] >= dataManager.pityData.pity_thresholds['5_star'].soft_pity) {
        probabilities['5_star'] += 0.1;
    }
    if (userPity['4_star'] >= dataManager.pityData.pity_thresholds['4_star'].soft_pity) {
        probabilities['4_star'] += 0.1;
    }

    if (probabilities['6_star'] && rand < (cumulative += probabilities['6_star'])) return '6_star';
    if (rand < (cumulative += probabilities['5_star'])) return '5_star';
    if (rand < (cumulative += probabilities['4_star'])) return '4_star';
    return '3_star';
}

async function selectCharacter(rarity, userPity, userId, userName, userInventory) {
    let characterList;
    let bannerSource = 'standard';

    if (rarity === '3_star') {
        characterList = dataManager.standardBanner['3_star'];
        bannerSource = 'standard';
    } else if (rarity === '6_star') {
        characterList = dataManager.seasonalBanner['6_star'];
        bannerSource = 'seasonal';
    } else if (rarity === '4_star') {
        if (Math.random() < 0.4) {
            characterList = dataManager.seasonalBanner['4_star'];
            bannerSource = 'seasonal';
            console.log('[Gacha] 4-star 40/60 result: seasonal.');
        } else {
            characterList = dataManager.standardBanner['4_star'];
            bannerSource = 'standard';
            console.log('[Gacha] 4-star 40/60 result: standard.');
        }
    } else if (rarity === '5_star') {
        let initialCharacterList;

        if (Math.random() < 0.5) {
            initialCharacterList = dataManager.seasonalBanner['5_star'];
            bannerSource = 'seasonal';
            console.log('[Gacha] 5-star 50/50 won, got seasonal.');
        } else {
            initialCharacterList = dataManager.standardBanner['5_star'];
            bannerSource = 'standard';
            console.log('[Gacha] 5-star 50/50 lost, got standard.');
        }

        let selectedCharacterName;
        let availableSeasonalCharacters = initialCharacterList.filter(charName => {
            const char = dataManager.characterData[charName];
            return char && (char.stock === undefined || char.stock > 0);
        });

        if (bannerSource === 'seasonal') {
            let potentialSeasonalPulls = availableSeasonalCharacters.filter(charName => 
                !userInventory[userId]['5_star'].includes(charName)
            );

            if (potentialSeasonalPulls.length > 0) {
                selectedCharacterName = potentialSeasonalPulls[Math.floor(Math.random() * potentialSeasonalPulls.length)];
                console.log(`[Gacha] Rerolled seasonal 5-star: ${selectedCharacterName} (new for user).`);
            } else {
                console.log('[Gacha] User has all available seasonal 5-stars. Rerolling to standard banner.');
                bannerSource = 'standard'; // Cambiar a estándar para la selección
            }
        }
        
        if (!selectedCharacterName && bannerSource === 'standard') {
            let standard5StarList = dataManager.standardBanner['5_star'];
            let availableStandardCharacters = standard5StarList.filter(charName => {
                const char = dataManager.characterData[charName];
                return char && (char.stock === undefined || char.stock > 0);
            });

            let potentialStandardPulls = availableStandardCharacters.filter(charName => 
                !userInventory[userId]['5_star'].includes(charName)
            );

            if (potentialStandardPulls.length > 0) {
                selectedCharacterName = potentialStandardPulls[Math.floor(Math.random() * potentialStandardPulls.length)];
                console.log(`[Gacha] Selected standard 5-star: ${selectedCharacterName} (new for user).`);
            } else {
                console.warn('[Gacha] User has all available standard 5-stars. Returning a default character.');
                return { name: 'Unknown Character', rarity: '3_star', image_url: '' };
            }
        }

        if (selectedCharacterName) {
            console.log(`[Gacha] Final 5-star selected: ${selectedCharacterName}`);
            return dataManager.characterData[selectedCharacterName];
        } else {
            console.error(`CRITICAL: Could not select a 5-star character for ${userName} after all attempts. Returning a default character.`);
            return { name: 'Unknown Character', rarity: '3_star', image_url: '' };
        }
    }

    // Lógica existente para otras rarezas y selección final si no es 5-star
    if (!characterList || characterList.length === 0) {
        console.warn(`[Gacha] No characters in chosen banner (${bannerSource}) for rarity ${rarity}. Falling back to standard banner.`);
        characterList = dataManager.standardBanner[rarity];
        if (!characterList || characterList.length === 0) {
             console.error(`CRITICAL: No characters found for rarity ${rarity} in any banner. Returning a default character.`);
             return { name: 'Unknown Character', rarity: '3_star', image_url: '' };
        }
    }

    if (!characterList || characterList.length === 0) {
        console.error(`CRITICAL: No characters found for rarity ${rarity} in any banner. Returning a default character.`);
        return { name: 'Unknown Character', rarity: '3_star', image_url: '' };
    }

    let availableCharacters = characterList.filter(charName => {
        const char = dataManager.characterData[charName];
        return char && (char.stock === undefined || char.stock > 0);
    });

    if (availableCharacters.length === 0) {
        console.warn(`[Gacha] No available characters for rarity ${rarity} in chosen banner. Falling back to standard banner.`);
        if (bannerSource === 'seasonal') {
            characterList = dataManager.standardBanner[rarity];
            availableCharacters = characterList.filter(charName => {
                const char = dataManager.characterData[charName];
                return char && (char.stock === undefined || char.stock > 0);
            });
        }
        if (availableCharacters.length === 0) {
            console.error(`CRITICAL: No characters found for rarity ${rarity} in any banner with available stock. Returning a default character.`);
            return { name: 'Unknown Character', rarity: '3_star', image_url: '' };
        }
    }

    let selectedCharacter = undefined;
    let attempts = 0;
    const maxAttempts = availableCharacters.length * 2;

    while (!selectedCharacter && attempts < maxAttempts) {
        const charName = availableCharacters[Math.floor(Math.random() * availableCharacters.length)];
        selectedCharacter = dataManager.characterData[charName];
        
        if (!selectedCharacter) {
            console.warn(`[Gacha] Attempted to select unloaded character '${charName}'. Re-rolling...`);
        }
        attempts++;
    }

    if (!selectedCharacter) {
        console.error(`CRITICAL: Could not select a loaded character for rarity ${rarity} after ${attempts} attempts. Finding a fallback.`);
        for (const charName of characterList) {
            if (dataManager.characterData[charName]) {
                return dataManager.characterData[charName];
            }
        }
        return { name: 'Unknown Character', rarity: '3_star', image_url: '' };
    }
    console.log(`[Gacha] Final selected character for rarity ${rarity}: ${selectedCharacter.name}`);
    return selectedCharacter;
}

async function performSinglePull(userId, userName) {
    if (!dataManager.userData.pity_counters[userId]) {
        dataManager.userData.pity_counters[userId] = {
            '4_star': 0,
            '5_star': 0,
            'total_pulls': 0
        };
    }
    // Eliminar campo obsoleto si existe
    if (dataManager.userData.pity_counters[userId].guaranteed_seasonal_4_star !== undefined) {
        delete dataManager.userData.pity_counters[userId].guaranteed_seasonal_4_star;
    }
    // También inicializar en userInventory si no existen
    let userInventory = await dataManager.loadUserInventory();
    if (!userInventory[userId]) {
        userInventory[userId] = {
            "userName": userName, // Guardar el nombre de usuario
            "4_star": [],
            "5_star": [],
            "6_star": [],
            "total_pulls": 0,
            "keys": 0, // Inicializar keys a 0
            "pulls_until_guaranteed_5_star": dataManager.pityData.pity_thresholds['5_star'].hard_pity
        };
    } else {
        // Asegurarse de que el userName esté actualizado en caso de cambio
        userInventory[userId].userName = userName;
        // Asegurarse de que 'keys' exista para usuarios existentes
        if (userInventory[userId].keys === undefined) {
            userInventory[userId].keys = 0;
        }
        if (userInventory[userId].pulls_until_guaranteed_5_star === undefined) {
            userInventory[userId].pulls_until_guaranteed_5_star = dataManager.pityData.pity_thresholds['5_star'].hard_pity - dataManager.userData.pity_counters[userId]['5_star'];
        }
    }
    await dataManager.saveUserInventory(userInventory); // Guardar los cambios en userInventory

    const userPity = dataManager.userData.pity_counters[userId];
    const rarity = selectRarity(userPity);
    const character = await selectCharacter(rarity, userPity, userId, userName, userInventory); // Pasar userId, userName y userInventory
    updateUserPity(userPity, rarity);

    // Asegurarse de que el array de rareza exista en el inventario del usuario
    if (!userInventory[userId][rarity]) {
        userInventory[userId][rarity] = [];
    }

    // Verificar si el personaje ya está en el inventario del usuario antes de descontar el stock
    const isNewCharacter = !userInventory[userId][rarity].includes(character.name);

    if (isNewCharacter && character.stock !== undefined && character.stock > 0) {
        dataManager.gachaConfig.character_stocks[character.name] = Math.max(0, dataManager.gachaConfig.character_stocks[character.name] - 1);
        dataManager.characterData[character.name].stock = dataManager.gachaConfig.character_stocks[character.name];
        await dataManager.saveGachaConfig();

        // Actualizar el stock en seasonalCharactersConfig si el personaje es de temporada
        const seasonalCharIndex = dataManager.seasonalCharactersConfig.characters.findIndex(c => c.name === character.name);
        if (seasonalCharIndex !== -1) {
            dataManager.seasonalCharactersConfig.characters[seasonalCharIndex].stock = dataManager.gachaConfig.character_stocks[character.name];
            await dataManager.saveSeasonalCharactersConfig();
        }
    }
    
    await dataManager.saveUserData();
    
    const characterForClient = { ...character };

    if (characterForClient.image_url && characterForClient.image_url.startsWith('public/')) {
        characterForClient.image_url = characterForClient.image_url.replace('public/', '');
    }
    
    characterForClient.rarity = rarity;
    
    console.log(`[Gacha] ${userName} pulled: ${characterForClient.name} (${rarity})`);
    return characterForClient;
}

async function writeLatestPullInfo(userId, userName, characters) {
    const LATEST_PULL_PATH = path.join(__dirname, '..', '..', 'GachaWish', 'latest_pull.json');
    
    const fiveStarCharacters = characters.filter(c => c.rarity === '5_star');

    if (fiveStarCharacters.length > 0) {
        const pullData = {
            userId: userId,
            userName: userName,
            characters: fiveStarCharacters.map(c => c.name),
            timestamp: new Date().toISOString()
        };

        try {
            await fs.writeFile(LATEST_PULL_PATH, JSON.stringify(pullData, null, 2));
            console.log(`[Gacha] Successfully wrote latest 5-star pull info for ${userName} to ${LATEST_PULL_PATH}`);
        } catch (error) {
            console.error(`[Gacha] Failed to write latest 5-star pull info:`, error);
        }
    } else {
        // Si no hay personajes de 5 estrellas, se puede optar por eliminar el archivo o dejarlo vacío.
        // Por ahora, lo dejaremos vacío si no hay un 5 estrellas.
        try {
            await fs.writeFile(LATEST_PULL_PATH, JSON.stringify({}, null, 2));
            console.log(`[Gacha] No 5-star characters pulled. ${LATEST_PULL_PATH} was reset.`);
        } catch (error) {
            console.error(`[Gacha] Failed to reset latest pull info:`, error);
        }
    }
}

async function pullSingleWithKey(userId, userName) {
    const userInventory = await dataManager.loadUserInventory();

    if (!userInventory[userId] || !userInventory[userId].keys || userInventory[userId].keys < 1) {
        throw new Error('No tienes suficientes llaves para realizar un tiro.');
    }

    console.log(`[GachaService] Antes de restar 1 llave para ${userName} (pullSingleWithKey). Llaves actuales: ${userInventory[userId].keys}`);
    userInventory[userId].keys -= 1;
    await dataManager.saveUserInventory(userInventory);
    console.log(`[GachaService] Después de restar 1 llave para ${userName} (pullSingleWithKey). Llaves restantes: ${userInventory[userId].keys}`);

    const character = await performSinglePull(userId, userName);
    // La función updateInventoryAndPulls se llama dentro de la ruta, así que no es necesario aquí.
    
    return character;
}

async function addKeysToUser(userId, userName, amount) {
    let userInventory = await dataManager.loadUserInventory();

    if (!userInventory[userId]) {
        userInventory[userId] = {
            "userName": userName,
            "4_star": [],
            "5_star": [],
            "6_star": [],
            "total_pulls": 0,
            "keys": 0,
            "pulls_until_guaranteed_5_star": dataManager.pityData.pity_thresholds['5_star'].hard_pity
        };
    } else {
        userInventory[userId].userName = userName;
        if (userInventory[userId].keys === undefined) {
            userInventory[userId].keys = 0;
        }
    }

    userInventory[userId].keys += amount;
    console.log(`[Inventory] Added ${amount} keys to ${userName}'s inventory. Total keys: ${userInventory[userId].keys}`);
    await dataManager.saveUserInventory(userInventory);
    return userInventory[userId].keys;
}

module.exports = {
    selectRarity,
    selectCharacter,
    updateUserPity,
    performSinglePull,
    updateInventoryAndPulls,
    writeLatestPullInfo,
    pullSingleWithKey,
    addKeysToUser
};
