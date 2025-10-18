using System;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks; // Necesario para Task.FromResult

public class CPHInline
{
    public bool Execute()
    {
        string userId = args["userId"].ToString();
        string userName = args["userName"].ToString();
        int keysToAdd = 10; // Cantidad de llaves a añadir

        List<string> usersClaimedDaily = CPH.GetGlobalVar<List<string>>("usersClaimedDaily", persisted: false) ?? new List<string>();

        // Verificar si el usuario ya usó el comando en este stream
        if (usersClaimedDaily.Contains(userId))
        {
            CPH.SetArgument("dailyMessage", $"@{userName}, ya has reclamado tus llaves diarias por este stream. ¡Vuelve mañana!");
            return true; // Devuelve true para que Streamerbot procese las sub-acciones (incluido el envío del mensaje).
        }

        string filePath = @"D:\proyectos programacion\test web socket\GachaWish\user_inventory.json";
        
        try
        {
            string jsonContent = File.ReadAllText(filePath, Encoding.UTF8);
            var allUsersData = JsonConvert.DeserializeObject<Dictionary<string, UserInventory>>(jsonContent);

            // --- LÓGICA DE ACTUALIZACIÓN DEL INVENTARIO POR userId ---
            if (allUsersData.ContainsKey(userId)) 
            {
                allUsersData[userId].Keys += keysToAdd; 
            }
            else
            {
                // Si el usuario no tiene inventario, lo creamos con los valores por defecto.
                allUsersData[userId] = new UserInventory 
                { 
                    UserName = userName, // Guardamos el nombre de usuario
                    Keys = keysToAdd, 
                    FourStar = new List<string>(), 
                    FiveStar = new List<string>(), 
                    SixStar = new List<string>(), 
                    TotalPulls = 0,
                    PullsUntilGuaranteed5Star = 90 // Valor por defecto
                };
            }

            string updatedJson = JsonConvert.SerializeObject(allUsersData, Formatting.Indented);
            File.WriteAllText(filePath, updatedJson, Encoding.UTF8);
            
            // Marcar que el usuario ya usó el comando añadiéndolo a la lista global
            usersClaimedDaily.Add(userId);
            CPH.SetGlobalVar("usersClaimedDaily", usersClaimedDaily, persisted: false);

            CPH.SetArgument("dailyMessage", $"@{userName}, ¡has recibido {keysToAdd} llaves diarias! Revisa tu inventario.");
            CPH.LogInfo($"Comando !daily ejecutado con éxito para {userName}.");
            return true; 
        }
        catch (Exception ex)
        {
            CPH.SetArgument("dailyMessage", $"@{userName}, hubo un error al reclamar tus llaves diarias. Inténtalo de nuevo más tarde.");
            CPH.LogError($"Excepción al ejecutar !daily para {userName}: {ex.Message}");
            return true; // Devuelve true para que Streamerbot procese las sub-acciones (incluido el envío del mensaje de error).
        }
    }
}

// Clase auxiliar UserInventory actualizada para reflejar la estructura completa del JSON.
// Debe ser idéntica a la usada en streamerbot_mission_reward.cs
public class UserInventory 
{ 
    [JsonProperty("userName")] 
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
