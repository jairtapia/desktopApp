using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using DesktopAssistant.Models;
using Microsoft.Win32;

namespace DesktopAssistant.Services;

public class SyncDataBuilderService
{
    private readonly IAppScannerService _appScanner;

    public SyncDataBuilderService(IAppScannerService appScanner)
    {
        _appScanner = appScanner;
    }

    public async Task<List<SyncCategory>> BuildAsync()
    {
        var appsTask = _appScanner.GetAllAppsAsync();
        var activeAppTask = Task.Run(() => _appScanner.GetForegroundApp());

        var apps = await appsTask;
        var activeApp = await activeAppTask;
        return SyncAppCatalog.BuildCategories(apps, activeApp);
    }

    // ── P/Invoke ──────────────────────────────────────────────────────────

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetSystemTimes(out long lpIdleTime, out long lpKernelTime, out long lpUserTime);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    // ── System metrics ────────────────────────────────────────────────────

    private static async Task<int> GetCpuUsageAsync()
    {
        try
        {
            GetSystemTimes(out long idle1, out long kernel1, out long user1);
            await Task.Delay(500);
            GetSystemTimes(out long idle2, out long kernel2, out long user2);
            long idle = idle2 - idle1;
            long total = (kernel2 - kernel1) + (user2 - user1);
            return total == 0 ? 0 : (int)(100.0 * (total - idle) / total);
        }
        catch { return 0; }
    }

    private static (long TotalGB, long UsedGB, int Percent) GetRamInfo()
    {
        try
        {
            var mem = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
            GlobalMemoryStatusEx(ref mem);
            long total = (long)(mem.ullTotalPhys / (1024UL * 1024 * 1024));
            long free = (long)(mem.ullAvailPhys / (1024UL * 1024 * 1024));
            long used = Math.Max(total - free, 0);
            return (Math.Max(total, 1), used, (int)mem.dwMemoryLoad);
        }
        catch { return (8, 4, 50); }
    }

    private static (long Total, long Used, long Free, int Percent) GetDiskInfo()
    {
        try
        {
            var drive = new DriveInfo("C");
            long total = drive.TotalSize / (1024L * 1024 * 1024);
            long free = drive.AvailableFreeSpace / (1024L * 1024 * 1024);
            long used = total - free;
            return (total, used, free, total > 0 ? (int)(100.0 * used / total) : 0);
        }
        catch { return (500, 250, 250, 50); }
    }

    private static (string Type, string Speed) GetNetworkInfo()
    {
        try
        {
            var adapter = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up
                         && n.NetworkInterfaceType != NetworkInterfaceType.Loopback
                         && n.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                .OrderByDescending(n => n.Speed)
                .FirstOrDefault();

            if (adapter == null) return ("Sin red", "0 Mbps");
            var type = adapter.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ? "WiFi" : "Ethernet";
            var speed = adapter.Speed >= 1_000_000_000 ? $"{adapter.Speed / 1_000_000_000} Gbps"
                : adapter.Speed > 0 ? $"{adapter.Speed / 1_000_000} Mbps" : "Auto";
            return (type, speed);
        }
        catch { return ("Red", "Auto"); }
    }

    // ── App detection ─────────────────────────────────────────────────────

    private record AppDetected(string Name, string Version, string ExePath);

    private static AppDetected? DetectApp(List<AppInfo> apps, params string[] keywords)
    {
        foreach (var kw in keywords)
        {
            var match = apps.FirstOrDefault(a => a.Name.Contains(kw, StringComparison.OrdinalIgnoreCase));
            if (match != null)
                return new AppDetected(match.Name, match.Version ?? "Instalado", match.ExecutablePath);
        }
        return null;
    }

    private static AppDetected? DetectBrowser(List<AppInfo> apps)
        => DetectApp(apps, "google chrome", "chromium", "mozilla firefox", "microsoft edge", "brave browser", "opera", "vivaldi");

    private record DetectedTools(AppDetected Primary, List<AppDetected> All);

