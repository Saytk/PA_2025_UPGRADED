﻿using Microsoft.AspNetCore.Mvc;

namespace Quantia.Controllers
{
    public class PortfolioController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
