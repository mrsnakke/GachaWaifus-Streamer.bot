// --- DIRECTIVAS NECESARIAS ---
using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Net;
using Newtonsoft.Json;

public class CPHInline
{
    public enum MissionType
    {
        UnscrambleWord, SimpleMath, TypeWord, Trivia
    }

    private string ScrambleWord(string word)
    {
        char[] chars = word.ToCharArray();
        Random rand = new Random();
        for (int i = 0; i < chars.Length; i++)
        {
            int j = rand.Next(chars.Length);
            var temp = chars[i];
            chars[i] = chars[j];
            chars[j] = temp;
        }
        string scrambled = new string(chars);
        return word.Equals(scrambled, StringComparison.OrdinalIgnoreCase) ? ScrambleWord(word) : scrambled;
    }
    
    // --- FUNCIÓN DE TRIVIA REESCRITA Y SIMPLIFICADA ---
    private (string Question, string Answer) GetTriviaQuestion()
    {
        try
        {
            using (var client = new HttpClient())
            {
                // Usamos la nueva API que sí tiene buen soporte para español
                string url = "https://the-trivia-api.com/v2/questions?limit=1&language=es";
                string jsonResponse = client.GetStringAsync(url).Result;

                // La respuesta de esta API es una lista, así que la leemos como tal
                var triviaItems = JsonConvert.DeserializeObject<List<TriviaApiResult>>(jsonResponse);
                
                if (triviaItems != null && triviaItems.Any())
                {
                    var trivia = triviaItems[0];
                    string question = trivia.Question.Text; // La pregunta está en un objeto anidado
                    string answer = trivia.CorrectAnswer;
                    
                    return (question, answer);
                }
            }
        }
        catch (Exception ex)
        {
            CPH.LogError($"Error al obtener pregunta de Trivia: {ex.Message}");
        }
        
        // Pregunta de emergencia si la API falla
        return ("¿Cuál es la capital de España?", "Madrid");
    }

    public bool Execute()
    {
        bool missionActive = CPH.GetGlobalVar<bool>("missionActive", persisted: false);
        if (missionActive)
        {
            return false;
        }
        
        var wordsToScramble = new List<string> { "juego", "premio", "llave", "directo", "chat", "mision", "bot", "alerta", "emote", "canal", "video", "nivel", "jefe", "equipo", "mando", "teclado", "raton", "consola", "duelo", "magia", "poder", "mapa", "cofre", "botin", "gema", "oro", "clan", "raid", "evento", "victoria", "derrota", "punto", "ronda", "meta", "inicio", "puzzle", "lucha", "carrera", "rol", "accion", "secreto", "codigo", "pista", "tesoro", "logro", "reto", "suerte", "enigma", "acijo", "regalo", "estrella" };
        var wordsToType = new List<string> { "evento", "cofre", "tesoro", "ganador", "punto", "secreto" };
        Random rand = new Random();
        string question = "", answer = "";
        MissionType randomMissionType = (MissionType)rand.Next(Enum.GetNames(typeof(MissionType)).Length);

        switch (randomMissionType)
        {
            case MissionType.UnscrambleWord:
                answer = wordsToScramble[rand.Next(wordsToScramble.Count)];
                question = $"¡Misión de Anagrama! Ordena esta palabra: {ScrambleWord(answer)}";
                break;
            case MissionType.SimpleMath:
                int num1 = rand.Next(1, 10);
                int num2 = rand.Next(1, 10);
                answer = (num1 + num2).ToString();
                question = $"¡Misión de Cálculo! ¿Cuánto es {num1} + {num2}?";
                break;
            case MissionType.TypeWord:
                answer = wordsToType[rand.Next(wordsToType.Count)];
                question = $"¡Misión de Velocidad! Escribe la palabra: {answer}";
                break;
            case MissionType.Trivia:
                var trivia = GetTriviaQuestion();
                question = $"¡Misión de Trivia! {trivia.Question}";
                answer = trivia.Answer;
                break;
        }

        CPH.SetGlobalVar("missionActive", true, persisted: false);
        CPH.SetGlobalVar("missionAnswer", answer, persisted: false);
        CPH.SetGlobalVar("missionStartTime", DateTime.UtcNow.ToString("o"), persisted: false);
        CPH.SetGlobalVar("usersWhoCompleted", new List<string>(), persisted: false);

        string finalMessage = $" Misión Rápida! {question} ¡Tienen 1 minuto para responder y ganar una llave! ";
        CPH.SetArgument("missionMessage", finalMessage);

        return true;
    }
}

// --- CLASES AUXILIARES ACTUALIZADAS PARA LA NUEVA API ---
public class TriviaApiResult
{
    [JsonProperty("question")]
    public QuestionObject Question { get; set; }

    [JsonProperty("correctAnswer")]
    public string CorrectAnswer { get; set; }
}

public class QuestionObject
{
    [JsonProperty("text")]
    public string Text { get; set; }
}