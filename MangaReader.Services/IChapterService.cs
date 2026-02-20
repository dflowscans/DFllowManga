using System.Collections.Generic;
using System.Threading.Tasks;
using MangaReader.Models;
using Microsoft.AspNetCore.Http;

namespace MangaReader.Services;

public interface IChapterService
{
	Task CreateChapterAsync(Chapter chapter, List<IFormFile> pages, string pageUrls);
}
