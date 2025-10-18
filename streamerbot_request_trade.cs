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
        string userInventoryFilePath = @"D:\proyectos programacion\test web socket\GachaWish\user_inventory.json";
        string tradesFilePath = @"D:\proyectos programacion\test web socket\GachaWish\trades.json";

        // --- 1. OBTENCIÓN DE DATOS DEL SOLICITANTE ---
        string requesterUserId = args["userId"].ToString();
        string requesterUserName = args["userName"].ToString();
        string rawMessage = args["message"].ToString();

        string finalMessage = "";

        try
        {
            // --- 2. PARSEO DEL MENSAJE DE COMANDO ---
            // Formato esperado: !trade <personaje_ofrecido> por <personaje_deseado> @<usuario_objetivo>
            // Ejemplo: !trade diluc por jean @GrimVTbot

            // Eliminar el comando "!trade " del mensaje
            string commandContent = rawMessage.Replace("!trade ", "").Trim();

            // Buscar " por " para separar el personaje ofrecido y el resto
            int byIndex = commandContent.IndexOf(" por ", StringComparison.OrdinalIgnoreCase);
            if (byIndex == -1)
            {
                finalMessage = $"@{requesterUserName}, formato incorrecto. Usa: !trade <personaje_ofrecido> por <personaje_deseado> @<usuario_objetivo>";
                CPH.SetArgument("tradeResult", finalMessage);
                return false;
            }

            string offeredCharacter = commandContent.Substring(0, byIndex).Trim();
            string remainingContent = commandContent.Substring(byIndex + " por ".Length).Trim();

            // Buscar "@" para separar el personaje deseado y el usuario objetivo
            int atIndex = remainingContent.LastIndexOf("@");
            if (atIndex == -1)
            {
                finalMessage = $"@{requesterUserName}, formato incorrecto. Debes mencionar al usuario objetivo con '@'.";
                CPH.SetArgument("tradeResult", finalMessage);
                return false;
            }

            string desiredCharacter = remainingContent.Substring(0, atIndex).Trim();
            string targetUserName = remainingContent.Substring(atIndex + 1).Trim();

            // --- 3. VALIDACIONES INICIALES ---
            if (string.IsNullOrWhiteSpace(offeredCharacter) || string.IsNullOrWhiteSpace(desiredCharacter) || string.IsNullOrWhiteSpace(targetUserName))
            {
                finalMessage = $"@{requesterUserName}, asegúrate de especificar el personaje ofrecido, el personaje deseado y el usuario objetivo.";
                CPH.SetArgument("tradeResult", finalMessage);
                return false;
            }

            if (targetUserName.Equals(requesterUserName, StringComparison.OrdinalIgnoreCase))
            {
                finalMessage = $"@{requesterUserName}, no puedes intercambiar contigo mismo.";
                CPH.SetArgument("tradeResult", finalMessage);
                return false;
            }

            // --- 4. CARGAR INVENTARIOS ---
            Dictionary<string, UserInventory> allUsersData = LoadUserInventories(userInventoryFilePath);
            if (allUsersData == null)
            {
                finalMessage = "Error del Bot: No se pudo cargar la base de datos de inventarios.";
                CPH.SetArgument("tradeResult", finalMessage);
                return false;
            }

            // --- 5. OBTENER ID DEL USUARIO OBJETIVO ---
            string targetUserId = allUsersData.FirstOrDefault(x => x.Value.UserName != null && x.Value.UserName.Equals(targetUserName, StringComparison.OrdinalIgnoreCase)).Key;
            if (string.IsNullOrEmpty(targetUserId))
            {
                finalMessage = $"@{requesterUserName}, no se encontró al usuario '{targetUserName}' en la base de datos de inventarios.";
                CPH.SetArgument("tradeResult", finalMessage);
                return false;
            }

            // --- 6. VERIFICAR INVENTARIO DEL SOLICITANTE ---
            if (!allUsersData.ContainsKey(requesterUserId))
            {
                finalMessage = $"@{requesterUserName}, no tienes un inventario para ofrecer personajes.";
                CPH.SetArgument("tradeResult", finalMessage);
                return false;
            }

            UserInventory requesterInventory = allUsersData[requesterUserId];
            if (requesterInventory.FiveStar == null || !requesterInventory.FiveStar.Any(c => c.Equals(offeredCharacter, StringComparison.OrdinalIgnoreCase)))
            {
                finalMessage = $"@{requesterUserName}, no tienes el personaje 🌟(5★) '{offeredCharacter}' en tu inventario.";
                CPH.SetArgument("tradeResult", finalMessage);
                return false;
            }

            // --- 7. CREAR Y GUARDAR PROPUESTA DE INTERCAMBIO ---
            Dictionary<string, TradeProposal> pendingTrades = LoadPendingTrades(tradesFilePath);
            if (pendingTrades == null)
            {
                pendingTrades = new Dictionary<string, TradeProposal>();
            }

            string tradeId = Guid.NewGuid().ToString().Substring(0, 8); // ID corto para el trade

            TradeProposal newTrade = new TradeProposal
            {
                TradeId = tradeId,
                RequesterUserId = requesterUserId,
                RequesterUserName = requesterUserName,
                OfferedCharacter = offeredCharacter,
                TargetUserId = targetUserId,
                TargetUserName = targetUserName,
                DesiredCharacter = desiredCharacter,
                Timestamp = DateTime.UtcNow,
                ExpirationTime = DateTime.UtcNow.AddMinutes(3) // El trade expira en 3 minutos
            };

            pendingTrades[tradeId] = newTrade;
            SavePendingTrades(tradesFilePath, pendingTrades);

            finalMessage = $"@{targetUserName}, @{requesterUserName} te ha ofrecido su 🌟(5★) '{offeredCharacter}' a cambio de tu 🌟(5★) '{desiredCharacter}'. Para aceptar, usa !aceptar_trade {tradeId}. Para rechazar, usa !rechazar_trade {tradeId}.";
        }
        catch (Exception ex)
        {
            CPH.LogInfo($"Error en streamerbot_request_trade: {ex.Message}");
            finalMessage = "Error del Bot: Ocurrió un problema al procesar la solicitud de intercambio.";
        }

        CPH.SetArgument("tradeResult", finalMessage);
        return true;
    }

    // --- MÉTODOS AUXILIARES ---

    private Dictionary<string, UserInventory> LoadUserInventories(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                CPH.LogInfo($"Archivo de inventario no encontrado: {filePath}");
                return new Dictionary<string, UserInventory>();
            }
            string jsonContent = File.ReadAllText(filePath, Encoding.UTF8);
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                return new Dictionary<string, UserInventory>();
            }
            return JsonConvert.DeserializeObject<Dictionary<string, UserInventory>>(jsonContent);
        }
        catch (Exception ex)
        {
            CPH.LogInfo($"Error al cargar inventarios de usuario: {ex.Message}");
            return null;
        }
    }

    private Dictionary<string, TradeProposal> LoadPendingTrades(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return new Dictionary<string, TradeProposal>();
            }
            string jsonContent = File.ReadAllText(filePath, Encoding.UTF8);
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                return new Dictionary<string, TradeProposal>();
            }
            return JsonConvert.DeserializeObject<Dictionary<string, TradeProposal>>(jsonContent);
        }
        catch (Exception ex)
        {
            CPH.LogInfo($"Error al cargar trades pendientes: {ex.Message}");
            return null;
        }
    }

    private void SavePendingTrades(string filePath, Dictionary<string, TradeProposal> trades)
    {
        try
        {
            string jsonContent = JsonConvert.SerializeObject(trades, Formatting.Indented);
            File.WriteAllText(filePath, jsonContent, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            CPH.LogInfo($"Error al guardar trades pendientes: {ex.Message}");
        }
    }
}

// --- CLASES AUXILIARES PARA JSON ---

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

public class TradeProposal
{
    [JsonProperty("tradeId")]
    public string TradeId { get; set; }

    [JsonProperty("requesterUserId")]
    public string RequesterUserId { get; set; }

    [JsonProperty("requesterUserName")]
    public string RequesterUserName { get; set; }

    [JsonProperty("offeredCharacter")]
    public string OfferedCharacter { get; set; }

    [JsonProperty("targetUserId")]
    public string TargetUserId { get; set; }

    [JsonProperty("targetUserName")]
    public string TargetUserName { get; set; }

    [JsonProperty("desiredCharacter")]
    public string DesiredCharacter { get; set; }

    [JsonProperty("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonProperty("expirationTime")]
    public DateTime ExpirationTime { get; set; }
}
