using MudBlazor;

namespace PurchaseOrders.Web;

/// <summary>
/// This app's own look: slate navy with an amber accent, square-ish corners and
/// dense tables - closer to the back-office ERP screens this kind of tool lives
/// beside. Deliberately unlike the other apps in the portfolio.
/// </summary>
public static class PurchasingTheme
{
    public static readonly MudTheme Theme = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#334155",
            Secondary = "#d97706",
            Tertiary = "#475569",
            Info = "#0369a1",
            Success = "#15803d",
            Warning = "#b45309",
            Error = "#b91c1c",
            Background = "#f8fafc",
            Surface = "#ffffff",
            AppbarBackground = "#1e293b",
            AppbarText = "#f1f5f9",
            DrawerBackground = "#f1f5f9",
            DrawerText = "#334155",
            DrawerIcon = "#64748b",
            TextPrimary = "#1e293b",
            TextSecondary = "#64748b",
            Divider = "#e2e8f0",
            TableLines = "#eef2f7"
        },
        Typography = new Typography
        {
            Default = new DefaultTypography { FontFamily = ["IBM Plex Sans", "Segoe UI", "sans-serif"], FontSize = "0.85rem" },
            H5 = new H5Typography { FontSize = "1.3rem", FontWeight = "600" },
            H6 = new H6Typography { FontSize = "0.98rem", FontWeight = "600" },
            Subtitle2 = new Subtitle2Typography { FontSize = "0.75rem", FontWeight = "600" },
            Caption = new CaptionTypography { FontSize = "0.72rem" },
            Button = new ButtonTypography { TextTransform = "none", FontWeight = "600", FontSize = "0.82rem" }
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "4px",
            DrawerWidthLeft = "225px"
        }
    };
}
