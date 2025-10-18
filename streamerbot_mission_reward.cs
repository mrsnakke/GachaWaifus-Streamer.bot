// --- DIRECTIVAS NECESARIAS ---
using System;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Globalization;

public class CPHInline
{
    public bool Execute()
    {
        bool missionActive = CPH.GetGlobalVar<bool>("missionActive", persisted: false);
        if (!missionActive) { return false; }

        string missionAnswer = CPH.GetGlobalVar<string>("missionAnswer", persisted: false);
        string missionStartTimeStr = CPH.GetGlobalVar<string>("missionStartTime", persisted: false);
        DateTime missionStartTime = DateTime.Parse(missionStartTimeStr, null, DateTimeStyles.RoundtripKind);
        
        // 1. PRIMERA BARRERA: ¿SE ACABÓ EL TIEMPO?
        if (DateTime.UtcNow > missionStartTime.AddMinutes(1))
        {
            CPH.SendMessage($"¡El tiempo ha terminado! La respuesta correcta era: {missionAnswer}");
            CPH.SetGlobalVar("missionActive", false, persisted: false);
            return false;
        }

        string userId = args["userId"].ToString();
        string userName = args["userName"].ToString();
        string message = args["message"].ToString().Trim();
        List<string> usersWhoCompleted = CPH.GetGlobalVar<List<string>>("usersWhoCompleted", persisted: false);

        // 2. SEGUNDA BARRERA: ¿RESPUESTA CORRECTA Y EL JUGADOR NO HA GANADO ANTES?
        if (message.Equals(missionAnswer, StringComparison.OrdinalIgnoreCase) && !usersWhoCompleted.Contains(userId))
        {
            // Añadimos al jugador a la lista de esta misión para que no pueda volver a ganar.
            usersWhoCompleted.Add(userId);
            CPH.SetGlobalVar("usersWhoCompleted", usersWhoCompleted, persisted: false);

            // La misión NO se desactiva, para que otros puedan seguir participando.

            string filePath = @"D:\proyectos programacion\test web socket\GachaWish\user_inventory.json";
            try
            {
                string jsonContent = File.ReadAllText(filePath, Encoding.UTF8);
                var allUsersData = JsonConvert.DeserializeObject<Dictionary<string, UserInventory>>(jsonContent);

                if (allUsersData.ContainsKey(userId)) 
                {
                    allUsersData[userId].Keys++; 
                }
                else
                {
                    allUsersData[userId] = new UserInventory 
                    { 
                        UserName = userName, Keys = 1, FourStar = new List<string>(), FiveStar = new List<string>(), 
                        SixStar = new List<string>(), TotalPulls = 0, PullsUntilGuaranteed5Star = 90, Is5StarGuaranteedSeasonal = false
                    };
                }

                string updatedJson = JsonConvert.SerializeObject(allUsersData, Formatting.Indented);
                File.WriteAllText(filePath, updatedJson, Encoding.UTF8);
                
                string winnerMessage = $"¡Bien hecho @{userName}! Has completado la misión y ganado 1 llave. 🔑";
                CPH.SetArgument("winnerMessage", winnerMessage);

                return true; 
            }
            catch (Exception ex)
            {
                CPH.LogError($"Error al actualizar inventario: {ex.Message}");
                return false;
            }
        }
        return false;
    }
}

public class UserInventory 
{ 
    [JsonProperty("userName")] public string UserName { get; set; }
    [JsonProperty("4_star")] public List<string> FourStar { get; set; } 
    [JsonProperty("5_star")] public List<string> FiveStar { get; set; } 
    [JsonProperty("6_star")] public List<string> SixStar { get; set; } 
    [JsonProperty("total_pulls")] public int TotalPulls { get; set; } 
    [JsonProperty("keys")] public int Keys { get; set; } 
    [JsonProperty("pulls_until_guaranteed_5_star")] public int PullsUntilGuaranteed5Star { get; set; }
    [JsonProperty("is_5_star_guaranteed_seasonal")] public bool Is5StarGuaranteedSeasonal { get; set; }
}