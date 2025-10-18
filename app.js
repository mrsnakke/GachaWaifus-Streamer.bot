const express = require('express');
const http = require('http');
const path = require('path');

const dataManager = require('./src/services/dataManager');
const websocket = require('./src/utils/websocket');
const gachaRoutes = require('./src/routes/gachaRoutes');
const adminRoutes = require('./src/routes/adminRoutes');
const tradeRoutes = require('./src/routes/tradeRoutes');
const viewRoutes = require('./src/routes/viewRoutes'); // Nueva importación

const kill = require('kill-port');

const app = express();
const server = http.createServer(app);

const PORT = 8085;

// Middlewares
app.use(express.json());
app.use(express.urlencoded({ extended: true }));
app.use(express.static(path.join(__dirname, 'web')));

// Inicializar WebSocket Server
websocket.initializeWebSocketServer(server);

// Rutas
app.use('/', gachaRoutes); // Las rutas de gacha están en la raíz
app.use('/admin', adminRoutes); // Las rutas de administración están bajo /admin
app.use('/api', tradeRoutes); // Las rutas de tradeo
app.use('/api', viewRoutes); // Las rutas de vista de personaje

// Ruta para que Streamer.bot envíe los datos del personaje y se emitan por WebSocket
app.get('/View', async (req, res) => {
    console.log(`[App.js] Solicitud recibida para /View con los siguientes parámetros:`, req.query);
    
    // El parámetro de streamerbot es 'character', no 'characterName'
    const { userId, userName, character: characterName } = req.query;

    if (!characterName) {
        // Enviar respuesta a Streamer.bot y no hacer nada más
        return res.status(400).send('El parámetro "character" es requerido.');
    }

    const normalizedCharacterName = characterName.toLowerCase();

    try {
        // 1. Cargar información del personaje
        const characterFilePath = path.join(__dirname, 'GachaWish/gacha_data/characters', `${normalizedCharacterName}.json`);
        let characterData;
        try {
            characterData = await dataManager.loadJsonFile(characterFilePath);
        } catch (err) {
            console.error(`[App.js/View] Personaje no encontrado: ${normalizedCharacterName}.json`);
            // Enviar respuesta a Streamer.bot
            return res.status(404).send(`Personaje "${characterName}" no encontrado.`);
        }

        // 2. Ajustar la ruta de la imagen para el frontend
        let characterImage = characterData.image_url;
        // Si la URL ya es una URL completa (de GitHub), no la modificamos.
        if (characterImage && !characterImage.startsWith('http')) {
            if (characterImage.startsWith('public/')) {
                characterImage = characterImage.replace('public/', '');
            }
            if (!characterImage.startsWith('/')) {
                characterImage = '/' + characterImage;
            }
        }
        
        // 3. Cargar inventario y datos de usuarios
        const userInventory = await dataManager.loadUserInventory();
        const allUsersData = dataManager.userData;

        let userOwnsCharacter = false;
        let totalInInventories = 0;
        const owners = [];

        for (const uId in userInventory) {
            const userInv = userInventory[uId];
            let userCharacterCount = 0;

            const rarityLevels = ['3_star', '4_star', '5_star', '6_star'];
            rarityLevels.forEach(rarity => {
                if (userInv[rarity] && Array.isArray(userInv[rarity])) {
                    const countInRarity = userInv[rarity].filter(char => char.toLowerCase() === normalizedCharacterName).length;
                    userCharacterCount += countInRarity;
                }
            });

            if (userCharacterCount > 0) {
                totalInInventories += userCharacterCount;
                
                const ownerUserName = (userInv.userName && userInv.userName !== '%user%') 
                    ? userInv.userName 
                    : (allUsersData[uId] ? allUsersData[uId].userName : `Usuario (${uId})`);

                owners.push({ userName: ownerUserName, count: userCharacterCount });

                if (uId === userId) {
                    userOwnsCharacter = true;
                }
            }
        }

        // 4. Formatear la calidad
        let quality = 'Desconocida';
        if (characterData.rarity) {
            quality = typeof characterData.rarity === 'number' 
                ? `${characterData.rarity}-star` 
                : String(characterData.rarity).replace('_', ' ');
        }

        // 5. Construir el payload para el WebSocket
        const payload = {
            character: {
                name: characterData.name,
                image: characterImage,
                quality: quality,
                description: characterData.description || 'No hay descripción disponible.'
            },
            userOwnsCharacter: userOwnsCharacter,
            totalInInventories: totalInInventories,
            owners: owners
        };

        // 6. Emitir el evento a través de WebSocket
        console.log(`[App.js/View] Enviando URL de imagen al cliente: ${characterImage}`);
        websocket.broadcast({ type: 'showCharacter', data: payload });

        // 7. Enviar respuesta a Streamer.bot
        res.status(200).send('OK');

    } catch (error) {
        console.error(`[App.js/View] Error al obtener datos del personaje "${characterName}":`, error);
        res.status(500).send('Error interno del servidor.');
    }
});

// Iniciar el servidor
dataManager.loadGachaData().then(() => {
    kill(PORT, 'tcp')
        .then(() => {
            console.log(`Puerto ${PORT} cerrado exitosamente.`);
            server.listen(PORT, '0.0.0.0', () => {
                const url = `http://localhost:${PORT}/admin.html`;
                console.log(`Servidor GachaWish iniciado en el puerto ${PORT}.`);
                console.log(`Panel de administración disponible en: ${url}`);
                // Usar import dinámico para la librería 'open'
                import('open').then(openModule => {
                    openModule.default(url);
                }).catch(err => {
                    console.error("Error al intentar abrir el navegador:", err);
                });
            });
        })
        .catch(error => {
            console.error(`No se pudo cerrar el puerto ${PORT}.`, error);
            // A pesar del error, intentamos iniciar el servidor
            server.listen(PORT, '0.0.0.0', () => {
                const url = `http://localhost:${PORT}/admin.html`;
                console.log(`Servidor GachaWish iniciado en el puerto ${PORT}.`);
                console.log(`Panel de administración disponible en: ${url}`);
                // Usar import dinámico para la librería 'open'
                import('open').then(openModule => {
                    openModule.default(url);
                }).catch(err => {
                    console.error("Error al intentar abrir el navegador:", err);
                });
            });
        });
}).catch(error => {
    console.error("No se pudo iniciar el servidor debido a un error al cargar los datos.", error);
});
