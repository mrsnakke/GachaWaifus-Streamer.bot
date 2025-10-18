const dataManager = require('./src/services/dataManager');
const gachaService = require('./src/services/gachaService');

async function runTestPull() {
    console.log('Iniciando prueba de tirada...');

    // Cargar todos los datos del gacha y de usuario
    await dataManager.loadGachaData();
    console.log('Datos de gacha cargados.');

    const testUserId = '4623815'; // Ejemplo de ID de usuario
    const testUserName = 'MrSnakeVT'; // Ejemplo de nombre de usuario
    console.log(`Realizando una tirada para el usuario: ${testUserName} (ID: ${testUserId})`);

    try {
        const character = await gachaService.performSinglePull(testUserId, testUserName);
        
        console.log('\n--- Resultado de la Tirada ---');
        console.log(`Usuario: ${testUserName} (ID: ${testUserId})`);
        console.log('Personaje Obtenido:', character);

        // Mostrar el inventario actualizado del usuario
        const userInventory = await dataManager.loadUserInventory();
        console.log('\n--- Inventario del Usuario (después de la tirada) ---');
        console.log(userInventory[testUserId]);

        // Mostrar los contadores de pity del usuario
        console.log('\n--- Contadores de Pity del Usuario (después de la tirada) ---');
        console.log(dataManager.userData.pity_counters[testUserId]);

    } catch (error) {
        console.error('Error durante la tirada de prueba:', error);
    }
}

runTestPull();
