# Sistema de Gacha e Inventario para Streamer.bot

Este proyecto integra un sistema de gacha (tiradas de personajes) y gestión de inventario con Streamer.bot, permitiendo a los espectadores interactuar a través de comandos de chat y recompensas de puntos de canal. El sistema ha sido actualizado para usar `userId` como identificador principal, lo que mejora la fiabilidad y evita problemas con cambios de nombres de usuario.

## Características Principales

*   **Sistema de Gacha:** Permite a los usuarios realizar tiradas para obtener personajes.
*   **Gestión de Inventario:** Los usuarios pueden consultar sus personajes y llaves.
*   **Sistema de Pity:** Muestra cuántas tiradas faltan para un personaje de 5 estrellas garantizado.
*   **Misiones Rápidas:** Los usuarios pueden participar en misiones de chat para ganar llaves.
*   **Anuncios de 5 Estrellas:** Notificaciones automáticas en el chat cuando un usuario obtiene un personaje de 5 estrellas.
*   **Ranking de Coleccionistas (`!top`):** Muestra a los usuarios con más personajes.

## Configuración del Proyecto (Node.js)

    El servidor se ejecutará en `http://localhost:8085`.

## Configuración en Streamer.bot

Para una configuración detallada de cada comando y recompensa, consulta el archivo `streamerbot_instructions.txt`. A continuación, se presenta un resumen de los puntos clave:

### 1. Comandos de Chat

*   **`!inventario`**: Muestra el inventario del usuario y el estado de su pity.
*   **`!top`**: Muestra un ranking de los coleccionistas.

### 2. Recompensas de Puntos de Canal (Tiradas)

Configura tus recompensas de puntos de canal para llamar a las siguientes URLs de tu API:

*   **Tirada Individual (`/pull-single`):**
    `http://[tuip]:8085/pull-single?userId=%userId%&userName=%user%`
*   **Tirada Múltiple (`/pull-multi`):**
    `http://[tuip]:8085/pull-multi?userId=%userId%&userName=%user%`
    (Añade `&useKeys=true` si usa llaves).
*   **Tirada Individual con Llave (`/pull-single-x1key`):**
    `http://[tuip]:8085/pull-single-x1key?userId=%userId%&userName=%user%`

### 3. Sistema de Misiones

*   **Acción "Iniciar Misión":** Configura un temporizador o comando para activar esta acción periódicamente.
*   **Acción "Entregar Recompensa de Misión":** Conéctala al evento `Twitch` -> `Chat` -> `Message` para procesar las respuestas de los usuarios.

### 4. Anuncio de 5 Estrellas

*   Configura dos acciones encadenadas (`Filtro` y `Anuncio`) activadas por un evento de archivo que monitorea `GachaWish/latest_pull.json`.

### 5. Importante: Referencias de C# en Streamer.bot

Para todas las acciones de código C# en Streamer.bot, debes añadir la referencia `System.Core`:

1.  Abre la sub-acción `Execute C# Code`.
2.  En la sección `References`, haz clic en `Add`.
3.  Busca y selecciona `System.Core`.

## Migración de Datos (Importante)

Si ya tienes un archivo `GachaWish/user_inventory.json` con nombres de usuario como claves, necesitarás un proceso para migrar esos datos a la nueva estructura (donde la clave principal es el `userId` y el `userName` se guarda dentro de la entrada del usuario).

## Pruebas

Después de configurar todo, realiza pruebas exhaustivas de todas las funcionalidades para asegurar que el sistema funciona correctamente.
