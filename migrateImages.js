const fs = require('fs').promises;
const path = require('path');
const { uploadImageToGitHub } = require('./src/utils/imageUploader.js');

const CHARACTERS_JSON_DIR = path.join(__dirname, 'GachaWish', 'gacha_data', 'characters');
const LOCAL_IMAGES_BASE_DIR = path.join(__dirname, 'web');

async function migrateImages() {
    console.log('Iniciando migración de imágenes a GitHub...');

    let files;
    try {
        files = await fs.readdir(CHARACTERS_JSON_DIR);
    } catch (error) {
        console.error(`Error: No se pudo leer el directorio de personajes: ${CHARACTERS_JSON_DIR}`, error);
        return;
    }

    const characterJsonFiles = files.filter(file => file.endsWith('.json'));
    let successCount = 0;
    let errorCount = 0;

    for (const jsonFile of characterJsonFiles) {
        const jsonFilePath = path.join(CHARACTERS_JSON_DIR, jsonFile);
        try {
            const characterData = JSON.parse(await fs.readFile(jsonFilePath, 'utf8'));

            // Si la URL ya es de GitHub, no la procesamos de nuevo
            if (characterData.image_url && characterData.image_url.startsWith('https://raw.githubusercontent.com/')) {
                console.log(`- Omitiendo a ${characterData.name}: La imagen ya es una URL de GitHub.`);
                continue;
            }

            // Construir la ruta local de la imagen
            const relativeImagePath = characterData.image_url.replace('public/', '');
            const localImagePath = path.join(LOCAL_IMAGES_BASE_DIR, relativeImagePath);
            const filename = path.basename(localImagePath);

            // Extraer la carpeta de rareza (ej. "4-star") de la ruta
            const rarityFolder = path.basename(path.dirname(localImagePath));

            // Verificar si la imagen local existe antes de intentar leerla
            try {
                await fs.access(localImagePath);
                console.log(`- Imagen local encontrada para ${characterData.name}: ${localImagePath}`);
            } catch (accessError) {
                console.warn(`- ADVERTENCIA: La imagen local para ${characterData.name} NO SE ENCONTRÓ en ${localImagePath}. Omitiendo subida.`);
                errorCount++;
                continue; // Saltar a la siguiente iteración si la imagen local no existe
            }

            // Leer la imagen local
            const imageBuffer = await fs.readFile(localImagePath);

            // Subir la imagen a GitHub
            console.log(`- Subiendo imagen para ${characterData.name} (${filename})...`);
            const newImageUrl = await uploadImageToGitHub(filename, imageBuffer, rarityFolder);

            // Actualizar y guardar el archivo JSON
            characterData.image_url = newImageUrl;
            await fs.writeFile(jsonFilePath, JSON.stringify(characterData, null, 2));

            console.log(`  => ¡Éxito! URL actualizada para ${characterData.name}: ${newImageUrl}`);
            successCount++;

        } catch (error) {
            console.error(`  => ERROR al procesar ${jsonFile}:`, error.message);
            errorCount++;
        }
    }

    console.log('\n--- Migración completada ---');
    console.log(`Imágenes procesadas exitosamente: ${successCount}`);
    console.log(`Errores encontrados: ${errorCount}`);
}

migrateImages();
