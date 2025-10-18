# Documentación de Integración de Gacha e Inventario con Streamer.bot

Esta documentación detalla cómo configurar tus comandos y recompensas de puntos de canal en Streamer.bot para interactuar con tu sistema de Gacha e Inventario. Es fundamental entender que la mayoría de los scripts de C# de Streamer.bot interactúan directamente con el archivo `GachaWish/user_inventory.json` para la gestión del inventario, utilizando `userId` y `userName` para una mayor fiabilidad. Esto permite una manipulación directa y eficiente de los datos del inventario del usuario sin necesidad de un servidor intermedio para estas operaciones.

---

## 1. Configuración del Comando `!inventario`

Este comando permitirá a los usuarios consultar su inventario actual, incluyendo personajes y el estado de su "pity" para el próximo personaje de 5 estrellas.

**Acción en Streamer.bot:**

1.  Ve a la pestaña `Actions`.
2.  Crea una nueva Acción (por ejemplo, `Gacha - Mostrar Inventario`).
3.  Dentro de esta Acción, añade una sub-acción de tipo `Code` -> `Execute C# Code`.
4.  Pega el contenido del archivo `streamerbot_inventory_command.cs` (que te he proporcionado) en el editor de código C#.
5.  **Importante: Añadir Referencia `System.Core`**
    *   En la parte inferior del editor de código, busca la sección `References`.
    *   Haz clic en `Add` y busca `System.Core`.
    *   Selecciona `System.Core` y haz clic en `OK`.

**Comando de Chat en Streamer.bot:**

1.  Ve a la pestaña `Commands`.
2.  Haz clic derecho y selecciona `Add`.
3.  **Command:** `!inventario`
4.  **Location:** `Twitch` -> `Chat`
5.  **Action:** Selecciona la Acción que creaste (`Gacha - Mostrar Inventario`).
6.  **Enabled:** Asegúrate de que esté marcado.

---

## 2. Configuración del Comando `!top`

Este comando mostrará un ranking de los usuarios con más personajes en su inventario.

**Acción en Streamer.bot:**

1.  Ve a la pestaña `Actions`.
2.  Crea una nueva Acción (por ejemplo, `Gacha - Mostrar Top`).
3.  Dentro de esta Acción, añade una sub-acción de tipo `Code` -> `Execute C# Code`.
4.  Pega el contenido del archivo `streamerbot_top_command.cs` (que te he proporcionado) en el editor de código C#.
5.  **Importante: Añadir Referencia `System.Core`**
    *   En la parte inferior del editor de código, busca la sección `References`.
    *   Haz clic en `Add` y busca `System.Core`.
    *   Selecciona `System.Core` y haz clic en `OK`.
6.  Después de la sub-acción de código C#, añade una sub-acción de tipo `Twitch` -> `Send Message to Channel`.
7.  En el campo `Message`, usa la variable `%topWaifusMessage%`.
8.  Asegúrate de que esta acción esté habilitada.

**Comando de Chat en Streamer.bot:**

1.  Ve a la pestaña `Commands`.
2.  Haz clic derecho y selecciona `Add`.
3.  **Command:** `!top`
4.  **Location:** `Twitch` -> `Chat`
5.  **Action:** Selecciona la Acción que creaste (`Gacha - Mostrar Top`).
6.  **Enabled:** Asegúrate de que esté marcado.

---

## 3. Configuración de Misiones

Este sistema permite iniciar misiones rápidas en el chat y recompensar a los usuarios con llaves.

**3.1. Acción "Iniciar Misión" (Ej. `Misiones - Iniciar`)**

1.  Ve a la pestaña `Actions`.
2.  Crea una nueva Acción (por ejemplo, `Misiones - Iniciar`).
3.  Dentro de esta Acción, añade una sub-acción de tipo `Code` -> `Execute C# Code`.
4.  Pega el contenido del archivo `streamerbot_start_mission.cs` (que te he proporcionado) en el editor de código C#.
5.  **Importante: Añadir Referencia `System.Core`**
    *   En la parte inferior del editor de código, busca la sección `References`.
    *   Haz clic en `Add` y busca `System.Core`.
    *   Selecciona `System.Core` y haz clic en `OK`.
6.  Después de la sub-acción de código C#, añade una sub-acción de tipo `Twitch` -> `Send Message to Channel`.
7.  En el campo `Message`, usa la variable `%missionMessage%`.
8.  Asegúrate de que esta acción esté habilitada.
9.  **Activación:** Configura un temporizador en Streamer.bot o un comando de chat para activar esta acción periódicamente o manualmente.

**3.2. Acción "Entregar Recompensa de Misión" (Ej. `Misiones - Recompensa`)**

