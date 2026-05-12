namespace eShopModernized.Models;

/// <summary>
/// Application settings — replaces Web.config appSettings.
/// Bound via IOptions<CatalogSettings> pattern.
/// 
/// MIGRATION:
///   ConfigurationManager.AppSettings["UseMockData"]     → IOptions<CatalogSettings>.Value.UseMockData
///   ConfigurationManager.AppSettings["UseCustomization"] → IOptions<CatalogSettings>.Value.UseCustomizationData
/// </summary>
public class CatalogSettings
{
    public bool UseMockData { get; set; }
    public bool UseCustomizationData { get; set; }
    public string PicBaseUrl { get; set; } = "/pics/";
}
