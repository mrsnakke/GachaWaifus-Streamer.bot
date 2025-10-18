using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;

public class CPHInline
{
    public bool Execute()
    {
        // --- CONFIGURACIÓN ---
        string filePath = @"D:\proyectos programacion\test web socket\GachaWish\latest_pull.json";
        string globalVarName = "lastPullContent";

        try
        {
            string newJsonContent = File.ReadAllText(filePath);

            // --- FILTRO 1: ¿El archivo está vacío o es un JSON nulo '{}'? ---
            if (string.IsNullOrWhiteSpace(newJsonContent) || newJsonContent.Trim() == "{}")
            {
                // Devolver 'false' detiene la ejecución de las siguientes sub-acciones.
                return false;
            }

            var oldJsonContent = CPH.GetGlobalVar<string>(globalVarName, false);

            // --- FILTRO 2: ¿Es la misma tirada que ya anunciamos? ---
            if (newJsonContent == oldJsonContent)
            {
                // Devolver 'false' detiene la ejecución.
                return false;
            }

            // --- SI PASAMOS LOS FILTROS, LA TIRADA ES NUEVA Y VÁLIDA ---
            // 1. Guardamos el contenido nuevo para no repetirlo en el futuro.
            CPH.SetGlobalVar(globalVarName, newJsonContent, false);
            
            // 2. Pasamos el contenido a la siguiente acción para no tener que leer el archivo de nuevo.
            CPH.SetArgument("newPullJson", newJsonContent);
        }
        catch (Exception ex)
        {
            CPH.LogError("GACHA FILTER ERROR: " + ex.Message);
            return false; // Si hay un error, también detenemos todo.
        }
        
        // 'true' permite que las siguientes sub-acciones se ejecuten.
        return true;
    }
}

// --- SEGUNDA ACCIÓN: GENERAR MENSAJE ---
using System;
using System.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;

public class CPHInline
{
    private static readonly Random _random = new Random();

    public bool Execute()
    {
        try
        {
            // Ya no leemos el archivo. Usamos el argumento que nos pasó el filtro.
            string newJsonContent = args["newPullJson"].ToString();
            
            var pullData = JsonConvert.DeserializeObject<LatestPull>(newJsonContent);

            // Asegurarse de que haya un nombre de usuario y personajes
            if (string.IsNullOrWhiteSpace(pullData.UserName) || pullData.Characters == null || !pullData.Characters.Any())
            {
                return true; // No hay datos suficientes, terminamos silenciosamente.
            }
            
            string charactersList = string.Join(", ", pullData.Characters);

            List<string> messageTemplates = new List<string>
            {
                "🌟🌟🌟 ¡QUÉ LOCURA! @{0} ha conseguido un 5 estrellas: {1}! ¡Felicidades! 🌟🌟🌟",
                "✨ ¡LA SUERTE ESTÁ DE SU LADO! @{0} acaba de sacar a {1}! ✨",
                "👑 ¡LOS DIOSES DEL GACHA HAN HABLADO! Le entregan a {1} a nuestro afortunado @{0}! 👑",
                "🔥 ¡PERO QUÉ ES ESTA SUERTE! @{0} acaba de romper el juego consiguiendo a {1}! 🔥",
                "🎉 ¡NO ME LO CREO! Miren todos la increíble tirada de @{0}, que se lleva a {1}! 🎉",
                "👑 ! @{0} acaba de conseguir una tirada mítica con {1}! El chat lo celebra contigo.",
                "💫 ¡EL CIELO SE ILUMINA DE DORADO! ¡Es {1} para @{0}! ¡Vaya tirada más épica! 💫",
                "🏆 ¡Bienvenido al club de los 5 estrellas, @{0}! Acabas de conseguir a nada menos que a {1}. 🏆",
                "💥 ¡BOOM! ¡Un 5 estrellas para @{0}! Se lleva a casa a {1}. 💥",
                "🤯 ¡EL STREAM SE PARALIZA! @{0} ha invocado a {1}. ¡QUÉ MOMENTO! 🤯",
                "💖 ¡DESTINO CUMPLIDO! @{0} ha obtenido a {1}!  💖",
                "🚨 ALERTA DE SUERTE EXTREMA: @{0} ha conseguido a {1}. Repito, @{0} tiene a {1}. 🚨"
            };

            int randomIndex = _random.Next(messageTemplates.Count);
            string chosenTemplate = messageTemplates[randomIndex];
            // Usamos pullData.UserName en lugar de pullData.Redeemer
            string finalMessage = string.Format(chosenTemplate, pullData.UserName, charactersList);

            CPH.SetArgument("pullResultMessage", finalMessage);
        }
        catch (Exception ex)
        {
            CPH.LogError("GACHA MESSAGE ERROR: " + ex.Message);
        }
        
        return true;
    }
}

public class LatestPull
{
    [JsonProperty("userId")] // Nueva propiedad para el ID de usuario
    public string UserId { get; set; }

    [JsonProperty("userName")] // Nueva propiedad para el nombre de usuario
    public string UserName { get; set; }

    [JsonProperty("characters")]
    public List<string> Characters { get; set; }

    [JsonProperty("timestamp")] // Propiedad para la marca de tiempo
    public string Timestamp { get; set; }
}