1.  Crea una **segunda** acción en Streamer.bot (por ejemplo, `Misiones - Recompensa`).
2.  Dentro de esta Acción, añade una sub-acción de tipo `Code` -> `Execute C# Code`.
3.  Pega el contenido del archivo `streamerbot_mission_reward.cs` (que te he proporcionado) en el editor de código C#.
4.  **Importante: Añadir Referencia `System.Core`**
    *   En la parte inferior del editor de código, busca la sección `References`.
    *   Haz clic en `Add` y busca `System.Core`.
    *   Selecciona `System.Core` y haz clic en `OK`.
5.  Después de la sub-acción de código C#, añade una sub-acción de tipo `Twitch` -> `Send Message to Channel`.
6.  En el campo `Message`, usa la variable `%winnerMessage%`.
7.  Asegúrate de que esta acción esté habilitada.

**3.3. Conectar la Recompensa con un Evento de Mensaje de Chat**

1.  Ve a la pestaña `Events` en Streamer.bot.
2.  En la sección `Twitch` -> `Chat`, haz clic derecho en `Message` y selecciona `Add`.
3.  En el campo `Action`, selecciona la acción que creaste (`Misiones - Recompensa`).
4.  Asegúrate de que esté habilitado.

---

## 4. Configuración de Recompensas de Puntos de Canal para Tiradas

Estas configuraciones son para las recompensas de puntos de canal que activan las tiradas de Gacha. Es crucial que envíen el `userId` y `userName` correctos a tu API.

**URLs de Endpoint de la API:**

Asegúrate de que tus recompensas de puntos de canal (o comandos que activen tiradas) llamen a las siguientes URLs:

*   **Tirada Individual (`/pull-single`):**
    `http://192.168.50.254:8085/pull-single?userId=%userId%&userName=%user%`

*   **Tirada Múltiple (`/pull-multi`):**
    `http://192.168.50.254:8085/pull-multi?userId=%userId%&userName=%user%`
    *   Si la tirada múltiple usa llaves, añade `&useKeys=true` al final de la URL:
        `http://192.168.50.254:8085/pull-multi?userId=%userId%&userName=%user%&useKeys=true`

*   **Tirada Individual con Llave (`/pull-single-x1key`):**
    `http://192.168.50.254:8085/pull-single-x1key?userId=%userId%&userName=%user%`

**Configuración de la Recompensa de Puntos de Canal (Ejemplo para Tirada Individual):**

1.  En Streamer.bot, ve a `Channel Points` -> `Rewards`.
2.  Haz clic derecho y selecciona `Add`.
3.  Configura el nombre, costo y otros detalles de la recompensa.
4.  En la sección `Actions`, añade una sub-acción de tipo `Network` -> `Fetch/GET URL`.
5.  **Method:** `GET`
6.  **URL:** Pega la URL correspondiente de la lista anterior (ej. `http://192.168.50.254:8085/pull-single?userId=%userId%&userName=%user%`).
7.  **Variable for Response:** `apiResponse` (opcional, para depuración).
8.  **Variable for Status Code:** `apiStatusCode` (opcional, para depuración).
9.  Puedes añadir una sub-acción `Twitch` -> `Send Message to Channel` para confirmar la tirada o mostrar errores si `apiStatusCode` no es `200`.

---

## 5. Configuración del Anuncio de 5 Estrellas

Este sistema detecta cuando un usuario obtiene un personaje de 5 estrellas y lo anuncia en el chat.

**5.1. Acción de Filtro (Ej. `Gacha - Filtro 5 Estrellas`)**

1.  Ve a la pestaña `Actions` en Streamer.bot.
2.  Haz clic derecho y selecciona `Add`. Nombra la acción, por ejemplo, `Gacha - Filtro 5 Estrellas`.
3.  Dentro de esta acción, añade una sub-acción de tipo `Code` -> `Execute C# Code`.
4.  Pega la **primera parte** del código C# del archivo `streamerbot_5star_announcement.cs` (la primera clase `CPHInline`) en el editor de código.
5.  **Importante: Añadir Referencia `System.Core`**
    *   En la parte inferior del editor de código, busca la sección `References`.
    *   Haz clic en `Add` y busca `System.Core`.
    *   Selecciona `System.Core` y haz clic en `OK`.
6.  Asegúrate de que esta acción esté habilitada.

**5.2. Acción de Mensaje (Ej. `Gacha - Anuncio 5 Estrellas`)**

