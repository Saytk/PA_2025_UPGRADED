using Microsoft.AspNetCore.Mvc;
using Quantia.Services;

namespace Quantia.Controllers
{
    public class SentimentAnalysisController : Controller
    {
        private readonly SentimentFileService _svc;

        public SentimentAnalysisController(SentimentFileService svc)
        {
            _svc = svc;
        }

        /// <summary>
        /// Affiche le dashboard de sentiment.
        /// </summary>
        public IActionResult Index()
        {
            var data = _svc.GetLatest();
            if (data == null)
            {
                // message simple si le JSON n'est pas encore prêt
                return Content("Sentiment data not available. "
                             + "The Python job may still be running.");
            }

            return View(data);          // passe le DTO à la vue Razor
        }
    }
}
