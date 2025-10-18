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
        string tradeHistoryFilePath = @"D:\proyectos programacion\test web socket\GachaWish\trade_history.json";

        // --- 1. OBTENCIÓN DE DATOS DEL USUARIO QUE EJECUTA EL COMANDO ---
        string currentUserId = args["userId"].ToString();
        string currentUserName = args["userName"].ToString();
        string rawMessage = args["message"].ToString();

        string finalMessage = "";

        try
        {
            // --- 2. PARSEO DEL MENSAJE DE COMANDO ---
            // Formato esperado: !aceptar_trade <id_trade> o !rechazar_trade <id_trade>
            string command = rawMessage.Split(' ')[0].Trim();
            string tradeId = rawMessage.Replace(command, "").Trim();

            if (string.IsNullOrWhiteSpace(tradeId))
            {
                finalMessage = $"@{currentUserName}, formato incorrecto. Usa: {command} <ID_del_trade>";
                CPH.SetArgument("tradeResult", finalMessage);
                return false;
            }

            // --- 3. CARGAR TRADES PENDIENTES ---
            Dictionary<string, TradeProposal> pendingTrades = LoadPendingTrades(tradesFilePath);
            if (pendingTrades == null || !pendingTrades.ContainsKey(tradeId))
            {
                finalMessage = $"@{currentUserName}, no se encontró un intercambio pendiente con el ID '{tradeId}'.";
                CPH.SetArgument("tradeResult", finalMessage);
                return false;
            }

            TradeProposal trade = pendingTrades[tradeId];

            // --- 4. VERIFICAR SI EL TRADE HA EXPIRADO ---
            if (trade.ExpirationTime < DateTime.UtcNow)
            {
                pendingTrades.Remove(tradeId); // Eliminar trade expirado
                SavePendingTrades(tradesFilePath, pendingTrades);
                finalMessage = $"@{currentUserName}, el intercambio con ID '{tradeId}' ha expirado y ha sido eliminado.";
                CPH.SetArgument("tradeResult", finalMessage);
                return false;
            }

            // --- 5. VERIFICAR QUE EL USUARIO ES EL OBJETIVO DEL TRADE ---
            if (!trade.TargetUserId.Equals(currentUserId, StringComparison.OrdinalIgnoreCase))
            {
                finalMessage = $"@{currentUserName}, este intercambio no está dirigido a ti.";
                CPH.SetArgument("tradeResult", finalMessage);
                return false;
            }

            // --- 5. LÓGICA PARA ACEPTAR O RECHAZAR ---
            if (command.Equals("!aceptar_trade", StringComparison.OrdinalIgnoreCase))
            {
                // --- ACEPTAR TRADE ---
                Dictionary<string, UserInventory> allUsersData = LoadUserInventories(userInventoryFilePath);
                if (allUsersData == null)
                {
                    finalMessage = "Error del Bot: No se pudo cargar la base de datos de inventarios.";
                    CPH.SetArgument("tradeResult", finalMessage);
                    return false;
                }

                // Verificar inventario del usuario objetivo (el que acepta)
                if (!allUsersData.ContainsKey(trade.TargetUserId))
                {
                    finalMessage = $"@{currentUserName}, no tienes un inventario para completar este intercambio.";
                    CPH.SetArgument("tradeResult", finalMessage);
                    return false;
                }

                UserInventory targetInventory = allUsersData[trade.TargetUserId];
                if (targetInventory.FiveStar == null || !targetInventory.FiveStar.Any(c => c.Equals(trade.DesiredCharacter, StringComparison.OrdinalIgnoreCase)))
                {
                    finalMessage = $"@{currentUserName}, no tienes el personaje 🌟(5★) '{trade.DesiredCharacter}' en tu inventario para completar el intercambio.";
                    CPH.SetArgument("tradeResult", finalMessage);
                    return false;
                }

                // Verificar inventario del solicitante (por si acaso ha cambiado desde la solicitud)
                if (!allUsersData.ContainsKey(trade.RequesterUserId))
                {
                    finalMessage = $"Error: El inventario de @{trade.RequesterUserName} no se encontró. El intercambio no puede completarse.";
                    CPH.SetArgument("tradeResult", finalMessage);
                    return false;
                }

                UserInventory requesterInventory = allUsersData[trade.RequesterUserId];
                if (requesterInventory.FiveStar == null || !requesterInventory.FiveStar.Any(c => c.Equals(trade.OfferedCharacter, StringComparison.OrdinalIgnoreCase)))
                {
                    finalMessage = $"Error: @{trade.RequesterUserName} ya no tiene el personaje 🌟(5★) '{trade.OfferedCharacter}'. El intercambio no puede completarse.";
                    CPH.SetArgument("tradeResult", finalMessage);
                    return false;
                }

                // Realizar el intercambio
                requesterInventory.FiveStar.RemoveAll(c => c.Equals(trade.OfferedCharacter, StringComparison.OrdinalIgnoreCase));
                requesterInventory.FiveStar.Add(trade.DesiredCharacter);

                targetInventory.FiveStar.RemoveAll(c => c.Equals(trade.DesiredCharacter, StringComparison.OrdinalIgnoreCase));
                targetInventory.FiveStar.Add(trade.OfferedCharacter);

                SaveUserInventories(userInventoryFilePath, allUsersData);

                // --- GUARDAR EN HISTORIAL DE TRADES ---
                List<TradeHistoryEntry> tradeHistory = LoadTradeHistory(tradeHistoryFilePath);
                if (tradeHistory == null)
                {
                    tradeHistory = new List<TradeHistoryEntry>();
                }

                TradeHistoryEntry historyEntry = new TradeHistoryEntry
                {
                    TradeId = trade.TradeId,
                    RequesterUserId = trade.RequesterUserId,
                    RequesterUserName = trade.RequesterUserName,
                    OfferedCharacter = trade.OfferedCharacter,
                    TargetUserId = trade.TargetUserId,
                    TargetUserName = trade.TargetUserName,
                    DesiredCharacter = trade.DesiredCharacter,
                    TradeDate = DateTime.UtcNow
                };
                tradeHistory.Add(historyEntry);
                SaveTradeHistory(tradeHistoryFilePath, tradeHistory);

                // Eliminar el trade pendiente
                pendingTrades.Remove(tradeId);
                SavePendingTrades(tradesFilePath, pendingTrades);

                finalMessage = $"¡Intercambio completado! @{trade.RequesterUserName} ha recibido 🌟(5★) '{trade.DesiredCharacter}' y @{trade.TargetUserName} ha recibido 🌟(5★) '{trade.OfferedCharacter}'.";
            }
            else if (command.Equals("!rechazar_trade", StringComparison.OrdinalIgnoreCase))
            {
                // --- RECHAZAR TRADE ---
                pendingTrades.Remove(tradeId);
                SavePendingTrades(tradesFilePath, pendingTrades);

                finalMessage = $"@{currentUserName} ha rechazado el intercambio. @{trade.RequesterUserName}, tu propuesta de '{trade.OfferedCharacter}' por '{trade.DesiredCharacter}' ha sido rechazada.";
            }
            else
            {
                finalMessage = $"@{currentUserName}, comando desconocido. Usa !aceptar_trade o !rechazar_trade.";
                CPH.SetArgument("tradeResult", finalMessage);
                return false;
            }
        }
        catch (Exception ex)
        {
            CPH.LogInfo($"Error en streamerbot_handle_trade: {ex.Message}");
            finalMessage = "Error del Bot: Ocurrió un problema al procesar la confirmación/rechazo del intercambio.";
        }

        CPH.SetArgument("tradeResult", finalMessage);
        return true;
    }

    // --- MÉTODOS AUXILIARES (COPIADOS DE streamerbot_request_trade.cs Y MODIFICADOS) ---

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

    private void SaveUserInventories(string filePath, Dictionary<string, UserInventory> allUsersData)
    {
        try
        {
            string jsonContent = JsonConvert.SerializeObject(allUsersData, Formatting.Indented);
            File.WriteAllText(filePath, jsonContent, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            CPH.LogInfo($"Error al guardar inventarios de usuario: {ex.Message}");
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

    private List<TradeHistoryEntry> LoadTradeHistory(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return new List<TradeHistoryEntry>();
            }
            string jsonContent = File.ReadAllText(filePath, Encoding.UTF8);
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                return new List<TradeHistoryEntry>();
            }
            return JsonConvert.DeserializeObject<List<TradeHistoryEntry>>(jsonContent);
        }
        catch (Exception ex)
        {
            CPH.LogInfo($"Error al cargar historial de trades: {ex.Message}");
            return null;
        }
    }

    private void SaveTradeHistory(string filePath, List<TradeHistoryEntry> history)
    {
        try
        {
            string jsonContent = JsonConvert.SerializeObject(history, Formatting.Indented);
            File.WriteAllText(filePath, jsonContent, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            CPH.LogInfo($"Error al guardar historial de trades: {ex.Message}");
        }
    }
}

// --- CLASES AUXILIARES PARA JSON (COPIADAS DE streamerbot_request_trade.cs) ---

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

public class TradeHistoryEntry
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

    [JsonProperty("tradeDate")]
    public DateTime TradeDate { get; set; }
}
