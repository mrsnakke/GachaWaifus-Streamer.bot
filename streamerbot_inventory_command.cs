// --- DIRECTIVAS NECESARIAS ---
using System;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Collections.Generic;

public class CPHInline
{
    public bool Execute()
    {
        // --- CONFIGURACIÓN ---
        string filePath = @"D:\proyectos programacion\test web socket\GachaWish\user_inventory.json";

        // --- 1. OBTENCIÓN DEL ID DE USUARIO ---
        // Usamos args["userId"] que es más confiable para los canjes.
        string userId = args["userId"].ToString();
        string userNameForDisplay = args["userName"].ToString(); // Para el mensaje inicial si no tiene inventario

        string finalMessage;

        // --- 2. BLOQUE TRY-CATCH PARA MANEJO DE ERRORES ---
        try
        {
            string jsonContent = File.ReadAllText(filePath, Encoding.UTF8);

            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                CPH.LogInfo($"El archivo JSON '{filePath}' está vacío o es inválido.");
                finalMessage = "Error del Bot: La base de datos de inventarios parece estar vacía o corrupta.";
            }
            else
            {
                // Deserializamos a un diccionario donde la clave es el userId
                var allUsersData = JsonConvert.DeserializeObject<Dictionary<string, UserInventory>>(jsonContent);

                // --- 3. LÓGICA DE BÚSQUEDA POR ID DE USUARIO ---
                if (allUsersData.ContainsKey(userId))
                {
                    var userData = allUsersData[userId];
                    
                    // Usamos el userName guardado en el inventario para el mensaje
                    string displayUserName = userData.UserName ?? userNameForDisplay; 

                    StringBuilder messageBuilder = new StringBuilder($"✨ Inventario de @{displayUserName}: ");

                    if (userData.FiveStar != null && userData.FiveStar.Count > 0)
                    {
                        messageBuilder.Append("🌟(5★): " + string.Join(", ", userData.FiveStar) + ". ");
                    }

                    if (userData.FourStar != null && userData.FourStar.Count > 0)
                    {
                        messageBuilder.Append("⭐(4★): " + string.Join(", ", userData.FourStar) + ". ");
                    }

                    if (userData.SixStar != null && userData.SixStar.Count > 0)
                    {
                        messageBuilder.Append("🌠(6★): " + string.Join(", ", userData.SixStar) + ". ");
                    }
                    
                    messageBuilder.Append($"🎟️ Tiradas Totales: {userData.TotalPulls}. ");
                    messageBuilder.Append($"🔑 Llaves: {userData.Keys}. ");

                    // Añadir información de pity
                    if (userData.PullsUntilGuaranteed5Star > 0) {
                        messageBuilder.Append($"Próximo 🌟(5★) en {userData.PullsUntilGuaranteed5Star} tiradas. ");
                    } else {
                        messageBuilder.Append($"¡Próximo 🌟(5★) garantizado! ");
                    }
                    
                    finalMessage = messageBuilder.ToString();
                }
                else
                {
                    finalMessage = $"@{userNameForDisplay}, aún no tienes un inventario. ¡Usa tus tiradas para conseguir personajes!";
                }
            }
        }
        catch (FileNotFoundException)
        {
            CPH.LogInfo($"Error Crítico: No se pudo encontrar el archivo JSON en: {filePath}");
            finalMessage = "Error del Bot: No se pudo acceder a la base de datos de inventarios.";
        }
        catch (JsonException ex)
        {
            CPH.LogInfo($"Error Crítico: El archivo JSON está mal formado. Error: {ex.Message}");
            finalMessage = "Error del Bot: La base de datos de inventarios parece estar corrupta.";
        }
        catch (Exception ex)
        {
            CPH.LogInfo($"Ha ocurrido un error inesperado al procesar el inventario: {ex.Message}");
            finalMessage = "Error del Bot: Algo salió mal al intentar leer el inventario.";
        }

        CPH.SetArgument("inventoryResult", finalMessage);
        return true;
    }
}

// Clases auxiliares para el JSON.
public class UserInventory
{
    [JsonProperty("userName")] // Nueva propiedad para guardar el nombre de usuario
    public string UserName { get; set; }

    [JsonProperty("4_star")]
    public List<string> FourStar { get; set; }

    [JsonProperty("5_star")]
    public List<string> FiveStar { get; set; }

    [JsonProperty("6_star")]
    public List<string> SixStar { get; set; }

    [JsonProperty("total_pulls")]
    public int TotalPulls { get; set; }

    [JsonProperty("keys")]
    public int Keys { get; set; }

    // Nuevas propiedades para la información de pity, si se desea mostrar en el inventario
    [JsonProperty("pulls_until_guaranteed_5_star")]
    public int PullsUntilGuaranteed5Star { get; set; }
}