1.  Crea una **segunda** acción en Streamer.bot (por ejemplo, `Gacha - Anuncio 5 Estrellas`).
2.  Dentro de esta acción, añade una sub-acción de tipo `Code` -> `Execute C# Code`.
3.  Pega la **segunda parte** del código C# del archivo `streamerbot_5star_announcement.cs` (la segunda clase `CPHInline` y la clase `LatestPull`) en el editor de código.
4.  **Importante: Añadir Referencia `System.Core`**
    *   En la parte inferior del editor de código, busca la sección `References`.
    *   Haz clic en `Add` y busca `System.Core`.
    *   Selecciona `System.Core` y haz clic en `OK`.
5.  Después de la sub-acción de código C#, añade una sub-acción de tipo `Twitch` -> `Send Message to Channel`.
6.  En el campo `Message`, usa la variable `%pullResultMessage%`.
7.  Asegúrate de que esta acción esté habilitada.

**5.3. Encadenar las Acciones con un Evento de Archivo**

1.  Ve a la pestaña `Events` en Streamer.bot.
2.  En la sección `File`, haz clic derecho y selecciona `Add`.
3.  **File Path:** `D:\proyectos programacion\test web socket\GachaWish\latest_pull.json` (asegúrate de que la ruta sea correcta).
4.  **Action:** Selecciona la acción de filtro que creaste (`Gacha - Filtro 5 Estrellas`).
5.  **Enabled:** Asegúrate de que esté marcado.

**5.4. Conectar el Filtro con el Anuncio**

1.  Vuelve a la acción `Gacha - Filtro 5 Estrellas`.
2.  Después de la sub-acción `Execute C# Code` (del filtro), añade una sub-acción de tipo `Core` -> `Execute Action`.
3.  En el campo `Action`, selecciona la acción de mensaje que creaste (`Gacha - Anuncio 5 Estrellas`).

---

## 6. Configuración de Comandos de Intercambio (Trade)

Este sistema permite a los usuarios intercambiar personajes de 5 estrellas entre sí.

**6.1. Acción "Solicitar Intercambio" (Ej. `Gacha - Request Trade`)**

1.  Ve a la pestaña `Actions`.
2.  Crea una nueva Acción (por ejemplo, `Gacha - Request Trade`).
3.  Dentro de esta Acción, añade una sub-acción de tipo `Code` -> `Execute C# Code`.
4.  Pega el contenido del archivo `streamerbot_request_trade.cs` en el editor de código C#.
5.  **Importante: Añadir Referencia `System.Core`**
    *   En la parte inferior del editor de código, busca la sección `References`.
    *   Haz clic en `Add` y busca `System.Core`.
    *   Selecciona `System.Core` y haz clic en `OK`.
6.  Después de la sub-acción de código C#, añade una sub-acción de tipo `Twitch` -> `Send Message to Channel`.
7.  En el campo `Message`, usa la variable `%tradeResult%`.
8.  Asegúrate de que esta acción esté habilitada.

**6.2. Comando de Chat para Solicitar Intercambio (`!trade`)**

1.  Ve a la pestaña `Commands`.
2.  Haz clic derecho y selecciona `Add`.
3.  **Command:** `!trade`
4.  **Location:** `Twitch` -> `Chat`
5.  **Action:** Selecciona la Acción que creaste (`Gacha - Request Trade`).
6.  **User Cooldown:** Puedes establecer un tiempo de espera para evitar spam (ej. `5` segundos).
7.  **Permissions:** Configura quién puede usar este comando (ej. `Everyone`).
8.  **Enabled:** Asegúrate de que esté marcado.

**6.3. Acción "Manejar Intercambio" (Ej. `Gacha - Handle Trade`)**

1.  Ve a la pestaña `Actions`.
2.  Crea una nueva Acción (por ejemplo, `Gacha - Handle Trade`).
3.  Dentro de esta Acción, añade una sub-acción de tipo `Code` -> `Execute C# Code`.
4.  Pega el contenido del archivo `streamerbot_handle_trade.cs` en el editor de código C#.
5.  **Importante: Añadir Referencia `System.Core`**
    *   En la parte inferior del editor de código, busca la sección `References`.
    *   Haz clic en `Add` y busca `System.Core`.
    *   Selecciona `System.Core` y haz clic en `OK`.
6.  Después de la sub-acción de código C#, añade una sub-acción de tipo `Twitch` -> `Send Message to Channel`.
7.  En el campo `Message`, usa la variable `%tradeResult%`.
8.  Asegúrate de que esta acción esté habilitada.

**6.4. Comandos de Chat para Aceptar/Rechazar Intercambio (`!aceptar_trade`, `!rechazar_trade`)**

