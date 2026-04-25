using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace EventsApp.Common
{
    public static class DisplayTextExtensions
    {
        public static string GetDisplayName(this Enum value)
        {
            var member = value.GetType().GetMember(value.ToString()).FirstOrDefault();
            return member?.GetCustomAttribute<DisplayAttribute>()?.GetName() ?? value.ToString();
        }

        public static string ToRoleDisplay(this string? role)
        {
            return role switch
            {
                GlobalConstants.Roles.Admin => "Админ",
                GlobalConstants.Roles.Organizer => "Организатор",
                GlobalConstants.Roles.User => "Потребител",
                _ => "Без роля",
            };
        }

        public static string ToTransactionStatusDisplay(this string? status)
        {
            return status switch
            {
                GlobalConstants.TransactionStatuses.Pending => "Изчаква",
                GlobalConstants.TransactionStatuses.Paid => "Платено",
                GlobalConstants.TransactionStatuses.Failed => "Неуспешно",
                GlobalConstants.TransactionStatuses.Cancelled => "Отказано",
                GlobalConstants.TransactionStatuses.Refunded => "Възстановено",
                _ => status ?? "Неизвестно",
            };
        }
    }
}
