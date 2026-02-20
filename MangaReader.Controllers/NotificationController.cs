using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MangaReader.Models;
using MangaReader.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MangaReader.Controllers;

[Authorize]
public class NotificationController(INotificationService notificationService) : Controller
{
    private readonly INotificationService _notificationService = notificationService;

    [HttpGet]
    public async Task<IActionResult> GetNotifications()
    {
        var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
        {
            return Unauthorized();
        }

        var notifications = await _notificationService.GetUserNotificationsAsync(userId);
        var unreadCount = await _notificationService.GetUnreadCountAsync(userId);

        var formattedNotifications = notifications.Select(n => new
        {
            id = n.Id,
            type = (int)n.Type,
            message = n.Message,
            isRead = n.IsRead,
            createdAt = n.CreatedAt,
            relatedMangaId = n.RelatedMangaId,
            relatedChapterId = n.RelatedChapterId,
            relatedCommentId = n.RelatedCommentId,
            triggerUser = n.TriggerUser != null ? new
            {
                username = n.TriggerUser.Username,
                avatarUrl = !string.IsNullOrWhiteSpace(n.TriggerUser.ProfilePicture) ? n.TriggerUser.ProfilePicture : n.TriggerUser.AvatarUrl
            } : null
        }).ToList();

        return Json(new
        {
            success = true,
            notifications = formattedNotifications,
            unreadCount = unreadCount
        });
    }

	[HttpPost]
	public async Task<IActionResult> MarkAsRead(int id)
	{
		await _notificationService.MarkAsReadAsync(id);
		return Json(new
		{
			success = true
		});
	}

    [HttpPost]
    public async Task<IActionResult> MarkAllAsRead(string? tab = null)
    {
        var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
        {
            return Unauthorized();
        }

        switch (tab)
        {
            case "system":
                await _notificationService.MarkAllAsReadAsync(userId, NotificationType.System);
                await _notificationService.MarkAllAsReadAsync(userId, NotificationType.Reward);
                break;
            case "series":
                await _notificationService.MarkAllAsReadAsync(userId, NotificationType.Comic);
                break;
            case "community":
                await _notificationService.MarkAllAsReadAsync(userId, NotificationType.Community);
                break;
            default:
                await _notificationService.MarkAllAsReadAsync(userId);
                break;
        }

        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        await _notificationService.DeleteNotificationAsync(id);
        return Json(new { success = true });
    }

    [HttpGet]
    public async Task<IActionResult> UnreadCount()
    {
        var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
        {
            return Json(new { count = 0 });
        }
        return Json(new { count = await _notificationService.GetUnreadCountAsync(userId) });
    }
}