    private static DetectedTools? DetectDevTools(List<AppInfo> apps)
    {
        string[] keywords = [
            "visual studio code", "vscode",
            "visual studio 20", "visual studio community", "visual studio professional", "visual studio enterprise",
            "github desktop",
            "postman",
            "docker desktop",
            "git for windows", "git version",
            "node.js",
            "python 3", "python 2",
            "android studio",
            "claude",
            "jetbrains", "intellij", "webstorm", "pycharm", "rider",
            "insomnia",
            "windows terminal",
            "powershell",
        ];

        var found = apps
            .Where(a => keywords.Any(k => a.Name.Contains(k, StringComparison.OrdinalIgnoreCase)))
            .Select(a => new AppDetected(a.Name, a.Version ?? "Instalado", a.ExecutablePath))
            .DistinctBy(a => a.Name)
            .ToList();

        if (found.Count == 0) return null;
        return new DetectedTools(found[0], found);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static string GetCpuName()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
            return ((key?.GetValue("ProcessorNameString") as string) ?? "CPU").Trim();
        }
        catch { return "CPU"; }
    }

    private static string GetOsName()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            var product = key?.GetValue("ProductName") as string ?? "Windows";
            var build = key?.GetValue("CurrentBuildNumber") as string ?? "";
            return build.Length > 0 ? $"{product} (Build {build})" : product;
        }
        catch { return $"Windows {Environment.OSVersion.Version.Major}"; }
    }

    private static string GetBrowserEngine(string name) =>
        name.Contains("firefox", StringComparison.OrdinalIgnoreCase) ? "Gecko" : "Chromium";

    private static string GetDefaultPrinter()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows NT\CurrentVersion\Windows");
            var raw = key?.GetValue("Device") as string;
            return raw?.Split(',')[0] ?? "Sin impresora";
        }
        catch { return "Sin impresora"; }
    }

    private static string GetOneDriveFreeSpace()
    {
        try
        {
            var path = Environment.GetEnvironmentVariable("OneDrive")
                ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "OneDrive");
            if (!Directory.Exists(path)) return "No configurado";
            var drv = new DriveInfo(Path.GetPathRoot(path) ?? "C:\\");
            return $"{drv.AvailableFreeSpace / (1024L * 1024 * 1024)} GB libres";
        }
        catch { return "No disponible"; }
    }

    private static long GetFreeGBForPath(string path)
    {
        try
        {
            var drv = new DriveInfo(Path.GetPathRoot(path) ?? "C:\\");
            return drv.AvailableFreeSpace / (1024L * 1024 * 1024);
        }
        catch { return 0; }
    }

    private static string GetSystemLanguage() =>
        System.Globalization.CultureInfo.CurrentUICulture.DisplayName;

    private static string GetRelativeTime(DateTime dt)
    {
        var diff = DateTime.Now - dt;
        if (diff.TotalMinutes < 60) return $"hace {(int)diff.TotalMinutes} min";
        if (diff.TotalHours < 24) return $"hace {(int)diff.TotalHours} h";
        return $"hace {(int)diff.TotalDays} días";
    }

    private static string TruncateName(string s, int max) =>
        s.Length > max ? s[..max] + "…" : s;

    private static string GetFileSize(string path)
    {
        try
        {
            var sz = new FileInfo(path).Length;
            return sz >= 1024 * 1024 * 1024 ? $"{sz / (1024L * 1024 * 1024)} GB"
                : sz >= 1024 * 1024 ? $"{sz / (1024L * 1024)} MB"
                : $"{sz / 1024} KB";
        }
        catch { return ""; }
    }

    private static List<string> GetRecentFiles(string folder, string[] exts, int take = 3)
    {
        try
        {
            if (!Directory.Exists(folder)) return new() { "Sin archivos recientes" };
            return Directory.GetFiles(folder)
                .Where(f => exts.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .OrderByDescending(File.GetLastWriteTime)
                .Take(take)
                .Select(f => $"{TruncateName(Path.GetFileName(f), 28)}  ·  {GetRelativeTime(File.GetLastWriteTime(f))}")
                .DefaultIfEmpty("Sin archivos recientes")
                .ToList();
        }
        catch { return new() { "Sin archivos recientes" }; }
    }

    private static List<string> GetRecentDownloads(string folder, int take = 3)
    {
        try
        {
            if (!Directory.Exists(folder)) return new() { "Carpeta no encontrada" };
            return Directory.GetFiles(folder)
                .OrderByDescending(File.GetLastWriteTime)
                .Take(take)
                .Select(f => $"{TruncateName(Path.GetFileName(f), 24)}  ·  {GetFileSize(f)}")
                .DefaultIfEmpty("Sin descargas recientes")
                .ToList();
        }
        catch { return new() { "Sin descargas recientes" }; }
    }

    private static (string Today, string Week) GetDocCounts()
    {
        try
        {
            var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string[] exts = [".docx", ".doc", ".odt", ".pdf", ".txt"];
            var files = Directory.GetFiles(docs, "*", SearchOption.TopDirectoryOnly)
                .Where(f => exts.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .Select(File.GetLastWriteTime)
                .ToList();
            var today = files.Count(d => d.Date == DateTime.Today);
            var week = files.Count(d => d >= DateTime.Today.AddDays(-7));
            return ($"{today} docs", $"{week} docs");
        }
        catch { return ("0 docs", "0 docs"); }
    }

    private static (string Total, string Used, string Free, string Type) GetDiskDetails()
    {
        try
        {
            var drive = new DriveInfo("C");
            var total = drive.TotalSize / (1024L * 1024 * 1024);
            var free = drive.AvailableFreeSpace / (1024L * 1024 * 1024);
            return ($"{total} GB", $"{total - free} GB", $"{free} GB", drive.DriveFormat);
        }
        catch { return ("N/A", "N/A", "N/A", "NTFS"); }
    }

    private static List<string> GetRecentDocs() =>
        GetRecentFiles(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            [".docx", ".doc", ".odt", ".pdf", ".txt"]);

    private static string GetDesktopAppVersion()
    {
        try { return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0"; }
        catch { return "1.0.0"; }
    }

    private static string GetApiBaseUrl() =>
        DesktopAssistant.Helpers.SecureStorage.GetApiBaseUrl();

    // ── Category builders ─────────────────────────────────────────────────

    private static SyncCategory BuildSystemCategory(
        int cpuPct, long ramTotal, long ramUsed, int ramPct,
        long diskTotal, long diskFree, int diskPct,
        string netType, string netSpeed, TimeSpan uptime)
    {
        var uptimeStr = uptime.Days > 0 ? $"{uptime.Days}d {uptime.Hours}h" : $"{uptime.Hours}h {uptime.Minutes}m";

        return new SyncCategory
        {
            Id = "system",
            Name = "SYSTEM MONITOR",
            Color = "#64748B",
            Icon = "Desktop",
            Shortcuts =
            [
                new()
                {
                    Id = "SYS_CPU",
                    Label = "PROCESADOR",
                    Icon = "Cpu",
                    Size = "big",
                    Subtitle = GetCpuName(),
                    Stats =
                    [
                        new() { Label = "USO", Value = $"{cpuPct}%" },
                        new() { Label = "NÚCLEOS", Value = $"{Environment.ProcessorCount}" },
                        new() { Label = "ARCO", Value = RuntimeInformation.OSArchitecture.ToString() },
                        new() { Label = "OS", Value = $"Win {Environment.OSVersion.Version.Major}" },
                    ],
                    ProgressValue = cpuPct,
                    SettingsGroups =
                    [
                        new()
                        {
                            Title = "Rendimiento",
                            Fields =
                            [
                                new() { Type = "select", Key = "power_plan", Label = "Plan de energía", Value = "Equilibrado", Options = ["Ahorro", "Equilibrado", "Alto rendimiento", "Máximo"] },
                                new() { Type = "toggle", Key = "turbo_boost", Label = "Turbo Boost", Value = true },
                                new() { Type = "slider", Key = "temp_alert", Label = "Alerta de temp.", Value = 85, Min = 60, Max = 100, Unit = "°C" },
                            ]
                        }
                    ]
                },
                new()
                {
                    Id = "SYS_RAM",
                    Label = "MEMORIA RAM",
                    Icon = "Memory",
                    Size = "tall",
                    Detail = $"RAM · {ramTotal} GB instalados",
                    Stats =
                    [
                        new() { Label = "USADA", Value = $"{ramUsed} GB" },
                        new() { Label = "LIBRE", Value = $"{ramTotal - ramUsed} GB" },
                        new() { Label = "USO", Value = $"{ramPct}%" },
                    ],
                    ProgressValue = ramPct,
                    SettingsGroups =
                    [
                        new()
                        {
                            Title = "Gestor RAM",
                            Fields =
                            [
                                new() { Type = "toggle", Key = "auto_flush", Label = "Liberación automática", Value = false },
                                new() { Type = "slider", Key = "flush_limit", Label = "Límite de uso", Value = 80, Min = 50, Max = 95, Unit = "%" },
                                new() { Type = "info", Key = "total", Label = "Total instalado", Value = $"{ramTotal} GB" },
                            ]
                        }
                    ]
                },
                new()
                {
                    Id = "SYS_DISK",
                    Label = "ALMACENAMIENTO",
                    Icon = "HardDrives",
                    Size = "wide",
                    Detail = $"Disco C: · {diskTotal} GB · {diskFree} GB libres",
                    ActionType = "slider",
                    Value = diskPct,
                    Min = 0,
                    Max = 100,
                    Unit = "%",
                    SettingsGroups =
                    [
                        new()
                        {
                            Title = "Disco Principal",
                            Fields =
                            [
                                new() { Type = "info", Key = "free", Label = "Espacio libre", Value = $"{diskFree} GB" },
                                new() { Type = "toggle", Key = "trim", Label = "Soporte TRIM", Value = true },
                                new() { Type = "slider", Key = "reserve", Label = "Espacio de reserva", Value = 10, Min = 0, Max = 20, Unit = "%" },
                            ]
                        }
                    ]
                },
                new()
                {
                    Id = "SYS_NET",
                    Label = "RED",
                    Icon = "WifiHigh",
                    Size = "wide",
                    Detail = $"{netType} · {netSpeed}",
                    ActionType = "chips",
                    Value = netType,
                    Options = ["WIFI", "ETHERNET", "VPN", "TODOS"],
                    SettingsGroups =
                    [
                        new()
                        {
                            Title = "Interfaces",
                            Fields =
                            [
                                new() { Type = "select", Key = "pref", Label = "Red preferida", Value = "Auto", Options = ["Auto", "LAN", "WLAN"] },
                                new() { Type = "toggle", Key = "vpn_kill", Label = "Killswitch VPN", Value = false },
                                new() { Type = "toggle", Key = "metered", Label = "Conexión medida", Value = false },
                            ]
                        }
                    ]
                },
                new()
                {
                    Id = "SYS_UPT",
                    Label = "UPTIME",
                    Icon = "Clock",
                    Size = "small",
                    Value = uptimeStr,
                    ActionType = "status",
                    SettingsGroups =
                    [
                        new()
                        {
                            Title = "Estado del Sistema",
                            Fields =
                            [
                                new() { Type = "info", Key = "os", Label = "Sistema Operativo", Value = GetOsName() },
                                new() { Type = "toggle", Key = "fast_boot", Label = "Inicio Rápido", Value = true },
                            ]
                        }
                    ]
                },
            ]
        };
    }

    private static SyncCategory BuildBrowserCategory(AppDetected browser)
    {
        var color = browser.Name.Contains("chrome", StringComparison.OrdinalIgnoreCase) ? "#4285F4"
            : browser.Name.Contains("firefox", StringComparison.OrdinalIgnoreCase) ? "#FF7139"
            : browser.Name.Contains("edge", StringComparison.OrdinalIgnoreCase) ? "#0078D4"
            : browser.Name.Contains("brave", StringComparison.OrdinalIgnoreCase) ? "#FB542B"
            : "#3B82F6";

        var downloadsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        var dlCount = Directory.Exists(downloadsFolder) ? Directory.GetFiles(downloadsFolder).Length : 0;
        var dlFreeGB = GetFreeGBForPath(downloadsFolder);

        return new SyncCategory
        {
            Id = "browser",
            Name = browser.Name.ToUpperInvariant(),
            Color = color,
            Icon = "Globe",
            Shortcuts =
            [
                new()
                {
                    Id = "BR_TABS",
                    Label = "NAVEGADOR",
                    Icon = "Globe",
                    Size = "big",
                    Subtitle = $"{browser.Name} · {browser.Version}",
                    Stats =
                    [
                        new() { Label = "ESTADO", Value = "Instalado" },
                        new() { Label = "VERSIÓN", Value = browser.Version },
                        new() { Label = "MOTOR", Value = GetBrowserEngine(browser.Name) },
                        new() { Label = "TIPO", Value = "Desktop" },
                    ],
                    ProgressValue = 0,
                    SettingsGroups =
                    [
                        new()
                        {
                            Title = "Rendimiento",
                            Fields =
                            [
                                new() { Type = "toggle", Key = "tab_sleep", Label = "Suspender tabs inactivas", Value = true },
                                new() { Type = "slider", Key = "sleep_after", Label = "Suspender después de", Value = 5, Min = 1, Max = 60, Unit = "min" },
                                new() { Type = "toggle", Key = "preload", Label = "Precargar páginas", Value = false },
                            ]
                        },
                        new()
                        {
                            Title = "Privacidad",
                            Fields =
                            [
                                new() { Type = "toggle", Key = "tracking", Label = "Bloquear rastreadores", Value = true },
                                new() { Type = "select", Key = "dns", Label = "DNS seguro", Value = "Cloudflare", Options = ["Sistema", "Google", "Cloudflare", "Quad9"] },
                            ]
                        }
                    ]
                },
                new()
                {
                    Id = "BR_DL",
                    Label = "DESCARGAS",
                    Icon = "DownloadSimple",
                    Size = "tall",
                    Detail = $"{dlCount} archivos · {dlFreeGB} GB libres",
                    Stats =
                    [
                        new() { Label = "ARCHIVOS", Value = dlCount.ToString() },
                        new() { Label = "LIBRES", Value = $"{dlFreeGB} GB" },
                        new() { Label = "CARPETA", Value = "Descargas" },
                    ],
                    Logs = GetRecentDownloads(downloadsFolder),
                    SettingsGroups =
                    [
                        new()
                        {
                            Title = "Rutas",
                            Fields =
                            [
                                new() { Type = "info", Key = "path", Label = "Guardar en", Value = downloadsFolder },
                                new() { Type = "toggle", Key = "ask", Label = "Preguntar ubicación", Value = false },
                            ]
                        }
                    ]
                },
                new()
                {
                    Id = "BR_ZOOM",
                    Label = "ZOOM DE PÁGINA",
                    Icon = "MagnifyingGlass",
                    Size = "wide",
                    Detail = "Nivel de zoom del navegador",
                    ActionType = "chips",
                    Value = "100%",
                    Options = ["75%", "90%", "100%", "125%", "150%"],
                    SettingsGroups =
                    [
                        new()
                        {
                            Title = "Accesibilidad",
                            Fields =
                            [
                                new() { Type = "toggle", Key = "high_contrast", Label = "Alto contraste web", Value = false },
                                new() { Type = "slider", Key = "font_size", Label = "Tamaño de fuente", Value = 16, Min = 10, Max = 24, Unit = "px" },
                            ]
                        }
                    ]
                },
                new()
                {
                    Id = "BR_SHIELD",
                    Label = "AD BLOCKER",
                    Icon = "ShieldCheck",
                    Size = "small",
                    Value = true,
                    ActionType = "toggle",
                    SettingsGroups =
                    [
                        new()
                        {
                            Title = "Filtros",
                            Fields =
                            [
                                new() { Type = "toggle", Key = "cosmetic", Label = "Filtros cosméticos", Value = true },
                                new() { Type = "toggle", Key = "social", Label = "Bloquear widgets sociales", Value = true },
                                new() { Type = "select", Key = "lists", Label = "Listas activas", Value = "EasyList", Options = ["EasyList", "UBlock", "Fanboy"] },
                            ]
                        }
                    ]
                },
                new()
                {
                    Id = "BR_BM",
                    Label = "MARCADORES",
                    Icon = "BookmarkSimple",
                    Size = "small",
                    Value = "Sincronizado",
                    ActionType = "status",
                    SettingsGroups =
                    [
                        new()
                        {
                            Title = "Sincronización",
                            Fields =
                            [
                                new() { Type = "toggle", Key = "sync_pass", Label = "Sincronizar contraseñas", Value = true },
                                new() { Type = "toggle", Key = "sync_ext", Label = "Sincronizar extensiones", Value = true },
                            ]
                        }
                    ]
                },
            ]
        };
    }

    private static SyncCategory BuildWordCategory(AppDetected wordApp)
    {
        var isLibre = wordApp.Name.Contains("libre", StringComparison.OrdinalIgnoreCase);
        var defaultFmt = isLibre ? ".odt" : ".docx";
        var (todayCount, weekCount) = GetDocCounts();

        return new SyncCategory
        {
            Id = "word",
            Name = "WORD PROCESSOR",
            Color = "#2563EB",
            Icon = "FileDoc",
            Shortcuts =
            [
                new()
                {
                    Id = "WD_DOC",
                    Label = "PROCESADOR DE TEXTO",
                    Icon = "FileDoc",
                    Size = "big",
                    Subtitle = wordApp.Name,
                    Stats =
                    [
                        new() { Label = "APP", Value = wordApp.Name },
                        new() { Label = "VERSIÓN", Value = wordApp.Version },
                        new() { Label = "FORMATO", Value = defaultFmt },
                        new() { Label = "ESTADO", Value = "Instalado" },
                    ],
                    ProgressValue = 0,
                    SettingsGroups =
                    [
                        new()
                        {
                            Title = "Autoguardado",
                            Fields =
                            [
                                new() { Type = "toggle", Key = "autosave", Label = "Autoguardado", Value = true },
                                new() { Type = "slider", Key = "save_every", Label = "Guardar cada", Value = 2, Min = 1, Max = 30, Unit = "min" },
                                new() { Type = "select", Key = "format", Label = "Formato por defecto", Value = defaultFmt, Options = [".docx", ".odt", ".pdf", ".txt"] },
                            ]
                        },
                        new()
                        {
                            Title = "Revisión",
                            Fields =
                            [
                                new() { Type = "toggle", Key = "spellcheck", Label = "Corrección ortográfica", Value = true },
                                new() { Type = "toggle", Key = "track", Label = "Control de cambios", Value = false },
                            ]
                        }
                    ]
                },
                new()
                {
                    Id = "WD_RECENT",
                    Label = "RECIENTES",
                    Icon = "Clock",
                    Size = "tall",
                    Detail = "Documentos recientes",
                    Stats =
                    [
                        new() { Label = "HOY", Value = todayCount },
                        new() { Label = "SEMANA", Value = weekCount },
                        new() { Label = "CARPETA", Value = "Documentos" },
                    ],
                    Logs = GetRecentDocs(),
                    SettingsGroups =
                    [
                        new()
                        {
                            Title = "Historial",
                            Fields =
                            [
                                new() { Type = "toggle", Key = "keep_history", Label = "Mantener historial", Value = true },
                                new() { Type = "slider", Key = "max_docs", Label = "Límite en lista", Value = 15, Min = 5, Max = 50 },
                            ]
                        }
                    ]
                },
                new()
                {
                    Id = "WD_PRINT",
                    Label = "IMPRESIÓN",
                    Icon = "Printer",
                    Size = "wide",
                    Detail = GetDefaultPrinter(),
                    ActionType = "chips",
                    Value = "NORMAL",
                    Options = ["BORRADOR", "NORMAL", "ALTA CAL.", "COLOR"],
                    SettingsGroups =
                    [
                        new()
                        {
                            Title = "Impresora",
                            Fields =
                            [
                                new() { Type = "info", Key = "printer", Label = "Impresora predeterminada", Value = GetDefaultPrinter() },
                                new() { Type = "toggle", Key = "eco_print", Label = "Modo Ecológico", Value = false },
                            ]
                        }
                    ]
                },
                new()
                {
                    Id = "WD_CLOUD",
                    Label = "NUBE SYNC",
                    Icon = "CloudArrowUp",
                    Size = "small",
                    Value = true,
                    ActionType = "toggle",
                    SettingsGroups =
                    [
                        new()
                        {
                            Title = "Nube",
                            Fields =
                            [
                                new() { Type = "toggle", Key = "wifi_only", Label = "Solo sincronizar WiFi", Value = true },
                                new() { Type = "info", Key = "quota", Label = "Almacenamiento", Value = GetOneDriveFreeSpace() },
                            ]
                        }
                    ]
                },
                new()
                {
                    Id = "WD_LANG",
                    Label = "IDIOMA",
                    Icon = "Globe",
                    Size = "small",
                    Value = GetSystemLanguage(),
                    ActionType = "status",
                    SettingsGroups =
                    [
                        new()
                        {
                            Title = "Idioma",
                            Fields =
                            [
                                new() { Type = "select", Key = "dict", Label = "Idioma del sistema", Value = GetSystemLanguage(), Options = ["Español (México)", "Español (España)", "English (US)"] },
                                new() { Type = "toggle", Key = "auto_lang", Label = "Detectar idioma escrito", Value = true },
                            ]
                        }
                    ]
                },
            ]
        };
    }

    private static SyncCategory BuildSpreadsheetCategory(AppDetected sheetApp)
    {
        var isLibre = sheetApp.Name.Contains("libre", StringComparison.OrdinalIgnoreCase);
        var defaultFmt = isLibre ? ".ods" : ".xlsx";

        return new SyncCategory
        {
            Id = "excel",
            Name = "SPREADSHEET",
            Color = "#16A34A",
            Icon = "Table",
            Shortcuts =
            [
                new()
                {
                    Id = "XL_SHEET",
                    Label = "HOJA DE CÁLCULO",
                    Icon = "Table",
                    Size = "big",
                    Subtitle = sheetApp.Name,
                    Stats =
                    [
                        new() { Label = "APP", Value = sheetApp.Name },
                        new() { Label = "VERSIÓN", Value = sheetApp.Version },
                        new() { Label = "FORMATO", Value = defaultFmt },
                        new() { Label = "ESTADO", Value = "Instalado" },
                    ],
                    ProgressValue = 0,
                    SettingsGroups =
                    [
                        new()
                        {
                            Title = "Cálculo",
                            Fields =
                            [
                                new() { Type = "select", Key = "calc_mode", Label = "Modo de cálculo", Value = "Automático", Options = ["Automático", "Manual", "Excepto tablas"] },
                                new() { Type = "toggle", Key = "iter_calc", Label = "Cálculo iterativo", Value = false },
                                new() { Type = "slider", Key = "max_iter", Label = "Máx. iteraciones", Value = 100, Min = 1, Max = 1000 },
                            ]
                        },
                        new()
                        {
                            Title = "Visualización",
                            Fields =
                            [
                                new() { Type = "toggle", Key = "gridlines", Label = "Líneas de cuadrícula", Value = true },
                                new() { Type = "toggle", Key = "formula_bar", Label = "Barra de fórmulas", Value = true },
                            ]
                        }
                    ]
                },
                new()
                {
                    Id = "XL_ERRORS",
                    Label = "VALIDACIÓN",
                    Icon = "Activity",
                    Size = "tall",
                    Detail = "Revisión de fórmulas",
                    Stats =
                    [
                        new() { Label = "TIPO", Value = isLibre ? "LibreOffice" : "Microsoft" },
                        new() { Label = "FORMATO", Value = defaultFmt },
                        new() { Label = "ESTADO", Value = "Listo" },
                    ],
                    Logs = ["Herramienta de validación lista.", "Revisión automática activa.", "Sin errores detectados."],
                    SettingsGroups =
                    [
                        new()
                        {
                            Title = "Depuración",
                            Fields =
                            [
                                new() { Type = "toggle", Key = "highlight_err", Label = "Resaltar errores", Value = true },
                                new() { Type = "select", Key = "err_color", Label = "Color de error", Value = "Rojo", Options = ["Rojo", "Amarillo", "Rosa"] },
                            ]
                        }
                    ]
                },
                new()
                {
                    Id = "XL_ZOOM",
                    Label = "ZOOM",
                    Icon = "MagnifyingGlass",
                    Size = "wide",
                    Detail = "Nivel de zoom de la hoja activa",
                    ActionType = "chips",
                    Value = "100%",
                    Options = ["75%", "100%", "125%", "150%", "200%"],
                    SettingsGroups =
                    [
                        new()
                        {
                            Title = "Presentación",
                            Fields =
                            [
                                new() { Type = "select", Key = "zoom_mode", Label = "Ajuste inicial", Value = "100%", Options = ["100%", "Ajustar ancho", "Ajustar selección"] },
                            ]
                        }
                    ]
                },
                new()
                {
                    Id = "XL_CLOUD",
                    Label = "NUBE SYNC",
                    Icon = "CloudArrowUp",
                    Size = "small",
                    Value = true,
                    ActionType = "toggle",
                    SettingsGroups =
                    [
                        new()
                        {
                            Title = "Co-Autoría",
                            Fields =
                            [
                                new() { Type = "toggle", Key = "live_edit", Label = "Mostrar cursores en vivo", Value = true },
                                new() { Type = "info", Key = "storage", Label = "Almacenamiento", Value = GetOneDriveFreeSpace() },
                            ]
                        }
                    ]
                },
                new()
                {
                    Id = "XL_REC",
                    Label = "MACRO REC.",
                    Icon = "ArrowClockwise",
                    Size = "small",
                    Value = false,
                    ActionType = "toggle",
                    SettingsGroups =
                    [
                        new()
                        {
                            Title = "Seguridad",
                            Fields =
                            [
                                new() { Type = "toggle", Key = "macro_sec", Label = "Deshabilitar macros sin firma", Value = true },
                            ]
                        }
                    ]
                },
            ]
        };
    }

    private static SyncCategory BuildSpotifyCategory()
    {
        return new SyncCategory
        {
            Id = "spotify",
            Name = "SPOTIFY",
            Color = "#1DB954",
            Icon = "MusicNotes",
            Shortcuts =
            [
                // ── Now Playing (interactive MediaControlTile) ──
                new()
                {
                    Id = "MED_NOW",
                    Label = "NOW PLAYING",
                    Icon = "MusicNotes",
                    Size = "big",
                    Subtitle = "Sin reproducción activa",
                    Stats =
                    [
                        new() { Label = "PISTA",   Value = "—" },
                        new() { Label = "ARTISTA", Value = "—" },
                        new() { Label = "ÁLBUM",   Value = "—" },
                        new() { Label = "CALIDAD", Value = "—" },
                    ],
                    ProgressValue = 0,
                    ProgressLabel = ["0:00", "0:00"],
                    Command = new() { Action = ActionTypes.MediaPlay },
                },
                // ── Cola ──────────────────────────────────────────
                new()
                {
                    Id = "SP_QUEUE",
                    Label = "EN COLA",
                    Icon = "Stack",
                    Size = "tall",
                    Detail = "Lista de reproducción activa",
                    Stats =
                    [
                        new() { Label = "ESTADO",  Value = "En espera" },
                        new() { Label = "TIPO",    Value = "Streaming" },
                        new() { Label = "CALIDAD", Value = "Very High" },
                    ],
                    Logs = ["Abre Spotify para ver la cola.", "Controla desde el móvil.", "Sincronización en tiempo real."],
                    SettingsGroups =
                    [
                        new()
                        {
                            Title = "Reproducción",
                            Fields =
                            [
                                new() { Type = "toggle", Key = "auto_dj", Label = "Autoplay cuando termine", Value = true },
                                new() { Type = "toggle", Key = "dj_ai",  Label = "Modo DJ AI",             Value = false },
                            ]
                        }
                    ]
                },
                // ── Volumen Spotify ───────────────────────────────
                new()
                {
                    Id = "SP_VOL",
                    Label = "VOLUMEN SPOTIFY",
                    Icon = "SpeakerHigh",
                    Size = "wide",
                    Detail = "Salida de audio de Spotify",
                    ActionType = "slider",
                    Value = 72,
                    Min = 0,
                    Max = 100,
                    Unit = "%",
                    Command = new() { Action = ActionTypes.SetVolume, Target = "spotify", Params = new() { ["value"] = 72 } },
                    SettingsGroups =
                    [
                        new()
                        {
                            Title = "Mezclador",
                            Fields =
                            [
                                new() { Type = "toggle", Key = "eq_active",  Label = "Ecualizador",  Value = false },
                                new() { Type = "slider", Key = "bass_boost", Label = "Bass Boost",   Value = 0, Min = 0, Max = 100, Unit = "%" },
                            ]
                        }
                    ]
                },
                // ── Volumen del sistema ───────────────────────────
                new()
                {
                    Id = "MED_VOL",
                    Label = "VOL. SISTEMA",
                    Icon = "SpeakerSimpleHigh",
                    Size = "wide",
                    Detail = "Volumen general del sistema",
                    ActionType = "slider",
                    Value = 70,
                    Min = 0,
                    Max = 100,
                    Unit = "%",
                    Command = new() { Action = ActionTypes.SetVolume, Params = new() { ["value"] = 70 } },
                },
                // ── Controles de reproducción ─────────────────────
                new()
                {
                    Id = "MED_PREV",
                    Label = "ANTERIOR",
                    Icon = "SkipBack",
                    Size = "small",
                    ActionType = "status",
                    Value = "⏮",
                    Command = new() { Action = ActionTypes.MediaPrev },
                },
                new()
                {
                    Id = "MED_PLAY",
                    Label = "PLAY / PAUSE",
                    Icon = "Play",
                    Size = "small",
                    ActionType = "toggle",
                    Value = false,
                    Command = new() { Action = ActionTypes.MediaPlay },
                },
                new()
                {
                    Id = "MED_NEXT",
                    Label = "SIGUIENTE",
                    Icon = "SkipForward",
                    Size = "small",
                    ActionType = "status",
                    Value = "⏭",
                    Command = new() { Action = ActionTypes.MediaNext },
                },
                new()
                {
                    Id = "MED_STOP",
                    Label = "DETENER",
                    Icon = "Stop",
                    Size = "small",
                    ActionType = "status",
                    Value = "⏹",
                    Command = new() { Action = ActionTypes.MediaStop },
                },
                new()
                {
                    Id = "MED_SHUF",
                    Label = "ALEATORIO",
                    Icon = "Shuffle",
                    Size = "small",
                    ActionType = "toggle",
                    Value = false,
                    Command = new() { Action = ActionTypes.MediaShuffle },
                },
                new()
                {
                    Id = "MED_MUTE",
                    Label = "SILENCIAR",
                    Icon = "SpeakerSlash",
                    Size = "small",
                    ActionType = "toggle",
                    Value = false,
                    Command = new() { Action = ActionTypes.VolumeMute },
                },
                // ── Podcasts ──────────────────────────────────────
                new()
                {
                    Id = "SP_PODCAST",
                    Label = "PODCASTS",
                    Icon = "VideoCamera",
                    Size = "small",
                    Value = "Activo",
                    ActionType = "status",
                    SettingsGroups =
                    [
                        new()
                        {
                            Title = "Suscripciones",
                            Fields =
                            [
                                new() { Type = "toggle", Key = "pod_down",  Label = "Descargar nuevos eps. en WiFi", Value = true },
                                new() { Type = "slider", Key = "pod_speed", Label = "Velocidad base", Value = 1, Min = 1, Max = 3, Unit = "x" },
                            ]
                        }
                    ]
                },
            ]
        };
    }

    private static SyncCategory BuildDevToolsCategory(DetectedTools tools)
    {
        var primary = tools.Primary;
        var all = tools.All;

        var editor = all.FirstOrDefault(a =>
            a.Name.Contains("visual studio code", StringComparison.OrdinalIgnoreCase) ||
            a.Name.Contains("vscode", StringComparison.OrdinalIgnoreCase));

        var vsFullIde = all.FirstOrDefault(a =>
            a.Name.Contains("visual studio 20", StringComparison.OrdinalIgnoreCase) ||
            a.Name.Contains("visual studio community", StringComparison.OrdinalIgnoreCase) ||
            a.Name.Contains("visual studio professional", StringComparison.OrdinalIgnoreCase));

        var github = all.FirstOrDefault(a =>
            a.Name.Contains("github desktop", StringComparison.OrdinalIgnoreCase));

        var postman = all.FirstOrDefault(a =>
            a.Name.Contains("postman", StringComparison.OrdinalIgnoreCase) ||
            a.Name.Contains("insomnia", StringComparison.OrdinalIgnoreCase));

        var docker = all.FirstOrDefault(a =>
            a.Name.Contains("docker", StringComparison.OrdinalIgnoreCase));

        var claudeApp = all.FirstOrDefault(a =>
            a.Name.Equals("claude", StringComparison.OrdinalIgnoreCase) ||
            a.Name.Contains("claude for", StringComparison.OrdinalIgnoreCase));

        var node = all.FirstOrDefault(a =>
            a.Name.Contains("node.js", StringComparison.OrdinalIgnoreCase));

        var python = all.FirstOrDefault(a =>
            a.Name.Contains("python", StringComparison.OrdinalIgnoreCase));

        var appNames = string.Join(", ", all.Take(4).Select(a => a.Name.Split(' ')[0]));
        var shortcuts = new List<SyncShortcut>();

        // ── Editor principal ─────────────────────────────────
        var editorEntry = editor ?? vsFullIde ?? primary;
        shortcuts.Add(new()
        {
            Id = "DEV_EDITOR",
            Label = "EDITOR DE CÓDIGO",
            Icon = "Code",
            Size = "big",
            Subtitle = editorEntry.Name,
            Stats =
            [
                new() { Label = "APP", Value = editorEntry.Name },
                new() { Label = "VERSIÓN", Value = editorEntry.Version },
                new() { Label = "TIPO", Value = editorEntry.Name.Contains("Visual Studio Code", StringComparison.OrdinalIgnoreCase) ? "Editor ligero" : "IDE completo" },
                new() { Label = "HERRAMIENTAS", Value = $"{all.Count} detectadas" },
            ],
            ProgressValue = 0,
            SettingsGroups =
            [
                new()
                {
                    Title = "Editor",
                    Fields =
                    [
                        new() { Type = "toggle", Key = "autosave", Label = "Guardado automático", Value = true },
                        new() { Type = "toggle", Key = "minimap", Label = "Minimapa", Value = true },
                        new() { Type = "slider", Key = "font_size", Label = "Tamaño de fuente", Value = 14, Min = 10, Max = 24, Unit = "px" },
                        new() { Type = "select", Key = "theme", Label = "Tema", Value = "Dark+", Options = ["Dark+", "One Dark Pro", "Dracula", "Monokai", "Light+"] },
                    ]
                },
                new()
                {
                    Title = "Terminal",
                    Fields =
                    [
                        new() { Type = "select", Key = "shell", Label = "Shell predeterminado", Value = "PowerShell", Options = ["PowerShell", "Command Prompt", "Git Bash", "WSL"] },
                        new() { Type = "toggle", Key = "integrated_term", Label = "Terminal integrada", Value = true },
                    ]
                }
            ]
        });

        // ── Herramientas detectadas ───────────────────────────
        var devLogs = all.Select(a => $"{a.Name}  ·  {a.Version}").ToList();
        shortcuts.Add(new()
        {
            Id = "DEV_TOOLS",
            Label = "HERRAMIENTAS",
            Icon = "Wrench",
            Size = "tall",
            Detail = $"{all.Count} herramientas de desarrollo",
            Stats =
            [
                new() { Label = "TOTAL", Value = $"{all.Count} apps" },
                new() { Label = "EDITOR", Value = editor != null ? "VS Code" : vsFullIde != null ? "Visual Studio" : "Otro" },
                new() { Label = "GIT", Value = github != null ? "GitHub Desktop" : "Git CLI" },
                new() { Label = "AI", Value = claudeApp != null ? "Claude" : "No detectado" },
            ],
            Logs = devLogs.Take(5).ToList(),
            SettingsGroups =
            [
                new()
                {
                    Title = "Entorno",
                    Fields =
                    [
                        new() { Type = "info", Key = "apps", Label = "Apps detectadas", Value = appNames },
                        new() { Type = "toggle", Key = "path_sync", Label = "Sincronizar PATH al iniciar", Value = false },
                    ]
                }
            ]
        });

        // ── Control de versiones ──────────────────────────────
        shortcuts.Add(new()
        {
            Id = "DEV_GIT",
            Label = "CONTROL DE VERSIONES",
            Icon = "GitBranch",
            Size = "wide",
            Detail = github != null ? "GitHub Desktop instalado" : "Git CLI disponible",
            ActionType = "chips",
            Value = "GIT",
            Options = ["GIT", "GITHUB", "GITLAB", "BITBUCKET"],
            SettingsGroups =
            [
                new()
                {
                    Title = "Git",
                    Fields =
                    [
                        new() { Type = "toggle", Key = "auto_fetch", Label = "Auto-fetch al abrir", Value = true },
                        new() { Type = "select", Key = "default_branch", Label = "Rama por defecto", Value = "main", Options = ["main", "master", "develop"] },
                        new() { Type = "toggle", Key = "sign_commits", Label = "Firmar commits (GPG)", Value = false },
                    ]
                }
            ]
        });

        // ── Runtime / entorno ─────────────────────────────────
        var runtimeName = node != null ? "Node.js" : python != null ? "Python" : "Runtime";
        var runtimeVer = node?.Version ?? python?.Version ?? "Detectado";
        shortcuts.Add(new()
        {
            Id = "DEV_RUNTIME",
            Label = runtimeName.ToUpperInvariant(),
            Icon = "Terminal",
            Size = "small",
            Value = runtimeVer,
            ActionType = "status",
            SettingsGroups =
            [
                new()
                {
                    Title = "Runtime",
                    Fields =
                    [
                        new() { Type = "info", Key = "version", Label = "Versión", Value = runtimeVer },
                        new() { Type = "toggle", Key = "nvm", Label = "Gestor de versiones (nvm/pyenv)", Value = false },
                        new() { Type = "select", Key = "pkg_manager", Label = "Gestor de paquetes", Value = node != null ? "npm" : "pip", Options = node != null ? ["npm", "yarn", "pnpm"] : ["pip", "conda", "poetry"] },
                    ]
                }
            ]
        });

        // ── API client (Postman / Insomnia) ───────────────────
        if (postman != null)
        {
            shortcuts.Add(new()
            {
                Id = "DEV_API",
                Label = "API CLIENT",
                Icon = "ArrowsLeftRight",
                Size = "small",
                Value = postman.Name.Contains("insomnia", StringComparison.OrdinalIgnoreCase) ? "INSOMNIA" : "POSTMAN",
                ActionType = "status",
                SettingsGroups =
                [
                    new()
                    {
                        Title = postman.Name,
                        Fields =
                        [
                            new() { Type = "info", Key = "version", Label = "Versión", Value = postman.Version },
                            new() { Type = "toggle", Key = "sync_collections", Label = "Sincronizar colecciones", Value = true },
                        ]
                    }
                ]
            });
        }

        // ── Docker ────────────────────────────────────────────
        if (docker != null)
        {
            shortcuts.Add(new()
            {
                Id = "DEV_DOCKER",
                Label = "DOCKER",
                Icon = "Package",
                Size = "small",
                Value = docker.Version,
                ActionType = "status",
                SettingsGroups =
                [
                    new()
                    {
                        Title = "Docker Desktop",
                        Fields =
                        [
                            new() { Type = "info", Key = "version", Label = "Versión", Value = docker.Version },
                            new() { Type = "toggle", Key = "start_on_login", Label = "Iniciar con Windows", Value = true },
                            new() { Type = "toggle", Key = "wsl2_backend", Label = "Backend WSL2", Value = true },
                        ]
                    }
                ]
            });
        }

        return new SyncCategory
        {
            Id = "dev",
            Name = "DEV TOOLS",
            Color = "#06B6D4",
            Icon = "Code",
            Shortcuts = shortcuts,
        };
    }

    private static SyncCategory BuildExplorerCategory(long diskTotal, long diskUsed, long diskFree, int diskPct)
    {
        var (_, _, _, driveFormat) = GetDiskDetails();

        return new SyncCategory
        {
            Id = "explorer",
            Name = "FILE EXPLORER",
            Color = "#F59E0B",
            Icon = "Folder",
            Shortcuts =
            [
                new()
                {
                    Id = "EX_DISK",
                    Label = "DISCO PRINCIPAL",
                    Icon = "HardDrives",
                    Size = "big",
                    Subtitle = $"Disco C: · {driveFormat}",
                    Stats =
                    [
                        new() { Label = "TOTAL", Value = $"{diskTotal} GB" },
                        new() { Label = "USADO", Value = $"{diskUsed} GB" },
                        new() { Label = "LIBRE", Value = $"{diskFree} GB" },
                        new() { Label = "TIPO", Value = driveFormat },
                    ],
                    ProgressValue = diskPct,
                    SettingsGroups =
                    [
                        new()
                        {
                            Title = "Limpieza",
                            Fields =
                            [
                                new() { Type = "toggle", Key = "recycle_alert", Label = "Alerta papelera llena", Value = true },
                                new() { Type = "slider", Key = "recycle_limit", Label = "Tamaño máx. papelera", Value = 10, Min = 1, Max = 50, Unit = "GB" },
                                new() { Type = "toggle", Key = "compress_old", Label = "Comprimir archivos viejos", Value = false },
                            ]
                        },
                        new()
                        {
                            Title = "Vista",
                            Fields =
                            [
                                new() { Type = "select", Key = "view_mode", Label = "Modo de vista", Value = "Detalles", Options = ["Íconos", "Lista", "Detalles", "Mosaico"] },
                                new() { Type = "toggle", Key = "hidden_files", Label = "Mostrar archivos ocultos", Value = false },
                            ]
                        }
                    ]
                },
                new()
                {
                    Id = "EX_RECENT",
                    Label = "ARCHIVOS RECIENTES",
                    Icon = "Clock",
                    Size = "tall",
                    Detail = "Últimas modificaciones",
                    Stats =
                    [
                        new() { Label = "DOCUMENTOS", Value = "Carpeta Docs" },
                        new() { Label = "DESCARGAS", Value = "Carpeta DL" },
                        new() { Label = "ESCRITORIO", Value = "Desktop" },
                    ],
                    Logs = GetRecentFiles(
                        Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                        [".lnk", ".exe", ".txt", ".pdf", ".docx"]),
                    SettingsGroups =
                    [
                        new()
                        {
                            Title = "Indización",
                            Fields =
                            [
                                new() { Type = "toggle", Key = "windows_search", Label = "Búsqueda rápida indexada", Value = true },
                                new() { Type = "info", Key = "index_root", Label = "Raíz indexada", Value = "C:\\" },
                            ]
                        }
                    ]
                },
                new()
                {
                    Id = "EX_ZIP",
                    Label = "COMPRIMIR",
                    Icon = "FileZip",
                    Size = "wide",
                    Detail = "Formato de compresión",
                    ActionType = "chips",
                    Value = "ZIP",
                    Options = ["ZIP", "7Z", "TAR.GZ", "RAR"],
                    SettingsGroups =
                    [
                        new()
                        {
                            Title = "Algoritmo",
                            Fields =
                            [
                                new() { Type = "select", Key = "zip_level", Label = "Nivel de compresión", Value = "Máxima", Options = ["Rápida", "Normal", "Máxima", "Ultra"] },
                                new() { Type = "toggle", Key = "zip_del", Label = "Borrar original al terminar", Value = false },
                            ]
                        }
                    ]
                },
                new()
                {
                    Id = "EX_CLOUD",
                    Label = "ONEDRIVE",
                    Icon = "CloudArrowUp",
                    Size = "small",
                    Value = Directory.Exists(Environment.GetEnvironmentVariable("OneDrive") ?? "") ? "SYNC" : "OFF",
                    ActionType = "status",
                    SettingsGroups =
                    [
                        new()
                        {
                            Title = "Nube Activa",
                            Fields =
                            [
                                new() { Type = "toggle", Key = "onedrive_boot", Label = "Arrancar con Windows", Value = true },
                                new() { Type = "info", Key = "one_quota", Label = "Espacio libre", Value = GetOneDriveFreeSpace() },
                            ]
                        }
                    ]
                },
                new()
                {
                    Id = "EX_HIDDEN",
                    Label = "ARCHIVOS OCULTOS",
                    Icon = "LockKey",
                    Size = "small",
                    Value = false,
                    ActionType = "toggle",
                    SettingsGroups =
                    [
                        new()
                        {
                            Title = "Privacidad",
                            Fields =
                            [
                                new() { Type = "toggle", Key = "sys_hidden", Label = "Ocultar archivos del OS", Value = true },
                                new() { Type = "toggle", Key = "ext_hidden", Label = "Ocultar extensiones", Value = false },
                            ]
                        }
                    ]
                },
            ]
        };
    }

    private static SyncCategory BuildXToolsCategory()
    {
        return new SyncCategory
        {
            Id = "xtools",
            Name = "X-TOOLS",
            Color = "#00F0FF",
            Icon = "Wrench",
            Shortcuts =
            [
                new()
                {
                    Id = "T_TRACKPAD",
                    Label = "TRACKPAD TÁCTIL",
                    Icon = "HandPointing",
                    Size = "big",
                    Subtitle = "Control remoto por gestos desde el móvil",
                    Stats =
                    [
                        new() { Label = "ESTADO", Value = "Disponible" },
                        new() { Label = "LATENCIA", Value = "< 20ms" },
                        new() { Label = "GESTOS", Value = "Tap, Swipe, Drag" },
                        new() { Label = "MODO", Value = "Mouse" },
                    ],
                    ProgressValue = 100,
                    SettingsGroups =
                    [
                        new()
                        {
                            Title = "Sensibilidad",
                            Fields =
                            [
                                new() { Type = "slider", Key = "sensitivity", Label = "Sensibilidad del puntero", Value = 70, Min = 10, Max = 100, Unit = "%" },
                                new() { Type = "toggle", Key = "natural_scroll", Label = "Desplazamiento natural", Value = false },
                                new() { Type = "toggle", Key = "tap_click", Label = "Toque como clic", Value = true },
                            ]
                        }
                    ]
                },
                new()
                {
                    Id = "T_FILES",
                    Label = "GESTOR DE ARCHIVOS",
                    Icon = "Folder",
                    Size = "tall",
                    Detail = "Acceso a archivos desde el móvil",
                    Stats =
                    [
                        new() { Label = "MODO", Value = "Local" },
                        new() { Label = "SOPORTE", Value = "PDF, IMG, DOC" },
                        new() { Label = "ESTADO", Value = "Listo" },
                    ],
                    Logs = ["Soporta transferencia de fotos.", "Soporta PDFs y documentos.", "Acceso seguro con token."],
                    SettingsGroups =
                    [
                        new()
                        {
                            Title = "Acceso",
                            Fields =
                            [
                                new() { Type = "toggle", Key = "allow_upload", Label = "Permitir subida de archivos", Value = true },
                                new() { Type = "toggle", Key = "allow_download", Label = "Permitir descarga", Value = true },
                                new() { Type = "select", Key = "root_dir", Label = "Directorio raíz", Value = "Documentos", Options = ["Documentos", "Descargas", "Escritorio", "C:\\"] },
                            ]
                        }
                    ]
                },
                new()
                {
                    Id = "T_AUDIO",
                    Label = "LAB DE AUDIO",
                    Icon = "Microphone",
                    Size = "wide",
                    Detail = "Grabación y reproducción desde el móvil",
                    ActionType = "chips",
                    Value = "STOP",
                    Options = ["RECORD", "PLAY", "STOP"],
                    SettingsGroups =
                    [
                        new()
                        {
                            Title = "Grabación",
                            Fields =
                            [
                                new() { Type = "select", Key = "format", Label = "Formato de audio", Value = "WAV", Options = ["WAV", "MP3", "OGG"] },
                                new() { Type = "slider", Key = "sample_rate", Label = "Frecuencia de muestreo", Value = 44100, Min = 8000, Max = 48000, Unit = "Hz" },
                                new() { Type = "toggle", Key = "noise_cancel", Label = "Cancelación de ruido", Value = false },
                            ]
                        }
                    ]
                },
            ]
        };
    }

    private enum AppLibraryFamily
    {
        Browser,
        Productivity,
        Communication,
        Media,
        Creative,
        Development,
        Utility,
        Other,
    }

    private static SyncCategory BuildActiveAppCategory(AppInfo activeApp, List<AppInfo> apps)
    {
        var metadata = FindMatchingAppMetadata(activeApp, apps);
        var family = ClassifyAppFamily(metadata ?? activeApp);
        var effectiveApp = metadata ?? activeApp;
        var publisher = effectiveApp.Publisher ?? "Desconocido";
        var version = effectiveApp.Version ?? "Detectada";
        var windowTitle = string.IsNullOrWhiteSpace(activeApp.WindowTitle) ? "Sin título visible" : activeApp.WindowTitle!;

        return new SyncCategory
        {
            Id = "active_app",
            Name = "ACTIVE APP",
            Color = "#22C55E",
            Icon = "CursorClick",
            Shortcuts =
            [
                new()
                {
                    Id = "APP_ACTIVE_NOW",
                    Label = "APP ACTIVA",
                    Icon = GetFamilyIcon(family),
                    Size = "big",
                    Subtitle = TruncateName(activeApp.Name, 42),
                    Detail = TruncateName(windowTitle, 70),
                    Value = "FOCUSED",
                    ActionType = "status",
                    Stats =
                    [
                        new() { Label = "PID", Value = activeApp.ProcessId?.ToString() ?? "—" },
                        new() { Label = "VERSIÓN", Value = version },
                        new() { Label = "VENDOR", Value = TruncateName(publisher, 18) },
                        new() { Label = "TIPO", Value = GetFamilyDisplayName(family) },
                    ],
                    ProgressValue = 100,
                    SettingsGroups =
                    [
                        new()
                        {
                            Title = "Ventana en foco",
                            Fields =
                            [
                                new() { Type = "info", Key = "window_title", Label = "Título activo", Value = windowTitle },
                                new() { Type = "info", Key = "exe_path", Label = "Ruta ejecutable", Value = activeApp.ExecutablePath },
                                new() { Type = "info", Key = "publisher", Label = "Publisher", Value = publisher },
                                new() { Type = "info", Key = "version", Label = "Versión", Value = version },
                            ]
                        },
                        BuildFamilySettingsGroup(effectiveApp, family),
                    ]
                }
            ]
        };
    }

    private static IEnumerable<SyncCategory> BuildGenericAppLibraryCategories(List<AppInfo> apps)
    {
        var appGroups = apps
            .Where(a => !string.IsNullOrWhiteSpace(a.Name))
            .Where(a => !IsCoveredByDetailedCategory(a))
            .GroupBy(ClassifyAppFamily)
            .OrderBy(g => GetFamilyOrder(g.Key));

        foreach (var group in appGroups)
        {
            var groupApps = group
                .OrderByDescending(a => a.IsRunning)
                .ThenBy(a => a.Name)
                .ToList();

            if (groupApps.Count == 0)
            {
                continue;
            }

            yield return new SyncCategory
            {
                Id = GetFamilyCategoryId(group.Key),
                Name = GetFamilyCategoryName(group.Key),
                Color = GetFamilyColor(group.Key),
                Icon = GetFamilyIcon(group.Key),
                Shortcuts = groupApps.Select(app => BuildGenericAppShortcut(app, group.Key)).ToList()
            };
        }
    }

    private static SyncShortcut BuildGenericAppShortcut(AppInfo app, AppLibraryFamily family)
    {
        var value = app.IsRunning
            ? "OPEN"
            : !string.IsNullOrWhiteSpace(app.Version)
                ? app.Version
                : "INSTALLED";

        var size = app.IsRunning
            ? family is AppLibraryFamily.Browser or AppLibraryFamily.Media ? "wide" : "tall"
            : "small";

        var detail = app.IsRunning
            ? (string.IsNullOrWhiteSpace(app.WindowTitle) ? "Proceso en ejecución" : TruncateName(app.WindowTitle!, 64))
            : !string.IsNullOrWhiteSpace(app.Publisher)
                ? TruncateName(app.Publisher!, 42)
                : "Aplicación detectada";

        var stats = new List<SyncStat>
        {
            new() { Label = "ESTADO", Value = app.IsRunning ? "Running" : "Installed" },
            new() { Label = "VERSIÓN", Value = app.Version ?? "N/D" },
            new() { Label = "VENDOR", Value = string.IsNullOrWhiteSpace(app.Publisher) ? "N/D" : TruncateName(app.Publisher!, 16) },
        };

        if (app.ProcessId.HasValue)
        {
            stats.Add(new() { Label = "PID", Value = app.ProcessId.Value.ToString() });
        }

        var logs = new List<string>();
        if (!string.IsNullOrWhiteSpace(app.WindowTitle))
        {
            logs.Add($"Ventana: {TruncateName(app.WindowTitle!, 48)}");
        }
        if (!string.IsNullOrWhiteSpace(app.ExecutablePath))
        {
            logs.Add($"Exe: {TruncateName(Path.GetFileName(app.ExecutablePath), 40)}");
        }
        if (app.InstallDate.HasValue)
        {
            logs.Add($"Instalada: {app.InstallDate:yyyy-MM-dd}");
        }

        return new SyncShortcut
        {
            Id = $"APP_{app.Id}",
            Label = TruncateName(app.Name, 34).ToUpperInvariant(),
            Icon = GetFamilyIcon(family),
            Size = size,
            Subtitle = !string.IsNullOrWhiteSpace(app.Version) ? app.Version : null,
            Detail = detail,
            Value = value,
            ActionType = "status",
            Stats = stats,
            Logs = logs.Count > 0 ? logs : null,
            SettingsGroups =
            [
                new()
                {
                    Title = "Identidad",
                    Fields =
                    [
                        new() { Type = "info", Key = "app_name", Label = "Nombre", Value = app.Name },
                        new() { Type = "info", Key = "publisher", Label = "Publisher", Value = app.Publisher ?? "Desconocido" },
                        new() { Type = "info", Key = "version", Label = "Versión", Value = app.Version ?? "N/D" },
                        new() { Type = "info", Key = "install_date", Label = "Instalada", Value = app.InstallDate?.ToString("yyyy-MM-dd") ?? "N/D" },
                        new() { Type = "info", Key = "exe_path", Label = "Ruta", Value = string.IsNullOrWhiteSpace(app.ExecutablePath) ? "N/D" : app.ExecutablePath },
                    ]
                },
                BuildFamilySettingsGroup(app, family),
            ]
        };
    }

    private static SyncSettingsGroup BuildFamilySettingsGroup(AppInfo app, AppLibraryFamily family)
    {
        return family switch
        {
            AppLibraryFamily.Browser => new SyncSettingsGroup
            {
                Title = "Navegación",
                Fields =
                [
                    new() { Type = "toggle", Key = "continue_session", Label = "Restaurar sesión", Value = true },
                    new() { Type = "toggle", Key = "tracker_block", Label = "Bloqueo de rastreadores", Value = true },
                    new() { Type = "select", Key = "profile", Label = "Perfil", Value = "Principal", Options = ["Principal", "Trabajo", "Invitado"] },
                ]
            },
            AppLibraryFamily.Productivity => new SyncSettingsGroup
            {
                Title = "Productividad",
                Fields =
                [
                    new() { Type = "toggle", Key = "autosave", Label = "Autoguardado", Value = true },
                    new() { Type = "toggle", Key = "cloud_sync", Label = "Sincronización nube", Value = true },
                    new() { Type = "select", Key = "workspace", Label = "Espacio", Value = "Recientes", Options = ["Recientes", "Trabajo", "Personal"] },
                ]
            },
            AppLibraryFamily.Communication => new SyncSettingsGroup
            {
                Title = "Comunicación",
                Fields =
                [
                    new() { Type = "toggle", Key = "notifications", Label = "Notificaciones", Value = true },
                    new() { Type = "select", Key = "presence", Label = "Estado", Value = "Disponible", Options = ["Disponible", "Ocupado", "Ausente", "No molestar"] },
                    new() { Type = "toggle", Key = "launch_on_boot", Label = "Abrir al iniciar", Value = true },
                ]
            },
            AppLibraryFamily.Media => new SyncSettingsGroup
            {
                Title = "Multimedia",
                Fields =
                [
                    new() { Type = "slider", Key = "volume", Label = "Volumen", Value = 72, Min = 0, Max = 100, Unit = "%" },
                    new() { Type = "select", Key = "quality", Label = "Calidad", Value = "Alta", Options = ["Normal", "Alta", "Muy alta"] },
                    new() { Type = "toggle", Key = "hardware_accel", Label = "Aceleración HW", Value = true },
                ]
            },
            AppLibraryFamily.Creative => new SyncSettingsGroup
            {
                Title = "Creativo",
                Fields =
                [
                    new() { Type = "toggle", Key = "gpu_accel", Label = "Aceleración GPU", Value = true },
                    new() { Type = "toggle", Key = "autosave", Label = "Autoguardado", Value = true },
                    new() { Type = "select", Key = "workspace", Label = "Workspace", Value = "Default", Options = ["Default", "Editing", "Review", "Color"] },
                ]
            },
            AppLibraryFamily.Development => new SyncSettingsGroup
            {
                Title = "Desarrollo",
                Fields =
                [
                    new() { Type = "toggle", Key = "restore_workspace", Label = "Restaurar workspace", Value = true },
                    new() { Type = "select", Key = "terminal", Label = "Terminal", Value = "PowerShell", Options = ["PowerShell", "Cmd", "Git Bash", "WSL"] },
                    new() { Type = "toggle", Key = "auto_update_tools", Label = "Auto update", Value = false },
                ]
            },
            AppLibraryFamily.Utility => new SyncSettingsGroup
            {
                Title = "Utilidad",
                Fields =
                [
                    new() { Type = "toggle", Key = "start_on_boot", Label = "Arrancar con Windows", Value = false },
                    new() { Type = "select", Key = "mode", Label = "Modo", Value = "Normal", Options = ["Normal", "Silencioso", "Avanzado"] },
                    new() { Type = "info", Key = "binary", Label = "Binario", Value = string.IsNullOrWhiteSpace(app.ExecutablePath) ? "N/D" : Path.GetFileName(app.ExecutablePath) },
                ]
            },
            _ => new SyncSettingsGroup
            {
                Title = "General",
                Fields =
                [
                    new() { Type = "toggle", Key = "favorite", Label = "Favorita", Value = false },
                    new() { Type = "toggle", Key = "pin_dashboard", Label = "Fijar en dashboard", Value = false },
                    new() { Type = "info", Key = "family", Label = "Familia", Value = GetFamilyDisplayName(family) },
                ]
            },
        };
    }

    private static AppInfo? FindMatchingAppMetadata(AppInfo app, List<AppInfo> apps)
    {
        return apps.FirstOrDefault(candidate =>
                   !string.IsNullOrWhiteSpace(app.ExecutablePath)
                   && !string.IsNullOrWhiteSpace(candidate.ExecutablePath)
                   && candidate.ExecutablePath.Equals(app.ExecutablePath, StringComparison.OrdinalIgnoreCase))
               ?? apps.FirstOrDefault(candidate =>
                   candidate.Name.Equals(app.Name, StringComparison.OrdinalIgnoreCase));
    }

    private static AppLibraryFamily ClassifyAppFamily(AppInfo app)
    {
        var corpus = string.Join(" | ",
            app.Name,
            app.Publisher ?? string.Empty,
            app.ExecutablePath ?? string.Empty).ToLowerInvariant();

        if (ContainsAny(corpus, "chrome", "firefox", "edge", "brave", "opera", "browser", "vivaldi"))
            return AppLibraryFamily.Browser;
        if (ContainsAny(corpus, "word", "excel", "powerpoint", "office", "onenote", "notion", "acrobat", "reader", "wps", "libreoffice", "todo", "calendar"))
            return AppLibraryFamily.Productivity;
        if (ContainsAny(corpus, "discord", "slack", "teams", "telegram", "whatsapp", "zoom", "meet", "skype", "signal"))
            return AppLibraryFamily.Communication;
        if (ContainsAny(corpus, "spotify", "vlc", "obs", "music", "video", "media", "player", "itunes", "netflix"))
            return AppLibraryFamily.Media;
        if (ContainsAny(corpus, "photoshop", "illustrator", "premiere", "after effects", "figma", "blender", "lightroom", "paint", "canva"))
            return AppLibraryFamily.Creative;
        if (ContainsAny(corpus, "visual studio", "vscode", "git", "docker", "postman", "node", "python", "android studio", "intellij", "pycharm", "webstorm", "rider", "terminal", "powershell", "claude", "insomnia"))
            return AppLibraryFamily.Development;
        if (ContainsAny(corpus, "7-zip", "winrar", "powertoys", "sharex", "explorer", "settings", "calculator", "snipping", "onedrive", "dropbox", "notepad"))
            return AppLibraryFamily.Utility;

        return AppLibraryFamily.Other;
    }

    private static bool IsCoveredByDetailedCategory(AppInfo app)
    {
        var corpus = string.Join(" | ",
            app.Name,
            app.Publisher ?? string.Empty,
            app.ExecutablePath ?? string.Empty).ToLowerInvariant();

        return ContainsAny(corpus,
            "chrome", "firefox", "edge", "brave", "opera", "browser", "vivaldi",
            "microsoft word", "word 2016", "word 2019", "word 2021", "libreoffice writer", "wps writer", "wordpad",
            "microsoft excel", "excel 2016", "excel 2019", "excel 2021", "libreoffice calc", "wps spreadsheet",
            "spotify",
            "visual studio", "vscode", "github desktop", "postman", "docker desktop", "git for windows",
            "node.js", "python 3", "android studio", "jetbrains", "intellij", "webstorm", "pycharm", "rider", "insomnia", "powershell", "windows terminal", "claude");
    }

    private static bool ContainsAny(string value, params string[] fragments)
        => fragments.Any(fragment => value.Contains(fragment, StringComparison.OrdinalIgnoreCase));

    private static int GetFamilyOrder(AppLibraryFamily family) => family switch
    {
        AppLibraryFamily.Communication => 0,
        AppLibraryFamily.Productivity => 1,
        AppLibraryFamily.Media => 2,
        AppLibraryFamily.Creative => 3,
        AppLibraryFamily.Utility => 4,
        AppLibraryFamily.Other => 5,
        _ => 6,
    };

    private static string GetFamilyCategoryId(AppLibraryFamily family) => family switch
    {
        AppLibraryFamily.Browser => "apps_browsers",
        AppLibraryFamily.Productivity => "apps_productivity",
        AppLibraryFamily.Communication => "apps_communication",
        AppLibraryFamily.Media => "apps_media",
        AppLibraryFamily.Creative => "apps_creative",
        AppLibraryFamily.Development => "apps_development",
        AppLibraryFamily.Utility => "apps_utilities",
        _ => "apps_other",
    };

    private static string GetFamilyCategoryName(AppLibraryFamily family) => family switch
    {
        AppLibraryFamily.Browser => "BROWSER LIBRARY",
        AppLibraryFamily.Productivity => "PRODUCTIVITY APPS",
        AppLibraryFamily.Communication => "COMMUNICATION APPS",
        AppLibraryFamily.Media => "MEDIA APPS",
        AppLibraryFamily.Creative => "CREATIVE APPS",
        AppLibraryFamily.Development => "DEVELOPMENT LIBRARY",
        AppLibraryFamily.Utility => "UTILITY APPS",
        _ => "OTHER APPS",
    };

    private static string GetFamilyDisplayName(AppLibraryFamily family) => family switch
    {
        AppLibraryFamily.Browser => "Browser",
        AppLibraryFamily.Productivity => "Productivity",
        AppLibraryFamily.Communication => "Communication",
        AppLibraryFamily.Media => "Media",
        AppLibraryFamily.Creative => "Creative",
        AppLibraryFamily.Development => "Development",
        AppLibraryFamily.Utility => "Utility",
        _ => "Other",
    };

    private static string GetFamilyColor(AppLibraryFamily family) => family switch
    {
        AppLibraryFamily.Browser => "#3B82F6",
        AppLibraryFamily.Productivity => "#2563EB",
        AppLibraryFamily.Communication => "#10B981",
        AppLibraryFamily.Media => "#EC4899",
        AppLibraryFamily.Creative => "#F97316",
        AppLibraryFamily.Development => "#06B6D4",
        AppLibraryFamily.Utility => "#F59E0B",
        _ => "#94A3B8",
    };

    private static string GetFamilyIcon(AppLibraryFamily family) => family switch
    {
        AppLibraryFamily.Browser => "Globe",
        AppLibraryFamily.Productivity => "Briefcase",
        AppLibraryFamily.Communication => "ChatsCircle",
        AppLibraryFamily.Media => "MusicNotes",
        AppLibraryFamily.Creative => "PaintBrush",
        AppLibraryFamily.Development => "Code",
        AppLibraryFamily.Utility => "Wrench",
        _ => "SquaresFour",
    };

    private static SyncCategory BuildAppSettingsCategory(string netType, string netSpeed)
    {
        var apiUrl = GetApiBaseUrl();
        var appVersion = GetDesktopAppVersion();
        var osName = GetOsName();

        return new SyncCategory
        {
            Id = "settings",
            Name = "CONFIGURACIÓN",
            Color = "#8B5CF6",
            Icon = "Gear",
            Shortcuts =
            [
                new()
                {
                    Id = "CFG_API",
                    Label = "CONEXIÓN API",
                    Icon = "WifiHigh",
                    Size = "big",
                    Subtitle = $"Servidor · {apiUrl}",
                    Stats =
                    [
                        new() { Label = "SERVIDOR", Value = apiUrl },
                        new() { Label = "RED", Value = $"{netType} · {netSpeed}" },
                        new() { Label = "PROTOCOLO", Value = "WebSocket + HTTP" },
                        new() { Label = "AUTH", Value = "JWT Bearer" },
                    ],
                    ProgressValue = 0,
                    SettingsGroups =
                    [
                        new()
                        {
                            Title = "Servidor",
                            Fields =
                            [
                                new() { Type = "info", Key = "api_url", Label = "URL del API", Value = apiUrl },
                                new() { Type = "slider", Key = "timeout", Label = "Tiempo de espera", Value = 10, Min = 5, Max = 60, Unit = "s" },
                                new() { Type = "toggle", Key = "auto_reconnect", Label = "Reconexión automática", Value = true },
                            ]
                        },
                        new()
                        {
                            Title = "Seguridad",
                            Fields =
                            [
                                new() { Type = "toggle", Key = "use_https", Label = "Forzar HTTPS", Value = false },
                                new() { Type = "slider", Key = "token_expiry", Label = "Expiración de sesión", Value = 24, Min = 1, Max = 168, Unit = "h" },
                            ]
                        }
                    ]
                },
                new()
                {
                    Id = "CFG_SYNC",
                    Label = "SINCRONIZACIÓN",
                    Icon = "CloudArrowUp",
                    Size = "wide",
                    Detail = "Configuración de envío de datos al móvil",
                    ActionType = "chips",
                    Value = "MANUAL",
                    Options = ["MANUAL", "1 MIN", "5 MIN", "10 MIN"],
                    SettingsGroups =
                    [
                        new()
                        {
                            Title = "Auto-Sync",
                            Fields =
                            [
                                new() { Type = "toggle", Key = "sync_on_start", Label = "Sincronizar al iniciar", Value = true },
                                new() { Type = "toggle", Key = "sync_on_connect", Label = "Sincronizar al conectar", Value = true },
                                new() { Type = "select", Key = "sync_interval", Label = "Intervalo automático", Value = "Manual", Options = ["Manual", "1 minuto", "5 minutos", "10 minutos"] },
                            ]
                        }
                    ]
                },
                new()
                {
                    Id = "CFG_NOTIF",
                    Label = "NOTIFICACIONES",
                    Icon = "BellRinging",
                    Size = "small",
                    Value = true,
                    ActionType = "toggle",
                    SettingsGroups =
                    [
                        new()
                        {
                            Title = "Alertas",
                            Fields =
                            [
                                new() { Type = "toggle", Key = "notif_connect", Label = "Notificar conexión", Value = true },
                                new() { Type = "toggle", Key = "notif_sync", Label = "Notificar sincronización", Value = false },
                                new() { Type = "toggle", Key = "notif_cmd", Label = "Notificar comandos recibidos", Value = true },
                            ]
                        }
                    ]
                },
                new()
                {
                    Id = "CFG_DISPLAY",
                    Label = "APARIENCIA",
                    Icon = "PresentationChart",
                    Size = "small",
                    Value = "DARK",
                    ActionType = "status",
                    SettingsGroups =
                    [
                        new()
                        {
                            Title = "Visual",
                            Fields =
                            [
                                new() { Type = "select", Key = "theme", Label = "Tema", Value = "Oscuro", Options = ["Oscuro", "Claro", "Sistema"] },
                                new() { Type = "toggle", Key = "tray_icon", Label = "Ícono en bandeja del sistema", Value = true },
                                new() { Type = "toggle", Key = "minimize_tray", Label = "Minimizar a bandeja", Value = true },
                            ]
                        }
                    ]
                },
                new()
                {
                    Id = "CFG_INFO",
                    Label = "INFORMACIÓN",
                    Icon = "Info",
                    Size = "small",
                    Value = $"v{appVersion}",
                    ActionType = "status",
                    SettingsGroups =
                    [
                        new()
                        {
                            Title = "Acerca de",
                            Fields =
                            [
                                new() { Type = "info", Key = "version", Label = "Versión de la app", Value = $"v{appVersion}" },
                                new() { Type = "info", Key = "os", Label = "Sistema Operativo", Value = osName },
                                new() { Type = "info", Key = "arch", Label = "Arquitectura", Value = RuntimeInformation.OSArchitecture.ToString() },
                            ]
                        }
                    ]
                },
            ]
        };
    }
}
