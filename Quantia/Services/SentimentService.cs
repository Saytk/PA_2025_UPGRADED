using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;

namespace Quantia.Services
{
    /// <summary>
    /// Lit le fichier sentiment.json généré par la brique Python (mise à jour par cron ou systemd timer).
    /// </summary>
    public class SentimentFileService
    {
        private readonly string _jsonPath;
        private readonly ILogger<SentimentFileService> _log;

        public SentimentFileService(IConfiguration cfg, ILogger<SentimentFileService> log)
        {
            _jsonPath = cfg["SentimentAnalysis:JsonPath"]
                        ?? "//wsl.localhost/Ubuntu/home/jobordeau/pa/sentiment.json"; // chemin par défaut
            _log = log;
        }

        /// <summary>
        /// Charge le JSON depuis le disque et le mappe vers le DTO.
        /// Retourne null si le fichier est absent ou incorrect.
        /// </summary>
        public SentimentDto? GetLatest()
        {
            try
            {
                if (!File.Exists(_jsonPath))
                {
                    _log.LogWarning("Fichier {Path} introuvable.", _jsonPath);
                    return null;
                }

                var txt = File.ReadAllText(_jsonPath);

                var opts = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true   // ?? clé magique
                };

                return JsonSerializer.Deserialize<SentimentDto>(txt, opts);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Lecture du fichier de sentiment échouée.");
                return null;
            }
        }

    }

    #region DTOs
    public class SentimentDto
    {
        public double Global_Index { get; set; }
        public List<Cluster> Clusters { get; set; } = new();
    }

    public class Cluster
    {
        public string Topic { get; set; } = "";
        public double Avg { get; set; }
        public int Freq { get; set; }
        public double Delta { get; set; }
        public string Summary { get; set; } = "";
        public List<string> Examples { get; set; } = new();
    }
    #endregion
}
