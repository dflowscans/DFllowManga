using System.Collections.Generic;
using System.Threading.Tasks;
using MangaReader.Models;

namespace MangaReader.Services;

public interface INotificationService
{
	Task CreateNotificationAsync(int userId, NotificationType type, string message, int? mangaId = null, int? chapterId = null, int? commentId = null, int? triggerUserId = null);

	Task<IEnumerable<Notification>> GetUserNotificationsAsync(int userId);

	Task MarkAsReadAsync(int notificationId);

	Task MarkAllAsReadAsync(int userId, NotificationType? type = null);

	Task<int> GetUnreadCountAsync(int userId);

	Task DeleteNotificationAsync(int notificationId);
}
