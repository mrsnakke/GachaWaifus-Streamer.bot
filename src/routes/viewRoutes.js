const express = require('express');
const dataManager = require('../services/dataManager');
const path = require('path');

const router = express.Router();

router.get('/view-character', async (req, res) => {
    console.log(`[ViewRoutes] Solicitud recibida para /api/view-character con los siguientes parámetros:`, req.query);
    const userId = req.query.userId;
    const userName = req.query.userName;
    const characterName = req.query.characterName;

    if (!characterName) {
        return res.status(400).json({ error: 'El parámetro "characterName" es requerido.' });
    }

    const normalizedCharacterName = characterName.toLowerCase();

    try {
        // 1. Cargar información del personaje
        const characterFilePath = path.join(__dirname, '../../GachaWish/gacha_data/characters', `${normalizedCharacterName}.json`);
        let characterData;
        try {
            characterData = await dataManager.loadJsonFile(characterFilePath);
        } catch (err) {
            console.error(`[ViewRoutes] Personaje no encontrado: ${normalizedCharacterName}.json`, err.message);
            return res.status(404).json({ error: `Personaje "${characterName}" no encontrado.` });
        }

        // Ajustar la ruta de la imagen para el frontend
        let characterImage = characterData.image_url;
        if (characterImage.startsWith('public/')) {
            characterImage = characterImage.replace('public', ''); // Eliminar 'public'
        }
        // Asegurarse de que empiece con '/'
        if (!characterImage.startsWith('/')) {
            characterImage = '/' + characterImage;
        }
        
        // 2. Cargar inventario de usuarios
        const userInventory = await dataManager.loadUserInventory();
        const allUsersData = dataManager.userData; // Para obtener los nombres de usuario

        let userOwnsCharacter = false;
        let totalInInventories = 0;
        const owners = []; // Lista de { userName, count }

        for (const uId in userInventory) {
            const userInv = userInventory[uId];
            let userCharacterCount = 0;

            // Comprobar en todos los niveles de rareza
            const rarityLevels = ['3_star', '4_star', '5_star', '6_star'];
            rarityLevels.forEach(rarity => {
                if (userInv[rarity] && Array.isArray(userInv[rarity])) {
                    const countInRarity = userInv[rarity].filter(charName => charName.toLowerCase() === normalizedCharacterName).length;
                    userCharacterCount += countInRarity;
                }
            });

            if (userCharacterCount > 0) {
                totalInInventories += userCharacterCount;
                
                let ownerUserName = userInv.userName;
                // Si userInv.userName es nulo o un placeholder, intentar obtenerlo de allUsersData
                if (!ownerUserName || ownerUserName === '%user%' || ownerUserName === '%userId%') {
                    ownerUserName = allUsersData[uId] ? allUsersData[uId].userName : uId;
                }
                // Limpieza adicional: eliminar "Usuario ()" si ya viene así
                if (ownerUserName && ownerUserName.startsWith('Usuario (') && ownerUserName.endsWith(')')) {
                    ownerUserName = ownerUserName.substring(8, ownerUserName.length - 1);
                }
                // Asegurarse de que no queden placeholders finales
                if (ownerUserName === '%user%' || ownerUserName === '%userId%') {
                    ownerUserName = uId; // Usar el uId directamente como último recurso
                }
                owners.push({ userName: ownerUserName, count: userCharacterCount });

                if (uId === userId) {
                    userOwnsCharacter = true;
                }
            }
        }

        let quality = 0; // Por defecto 0 estrellas
        if (characterData.rarity) {
            if (typeof characterData.rarity === 'number') {
                quality = characterData.rarity;
            } else if (typeof characterData.rarity === 'string') {
                const match = characterData.rarity.match(/^(\d+)[_-]star/); // Modificado para aceptar '-' o '_'
                if (match && match[1]) {
                    quality = parseInt(match[1], 10);
                }
            }
        }

        res.status(200).json({
            character: {
                name: characterData.name.toUpperCase(), // Nombre en mayúsculas
                image: characterImage, // Ruta de imagen corregida
                quality: quality, // Ahora es un número
                description: characterData.description || 'No hay descripción disponible.'
            },
            userOwnsCharacter: userOwnsCharacter,
            totalInInventories: totalInInventories,
            owners: owners
        });

    } catch (error) {
        console.error(`[ViewRoutes] Error al obtener datos del personaje "${characterName}":`, error);
        res.status(500).json({ error: 'Error interno del servidor al obtener datos del personaje.' });
    }
});

module.exports = router;
