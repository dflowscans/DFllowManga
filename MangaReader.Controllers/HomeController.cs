using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using MangaReader.Data;
using MangaReader.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MangaReader.Controllers;

public class HomeController(ApplicationDbContext context, ILogger<HomeController> logger) : Controller
{
    private readonly ApplicationDbContext _context = context;
    private readonly ILogger<HomeController> _logger = logger;

    public async Task<IActionResult> Index()
    {
        try
        {
            var manga = await _context.Mangas.Include(m => m.Chapters)
                .OrderByDescending(m => m.UpdatedAt)
                .ToListAsync();

            var settings = await _context.SiteSettings
                .Where(s => s.Key == "EnableBannerShadow" || s.Key == "BannerShadowStrength" || s.Key == "BannerShadowDepth")
                .ToDictionaryAsync(s => s.Key, s => s.Value);

            ViewBag.EnableBannerShadow = settings.GetValueOrDefault("EnableBannerShadow", "false");
            ViewBag.BannerShadowStrength = settings.GetValueOrDefault("BannerShadowStrength", "0.8");
            ViewBag.BannerShadowDepth = settings.GetValueOrDefault("BannerShadowDepth", "4");

            return View(manga);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading homepage");
            ViewBag.EnableBannerShadow = "false";
            ViewBag.BannerShadowStrength = "0.8";
            ViewBag.BannerShadowDepth = "4";
            return View(new List<Manga>());
        }
    }

    public async Task<IActionResult> Changelog()
    {
        try
        {
            var entries = await _context.ChangelogEntries
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
            return View(entries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading changelog");
            return View(new List<ChangelogEntry>());
        }
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult Maintenance(string error)
    {
        ViewBag.IsDbError = error == "db";
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }
}
