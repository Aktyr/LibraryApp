using LibApp.Core.Enums;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorWeb.Client.Shared;

/// <summary>
/// Бейджи роли пользователя и статуса подписки — единое представление
/// для Users, UserProfile и MyProfile. Тексты, цвета и разметка в одном месте.
/// </summary>
public static class Badges
{
    public static RenderFragment Role(UserRole role) => b => RenderBadge(b, RoleStyle(role));
    public static RenderFragment Status(SubscriptionStatus status) => b => RenderBadge(b, StatusStyle(status));

    public static string RoleName(UserRole role) => role switch
    {
        UserRole.Admin => "Администратор",
        UserRole.Librarian => "Библиотекарь",
        _ => "Читатель"
    };

    public static string StatusName(SubscriptionStatus status) => status switch
    {
        SubscriptionStatus.Active => "Активен",
        SubscriptionStatus.Frozen => "Заморожен",
        _ => "Не подтверждён"
    };

    private static (string Bg, string Fg, string Text) RoleStyle(UserRole role) => role switch
    {
        UserRole.Admin => ("#fde7e9", "#a30000", RoleName(role)),
        UserRole.Librarian => ("#e6f0fb", "#003a6b", RoleName(role)),
        _ => ("#f3f3f3", "#555", RoleName(role))
    };

    private static (string Bg, string Fg, string Text) StatusStyle(SubscriptionStatus status) => status switch
    {
        SubscriptionStatus.Active => ("#e6f7eb", "#0a7f2e", StatusName(status)),
        SubscriptionStatus.Frozen => ("#fde7e9", "#a30000", StatusName(status)),
        _ => ("#fff3d9", "#8a4b00", StatusName(status))
    };

    private static void RenderBadge(RenderTreeBuilder b, (string Bg, string Fg, string Text) s)
    {
        b.OpenElement(0, "span");
        b.AddAttribute(1, "style",
            $"background:{s.Bg};color:{s.Fg};padding:2px 10px;border-radius:10px;font-size:12px;");
        b.AddContent(2, s.Text);
        b.CloseElement();
    }
}