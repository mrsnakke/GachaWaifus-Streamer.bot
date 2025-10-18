// --- DIRECTIVAS NECESARIAS ---
using System;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Collections.Generic;

public class CPHInline
{
    // --- 1. CONFIGURACIÓN MEJORADA (LISTA DE EXCLUIDOS) ---
    // Añade aquí los nombres de los usuarios que quieres excluir del ranking.
    // No importa si usas mayúsculas, minúsculas, guiones o guiones bajos, el código los manejará.
    private readonly List<string> ExcludedUsers = new List<string>
    {
        "MrSnake_Bot", // Ejemplo 1
        "test-account",  // Ejemplo 2
        "nightbot"       // Ejemplo 3
    };

    /// <summary>
    /// Función de ayuda para "normalizar" nombres de usuario.
    /// Esto asegura que "Daii_vv", "daii-vv", " @daii_vv " se traten como la misma persona.
    /// </summary>
    private string NormalizeUserName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return string.Empty;

        // 1. Quita el @ al principio.
        // 2. Convierte a minúsculas.
        // 3. Reemplaza guiones por guiones bajos (para consistencia).
        // 4. Quita espacios al inicio y al final.
        return name.Trim().ToLower().Replace('-', '_');
    }

    public bool Execute()
    {
        // --- CONFIGURACIÓN ---
        string filePath = @"D:\proyectos programacion\test web socket\GachaWish\user_inventory.json";
        
        // --- 2. OBTENCIÓN Y LIMPIEZA DEL NOMBRE DE USUARIO DEL SOLICITANTE ---
        // args["userName"] es el nombre de usuario de quien ejecuta el comando.
        string rawRequesterName = args["userName"].ToString();
        string normalizedRequesterName = NormalizeUserName(rawRequesterName);

        string finalMessage;

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
                
                // Crea un 'HashSet' de usuarios excluidos normalizados para una búsqueda súper rápida.
                var normalizedExcludedUsers = new HashSet<string>(ExcludedUsers.Select(NormalizeUserName));

                // --- 3. CÁLCULO DEL RANKING CON LÓGICA ROBUSTA ---
                var rankings = new List<UserRanking>();

                foreach (var userEntry in allUsersData)
                {
                    // userEntry.Key es ahora el userId, necesitamos userEntry.Value.UserName
                    string currentUserName = userEntry.Value.UserName;
                    string currentNormalizedName = NormalizeUserName(currentUserName);

                    // Si el usuario actual está en la lista de excluidos, se lo salta.
                    if (normalizedExcludedUsers.Contains(currentNormalizedName))
                    {
                        continue; // Saltar al siguiente usuario
                    }
                    
                    int totalChars = (userEntry.Value.FourStar?.Count ?? 0) + 
                                     (userEntry.Value.FiveStar?.Count ?? 0) + 
                                     (userEntry.Value.SixStar?.Count ?? 0);

                    // Guardamos el nombre de usuario real para mostrarlo.
                    rankings.Add(new UserRanking { 
                        Username = currentUserName, 
                        TotalCharacters = totalChars 
                    });
                }

                var sortedRankings = rankings.OrderByDescending(r => r.TotalCharacters).ToList();

                // Busca al usuario que pidió el comando usando la misma lógica de normalización.
                int userIndex = sortedRankings.FindIndex(r => NormalizeUserName(r.Username) == normalizedRequesterName);

                StringBuilder messageBuilder = new StringBuilder();

                // --- 4. CONSTRUCCIÓN DEL MENSAJE DE RESPUESTA ---
                
                // Comprueba si el usuario que pide el top está en la lista de excluidos.
                if (normalizedExcludedUsers.Contains(normalizedRequesterName))
                {
                    messageBuilder.AppendLine($"👑 El usuario @{rawRequesterName} está excluido del ranking, ¡pero gracias por preguntar!");
                }
                else if (userIndex != -1)
                {
                    int userRank = userIndex + 1;
                    int userCharCount = sortedRankings[userIndex].TotalCharacters;
                    messageBuilder.AppendLine($"👑 @{sortedRankings[userIndex].Username}, estás en la posición #{userRank} con {userCharCount} personajes.");
                }
                else
                {
                    messageBuilder.AppendLine($"@{rawRequesterName}, aún no apareces en el ranking. ¡Colecciona más personajes!");
                }

                messageBuilder.AppendLine("--- 🏆 Top 3 Coleccionistas 🏆 ---");

                for (int i = 0; i < Math.Min(3, sortedRankings.Count); i++)
                {
                    var ranker = sortedRankings[i];
                    string medal = i switch { 0 => "🥇", 1 => "🥈", 2 => "🥉", _ => "" };
                    messageBuilder.AppendLine($"{medal} #{i + 1}: @{ranker.Username} ({ranker.TotalCharacters} pjs)");
                }

                finalMessage = messageBuilder.ToString();
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
            CPH.LogInfo($"Ha ocurrido un error inesperado al procesar el ranking: {ex.Message}");
            finalMessage = "Error del Bot: Algo salió mal al calcular el ranking.";
        }
        
        CPH.SetArgument("topWaifusMessage", finalMessage);
        return true;
    }
}

// Las clases auxiliares.
public class UserRanking
{
    public string Username { get; set; }
    public int TotalCharacters { get; set; }
}

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

    [JsonProperty("pulls_until_guaranteed_5_star")]
    public int PullsUntilGuaranteed5Star { get; set; }
}
