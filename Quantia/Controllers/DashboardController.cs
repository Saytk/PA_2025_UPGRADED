using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Quantia.Models;

public class DashboardController : Controller
{
    public ActionResult Index()
    {
        return View();
    }
}