1.  Ve a la pestaña `Commands`.
2.  Haz clic derecho y selecciona `Add`.
3.  **Command:** `!aceptar_trade`
4.  **Location:** `Twitch` -> `Chat`
5.  **Action:** Selecciona la Acción que creaste (`Gacha - Handle Trade`).
6.  **User Cooldown:** `5` segundos.
7.  **Permissions:** Configura quién puede usar este comando (ej. `Everyone`).
8.  **Enabled:** Asegúrate de que esté marcado.
9.  Repite los pasos 2-8 para el comando `!rechazar_trade`, asociándolo también a la acción `Gacha - Handle Trade`.

**6.5. Ejemplo de Flujo de Intercambio:**

**Usuario A (ej. `MrSnakeVT`) quiere intercambiar su "diluc" por el "Jean" de Usuario B (ej. `GrimVTbot`).**

1.  **Usuario A inicia el trade:**
    `MrSnakeVT: !trade diluc por jean @GrimVTbot`

2.  **Bot responde (visible para todos, pero dirigido a GrimVTbot):**
    `Bot: @GrimVTbot, @MrSnakeVT te ha ofrecido su 🌟(5★) 'diluc' a cambio de tu 🌟(5★) 'Jean'. Para aceptar, usa !aceptar_trade <ID_del_trade>. Para rechazar, usa !rechazar_trade <ID_del_trade>.`
    *(Nota: `<ID_del_trade>` será un código corto generado por el bot, como `a1b2c3d4`)*

3.  **Usuario B decide aceptar (usando el ID proporcionado por el bot):**
    `GrimVTbot: !aceptar_trade a1b2c3d4`

4.  **Bot responde:**
    `Bot: ¡Intercambio completado! @MrSnakeVT ha recibido 🌟(5★) 'Jean' y @GrimVTbot ha recibido 🌟(5★) 'diluc'.`

    **O, si Usuario B decide rechazar:**
    `GrimVTbot: !rechazar_trade a1b2c3d4`

5.  **Bot responde:**
    `Bot: @GrimVTbot ha rechazado el intercambio. @MrSnakeVT, tu propuesta de 'diluc' por 'Jean' ha sido rechazada.`

---

## 7. Consideraciones Importantes

*   **Rutas de Archivo:** Asegúrate de que las rutas de archivo `user_inventory.json` y `trades.json` en ambos códigos C# sean **exactas** a la ubicación real en tu sistema. Actualmente están configuradas como:
    *   `@"D:\proyectos programacion\test web socket\GachaWish\user_inventory.json"`
    *   `@"D:\proyectos programacion\test web socket\GachaWish\trades.json"`
*   **Creación de `trades.json`:** El archivo `GachaWish/trades.json` se creará automáticamente la primera vez que se intente guardar un trade, si no existe. No necesitas crearlo manualmente.
*   **Nombres de Personajes:** Los nombres de los personajes deben coincidir exactamente (mayúsculas/minúsculas) con los que están en `user_inventory.json`. Si los usuarios escriben mal un nombre, el bot no lo encontrará.
*   **Pruebas:** Realiza pruebas exhaustivas con diferentes usuarios y escenarios para asegurarte de que todo funciona como se espera.
*   **Migración de Datos de Inventario:** Si ya tienes un archivo `GachaWish/user_inventory.json` con nombres de usuario como claves, necesitarás un proceso para migrar esos datos a la nueva estructura (donde la clave principal es el `userId` y el `userName` se guarda dentro de la entrada del usuario).
*   **Reiniciar el Servidor:** Después de realizar cambios en el código de tu servidor Node.js, asegúrate de reiniciar el servidor para que los cambios surtan efecto.

---

## 8. Nota Importante sobre Mensajes en C# (CPH.SetArgument vs. CPH.SendMessage)

Para una mayor flexibilidad y compatibilidad con otras funcionalidades de Streamer.bot (como Speaker.bot, respuestas personalizadas, etc.), se recomienda encarecidamente utilizar `CPH.SetArgument("nombreVariableMensaje", "Tu mensaje aquí");` en tus scripts de C# en lugar de `CPH.SendMessage("Tu mensaje aquí");`.

Después de establecer el argumento en el script C#, debes añadir una sub-acción `Twitch` -> `Send Message to Channel` en la Acción de Streamer.bot, y en el campo `Message` usar la variable `%nombreVariableMensaje%`.

**Ejemplo:**

En tu script C#:
```csharp
CPH.SetArgument("miMensajeDeChat", $"¡Hola @{userName}, este es un mensaje flexible!");
```

En la Acción de Streamer.bot (después de la sub-acción de código C#):
1.  Añade una sub-acción de tipo `Twitch` -> `Send Message to Channel`.
2.  En el campo `Message`, escribe `%miMensajeDeChat%`.

Este enfoque te permite tener un control más granular sobre cómo y cuándo se envía el mensaje, y facilita la integración con otras herramientas de Streamer.bot.
