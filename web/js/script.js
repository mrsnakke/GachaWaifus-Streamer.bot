/* Waifu Wisher para Streamer.bot - Frontend Script v3.0 */

// Importar funciones de animaciones del gachapon
import { handleSinglePullRequest, handleMultiPullRequest, displayTestMessage } from './gachaAnimations.js';

// --- Funciones de Manejo de Eventos de WebSocket ---

// Estas funciones ahora simplemente reenvían las llamadas a gachaAnimations.js
// Las funciones de inicialización del DOM y la cola se manejan dentro de gachaAnimations.js

// La conexión WebSocket es iniciada por websocket.js
