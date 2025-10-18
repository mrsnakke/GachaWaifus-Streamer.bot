const express = require('express');
const gachaService = require('../services/gachaService');
const dataManager = require('../services/dataManager');
const { broadcast } = require('../utils/websocket');

const router = express.Router();

router.get('/pull-single', async (req, res) => {
    const userId = req.query.userId;
    let userName = req.query.user || req.query.userName || 'test-user'; // Valor por defecto

    if (!userId) {
        console.error('[API] Error: userId es requerido para /pull-single');
        return res.status(400).send('El parámetro "userId" es requerido.');
    }

    // Cargar inventario para obtener el nombre de usuario correcto
    const userInventory = await dataManager.loadUserInventory();
    if (userInventory[userId] && userInventory[userId].userName) {
        userName = userInventory[userId].userName;
    }

    console.log(`[API] Single pull triggered by ${userName} (ID: ${userId})`);
    
    const character = await gachaService.performSinglePull(userId, userName);
    
    await gachaService.updateInventoryAndPulls(userId, userName, [character], 1);
    await gachaService.writeLatestPullInfo(userId, userName, [character]);
    
    broadcast({
        event: 'gacha_wish',
        data: {
            pull_type: 'single',
            userId: userId,
            userName: userName,
            character: character
        }
    });

    res.status(200).send(`Single pull for ${userName} (ID: ${userId}) processed.`);
});

router.get('/pull-multi', async (req, res) => {
    const userId = req.query.userId;
    let userName = req.query.user || req.query.userName || 'test-user'; // Valor por defecto
    const useKeys = req.query.useKeys === 'true';

    if (!userId) {
        console.error('[API] Error: userId es requerido para /pull-multi');
        return res.status(400).send('El parámetro "userId" es requerido.');
    }

    // Cargar inventario para obtener el nombre de usuario correcto
    const userInventory = await dataManager.loadUserInventory();
    if (userInventory[userId] && userInventory[userId].userName) {
        userName = userInventory[userId].userName;
    }

    console.log(`[API] Multi pull (x10) triggered by ${userName} (ID: ${userId}). Use keys: ${useKeys}`);

    try {
        if (useKeys) {
            if (!userInventory[userId] || !userInventory[userId].keys || userInventory[userId].keys < 10) {
                throw new Error('No tienes suficientes llaves para realizar un tiro múltiple (x10).');
            }
            console.log(`[GachaRoutes] Antes de restar 10 llaves para ${userName}. Llaves actuales: ${userInventory[userId].keys}`);
            userInventory[userId].keys -= 10;
            await dataManager.saveUserInventory(userInventory);
            console.log(`[GachaRoutes] Después de restar 10 llaves para ${userName}. Llaves restantes: ${userInventory[userId].keys}`);
        }

        const results = [];
        for (let i = 0; i < 10; i++) {
            const character = await gachaService.performSinglePull(userId, userName);
            results.push(character);
        }
        
        await gachaService.updateInventoryAndPulls(userId, userName, results, 10);
        await gachaService.writeLatestPullInfo(userId, userName, results);

        broadcast({
            event: 'gacha_wish',
            data: {
                pull_type: 'multi',
                userId: userId,
                userName: userName,
                characters: results
            }
        });

        res.status(200).send(`Multi pull for ${userName} (ID: ${userId}) processed.`);
    } catch (error) {
        console.error(`[API] Error during multi pull (x10) for ${userName} (ID: ${userId}):`, error.message);
        res.status(400).send(error.message);
    }
});

router.get('/pull-single-x1key', async (req, res) => {
    const userId = req.query.userId;
    let userName = req.query.user || req.query.userName; // Valor inicial

    if (!userId) {
        return res.status(400).send('El parámetro "userId" es requerido.');
    }
    if (!userName) {
        return res.status(400).send('El parámetro "userName" es requerido.');
    }

    // Cargar inventario para obtener el nombre de usuario correcto
    const userInventory = await dataManager.loadUserInventory();
    if (userInventory[userId] && userInventory[userId].userName) {
        userName = userInventory[userId].userName;
    }

    console.log(`[API] Single pull with key triggered by ${userName} (ID: ${userId})`);

    try {
        const character = await gachaService.pullSingleWithKey(userId, userName);
        
        await gachaService.updateInventoryAndPulls(userId, userName, [character], 1);
        await gachaService.writeLatestPullInfo(userId, userName, [character]);

        broadcast({
            event: 'gacha_wish',
            data: {
                pull_type: 'single',
                userId: userId,
                userName: userName,
                character: character
            }
        });

        res.status(200).send(`Single pull with key for ${userName} (ID: ${userId}) processed.`);
    } catch (error) {
        console.error(`[API] Error during single pull with key for ${userName} (ID: ${userId}):`, error.message);
        res.status(400).send(error.message);
    }
});



router.get('/add-keys', async (req, res) => {
    const userId = req.query.userId;
    const userName = req.query.user || req.query.userName;
    const amount = parseInt(req.query.amount, 10);

    if (!userId || !userName || isNaN(amount) || amount <= 0) {
        return res.status(400).send('Los parámetros "userId", "userName" y "amount" (un número positivo) son requeridos.');
    }

    console.log(`[API] Adding ${amount} keys to ${userName} (ID: ${userId})`);

    try {
        const newKeyCount = await gachaService.addKeysToUser(userId, userName, amount);
        broadcast({
            event: 'keys_updated',
            data: {
                userId: userId,
                userName: userName,
                newKeyCount: newKeyCount
            }
        });
        res.status(200).send(`Se añadieron ${amount} llaves a ${userName}. Total: ${newKeyCount}`);
    } catch (error) {
        console.error(`[API] Error adding keys for ${userName} (ID: ${userId}):`, error.message);
        res.status(500).send('Error interno del servidor al añadir llaves.');
    }
});

module.exports = router;
