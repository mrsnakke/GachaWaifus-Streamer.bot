const { Client } = require('@notionhq/client');
const fs = require('fs').promises;
const path = require('path');

// --- CONFIGURACIÓN ---
require('dotenv').config(); // Cargar variables de entorno
const NOTION_TOKEN = process.env.NOTION_TOKEN;
const DATABASE_ID = process.env.NOTION_DATABASE_ID; // Asumiendo que también quieres que el ID de la base de datos sea una variable de entorno

const GACHA_DATA_PATH = path.join(__dirname, 'GachaWish', 'gacha_data');
const CHARACTERS_PATH = path.join(GACHA_DATA_PATH, 'characters');
const GACHA_CONFIG_PATH = path.join(__dirname, 'web', 'gacha_config.json');

const notion = new Client({ auth: NOTION_TOKEN });

// Función para leer y normalizar los detalles de un personaje
async function getCharacterDetails(characterName) {
    try {
        const filePath = path.join(CHARACTERS_PATH, `${characterName}.json`);
        const data = await fs.readFile(filePath, 'utf-8');
        const details = JSON.parse(data);

        // Normalizar la rareza a un número si es un string como "5_star"
        if (typeof details.rarity === 'string') {
            const match = details.rarity.match(/\d+/);
            if (match) {
                details.rarity = parseInt(match[0], 10);
            }
        }
        
        return details;
    } catch (error) {
        console.error(`No se pudo leer el archivo para el personaje: ${characterName}`, error);
        return null;
    }
}

async function main() {
    try {
        console.log('Iniciando la actualización de Notion con nuevas reglas...');

        // 1. Leer archivos de configuración y personajes
        console.log('Leyendo archivos de configuración y personajes...');
        const gachaConfig = JSON.parse(await fs.readFile(GACHA_CONFIG_PATH));
        const limitedStocks = gachaConfig.character_stocks || {};
        const characterFiles = await fs.readdir(CHARACTERS_PATH);
        const allCharacterNames = characterFiles.map(file => path.parse(file).name);

        // 2. Obtener las páginas existentes en la base de datos de Notion
        console.log('Obteniendo datos existentes de Notion...');
        const existingPages = await notion.databases.query({ database_id: DATABASE_ID });

        const notionCharacterMap = new Map();
        for (const page of existingPages.results) {
            const characterName = page.properties.Nombre.title[0]?.text.content;
            if (characterName) {
                notionCharacterMap.set(characterName, page.id);
            }
        }
        console.log(`Se encontraron ${notionCharacterMap.size} personajes en Notion.`);

        // 3. Procesar y eliminar personajes de 3 estrellas
        console.log('Buscando personajes de 3 estrellas para eliminar...');
        for (const charName of allCharacterNames) {
            const details = await getCharacterDetails(charName);
            if (details && details.rarity === 3) {
                const pageId = notionCharacterMap.get(charName);
                if (pageId) {
                    console.log(`Eliminando personaje de 3 estrellas: ${charName}`);
                    await notion.pages.update({ page_id: pageId, archived: true });
                }
            }
        }

        // 4. Determinar el estado de los personajes de 4, 5 y 6 estrellas y sincronizar
        console.log('Sincronizando personajes de 4, 5 y 6 estrellas...');
        for (const charName of allCharacterNames) {
            const details = await getCharacterDetails(charName);
            if (!details || details.rarity < 4) {
                continue; // Ignorar si no hay detalles o es menor a 4 estrellas
            }

            let status;
            if (limitedStocks.hasOwnProperty(charName)) {
                status = limitedStocks[charName] > 0 ? 'Disponible (Limitado)' : 'No Disponible (Sin Stock)';
            } else {
                status = 'Siempre Disponible';
            }

            const pageId = notionCharacterMap.get(charName);
            const starString = '★'.repeat(details.rarity);
            const properties = {
                'Nombre': { title: [{ text: { content: charName } }] },
                'Estado': { select: { name: status } },
                'Estrellas': { select: { name: starString } }
            };

            if (pageId) {
                console.log(`Actualizando a ${charName} con estado: ${status}`);
                await notion.pages.update({ page_id: pageId, properties: properties });
            } else {
                console.log(`Creando a ${charName} con estado: ${status}`);
                await notion.pages.create({
                    parent: { database_id: DATABASE_ID },
                    properties: properties,
                });
            }
        }

        console.log('¡Actualización completada exitosamente!');

    } catch (error) {
        console.error('Ocurrió un error al actualizar Notion:', error);
    }
}

main();
