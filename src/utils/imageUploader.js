const sharp = require('sharp');
const { Octokit } = require('@octokit/rest');

const GITHUB_TOKEN = process.env.GITHUB_TOKEN; // Se obtiene de las variables de entorno
const GITHUB_OWNER = 'mrsnakke';
const GITHUB_REPO = 'gachaIMG';
const GITHUB_BRANCH = 'main'; // Asumimos la rama principal, se puede ajustar si es necesario

const octokit = new Octokit({ auth: GITHUB_TOKEN });

/**
 * Redimensiona una imagen a un tamaño máximo de 1000px en su lado más largo.
 * @param {Buffer} imageBuffer - El buffer de la imagen original.
 * @returns {Promise<Buffer>} - Un buffer de la imagen redimensionada.
 */
async function resizeImage(imageBuffer) {
    return sharp(imageBuffer)
        .resize(1000, 1000, {
            fit: sharp.fit.inside,
            withoutEnlargement: true
        })
        .toBuffer();
}

/**
 * Sube una imagen a GitHub.
 * @param {string} filename - El nombre del archivo para guardar en GitHub (ej. "Nami.png").
 * @param {Buffer} imageBuffer - El buffer de la imagen a subir.
 * @param {string} rarity - La rareza del personaje para determinar la ruta en GitHub.
 * @returns {Promise<string>} - La URL de la imagen subida en GitHub.
 */
async function uploadImageToGitHub(filename, imageBuffer, rarity) {
    const pathInRepo = `img/characters/${rarity}/${filename}`;
    const contentBase64 = imageBuffer.toString('base64');

    try {
        // Intentar obtener el archivo para ver si ya existe
        let sha = null;
        try {
            const { data } = await octokit.repos.getContent({
                owner: GITHUB_OWNER,
                repo: GITHUB_REPO,
                path: pathInRepo,
                ref: GITHUB_BRANCH
            });
            sha = data.sha;
        } catch (error) {
            if (error.status !== 404) { // Si no es un 404, es un error real
                throw error;
            }
            // Si es 404, el archivo no existe, se creará.
        }

        const commitMessage = sha ? `Update ${pathInRepo}` : `Add ${pathInRepo}`;

        const { data } = await octokit.repos.createOrUpdateFileContents({
            owner: GITHUB_OWNER,
            repo: GITHUB_REPO,
            path: pathInRepo,
            message: commitMessage,
            content: contentBase64,
            branch: GITHUB_BRANCH,
            sha: sha // Solo se proporciona si el archivo ya existe (para actualizar)
        });

        // La URL de la imagen cruda (raw) es la que se debe usar
        return data.content.download_url;
    } catch (error) {
        console.error(`Error al subir la imagen a GitHub (${pathInRepo}):`, error);
        throw new Error(`No se pudo subir la imagen a GitHub: ${error.message}`);
    }
}

module.exports = {
    resizeImage,
    uploadImageToGitHub
};
