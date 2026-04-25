using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using DesktopAssistant.Models;

namespace DesktopAssistant.Services;

internal static class SyncAppCatalog
{
    private sealed record AppCatalogEntry(
        string Id,
        string DisplayName,
        string Color,
        string Icon,
        string[] Aliases,
        string Detail,
        string Kind,
        string QuickActionLabel,
        string QuickActionIcon,
        string QuickActionValue,
        string[] QuickActionOptions,
        SyncStat[] BaseStats,
        string[] Logs,
        SyncSettingsGroup[] SettingsGroups);

    private static readonly AppCatalogEntry[] Catalog =
    [
        // ── TERMINAL ─────────────────────────────────────────────────────────
        // Dashboard: cambiar de shell, limpiar pantalla, ejecutar comandos frecuentes
        new(
            "terminal",
            "Terminal",
            "#94A3B8",
            "Terminal",
            ["windows terminal", "terminal"],
            "Shell activa en el desktop.",
            "Shell",
            "ACCIONES",
            "Terminal",
            "Limpiar",
            ["Limpiar", "Interrumpir", "Nueva pestaña", "Cerrar"],
            [
                new() { Label = "SHELL", Value = "PowerShell" },
                new() { Label = "AREA", Value = "Consola" },
            ],
            [
                "Controla la sesion de terminal activa.",
                "Limpiar, interrumpir proceso o abrir nueva pestaña.",
                "Cambia entre shells sin tocar la PC.",
            ],
            [
                new()
                {
                    Title = "Shell activa",
                    Fields =
                    [
                        new() { Type = "select", Key = "switch_shell", Label = "Cambiar a shell", Value = "PowerShell", Options = ["PowerShell", "CMD", "WSL", "Git Bash"] },
                        new() { Type = "select", Key = "quick_command", Label = "Ejecutar comando", Value = "Ninguno", Options = ["Ninguno", "cls", "cd ~", "npm run dev", "git status", "docker ps"] },
                    ]
                },
                new()
                {
                    Title = "Proceso activo",
                    Fields =
                    [
                        new() { Type = "toggle", Key = "kill_process", Label = "Matar proceso (Ctrl+C)", Value = false },
                        new() { Type = "toggle", Key = "new_tab", Label = "Abrir nueva pestaña", Value = false },
                        new() { Type = "toggle", Key = "split_pane", Label = "Dividir panel", Value = false },
                    ]
                },
            ]),

        // ── VS CODE ──────────────────────────────────────────────────────────
        // Dashboard: cambiar workspace/rama, abrir terminal, ejecutar task
        new(
            "vs_code_insiders",
            "VS Code Insiders",
            "#14B8A6",
            "Code",
            ["visual studio code insiders", "visual studio code insiders edition", "visual studio code insiders build", "visual studio code insiders exe", "visual studio code - insiders", "vs code insiders", "code insiders", "insiders"],
            "Editor Insiders activo en el desktop.",
            "Editor",
            "ACCIONES",
            "Code",
            "Terminal",
            ["Terminal", "Git pull", "Run task", "Comando"],
            [
                new() { Label = "CANAL", Value = "Insiders" },
                new() { Label = "AREA", Value = "Editor" },
            ],
            [
                "Controla el editor sin tocar la PC.",
                "Ejecuta tasks, abre terminal o cambia rama.",
                "Acciones frecuentes de desarrollo remoto.",
            ],
            [
                new()
                {
                    Title = "Git",
                    Fields =
                    [
                        new() { Type = "select", Key = "git_action", Label = "Accion Git", Value = "Ninguna", Options = ["Ninguna", "git pull", "git push", "git stash", "git status", "git fetch"] },
                        new() { Type = "toggle", Key = "open_source_control", Label = "Abrir Source Control", Value = false },
                    ]
                },
                new()
                {
                    Title = "Workspace",
                    Fields =
                    [
                        new() { Type = "select", Key = "run_task", Label = "Ejecutar task", Value = "Ninguna", Options = ["Ninguna", "build", "test", "watch", "lint", "start"] },
                        new() { Type = "toggle", Key = "open_terminal", Label = "Abrir terminal integrada", Value = false },
                        new() { Type = "toggle", Key = "toggle_sidebar", Label = "Alternar sidebar", Value = false },
                    ]
                },
            ]),

        new(
            "vs_code",
            "VS Code",
            "#22A5F7",
            "Code",
            ["visual studio code", "vscode", "vs code"],
            "Editor principal activo en el desktop.",
            "Editor",
            "ACCIONES",
            "Code",
            "Terminal",
            ["Terminal", "Git pull", "Run task", "Comando"],
            [
                new() { Label = "CANAL", Value = "Stable" },
                new() { Label = "AREA", Value = "Editor" },
            ],
            [
                "Controla el editor sin tocar la PC.",
                "Ejecuta tasks, abre terminal o cambia rama.",
                "Acciones frecuentes de desarrollo remoto.",
            ],
            [
                new()
                {
                    Title = "Git",
                    Fields =
                    [
                        new() { Type = "select", Key = "git_action", Label = "Accion Git", Value = "Ninguna", Options = ["Ninguna", "git pull", "git push", "git stash", "git status", "git fetch"] },
                        new() { Type = "toggle", Key = "open_source_control", Label = "Abrir Source Control", Value = false },
                    ]
                },
                new()
                {
                    Title = "Workspace",
                    Fields =
                    [
                        new() { Type = "select", Key = "run_task", Label = "Ejecutar task", Value = "Ninguna", Options = ["Ninguna", "build", "test", "watch", "lint", "start"] },
                        new() { Type = "toggle", Key = "open_terminal", Label = "Abrir terminal integrada", Value = false },
                        new() { Type = "toggle", Key = "toggle_sidebar", Label = "Alternar sidebar", Value = false },
                    ]
                },
            ]),

        // ── ANTIGRAVITY ──────────────────────────────────────────────────────
        // Dashboard: cambiar perfil activo, ajustar sensibilidad en vivo, activar/pausar
        new(
            "antigravity",
            "Antigravity",
            "#8B5CF6",
            "Target",
            ["antigravity"],
            "Control de movimiento activo en el desktop.",
            "Experimental",
            "ACCIONES",
            "Target",
            "Pausar",
            ["Pausar", "Reanudar", "Recalibrar", "Resetear"],
            [
                new() { Label = "PERFIL", Value = "Precision" },
                new() { Label = "AREA", Value = "Control" },
            ],
            [
                "Pausa, reanuda o recalibra sin tocar la PC.",
                "Cambia perfil de movimiento al vuelo.",
                "Ajusta sensibilidad en tiempo real.",
            ],
            [
                new()
                {
                    Title = "Control activo",
                    Fields =
                    [
                        new() { Type = "toggle", Key = "pause_resume", Label = "Pausar / Reanudar", Value = false },
                        new() { Type = "toggle", Key = "recalibrate", Label = "Recalibrar ahora", Value = false },
                        new() { Type = "select", Key = "switch_profile", Label = "Cambiar perfil", Value = "Precision", Options = ["Precision", "Orbit", "Gaming", "Sleep"] },
                    ]
                },
                new()
                {
                    Title = "Ajuste en vivo",
                    Fields =
                    [
                        new() { Type = "slider", Key = "sensitivity", Label = "Sensibilidad", Value = 72, Min = 10, Max = 100, Unit = "%" },
                        new() { Type = "toggle", Key = "smoothing", Label = "Suavizado", Value = true },
                    ]
                },
            ]),

        // ── CLAUDE ───────────────────────────────────────────────────────────
        // Dashboard: nueva conversacion, cambiar modelo, abrir proyecto especifico
        new(
            "claude",
            "Claude",
            "#D97706",
            "Sparkle",
            ["claude", "claude ai", "anthropic claude"],
            "Cliente Claude activo en el desktop.",
            "IA",
            "ACCIONES",
            "Sparkle",
            "Nueva chat",
            ["Nueva chat", "Nuevo proyecto", "Limpiar contexto", "Copiar respuesta"],
            [
                new() { Label = "MODELO", Value = "Sonnet" },
                new() { Label = "PROVEEDOR", Value = "Anthropic" },
            ],
            [
                "Inicia una nueva conversacion o proyecto.",
                "Cambia modelo sin interrumpir el flujo.",
                "Acceso rapido a funciones frecuentes.",
            ],
            [
                new()
                {
                    Title = "Conversacion",
                    Fields =
                    [
                        new() { Type = "toggle", Key = "new_chat", Label = "Nueva conversacion", Value = false },
                        new() { Type = "select", Key = "switch_model", Label = "Cambiar modelo", Value = "Sonnet", Options = ["Haiku", "Sonnet", "Opus"] },
                        new() { Type = "toggle", Key = "extended_thinking", Label = "Activar razonamiento extendido", Value = false },
                    ]
                },
                new()
                {
                    Title = "Proyecto activo",
                    Fields =
                    [
                        new() { Type = "toggle", Key = "new_project", Label = "Nuevo proyecto", Value = false },
                        new() { Type = "toggle", Key = "open_artifacts", Label = "Ver Artifacts", Value = false },
                    ]
                },
            ]),

        // ── CHATGPT ──────────────────────────────────────────────────────────
        // Dashboard: nueva chat, cambiar modelo, activar/desactivar busqueda web
        new(
            "chatgpt",
            "ChatGPT",
            "#10A37F",
            "Brain",
            ["chatgpt", "chat gpt", "openai"],
            "Cliente ChatGPT activo en el desktop.",
            "IA",
            "ACCIONES",
            "Brain",
            "Nueva chat",
            ["Nueva chat", "Subir imagen", "Busqueda web", "Copiar respuesta"],
            [
                new() { Label = "MODELO", Value = "GPT-4o" },
                new() { Label = "PROVEEDOR", Value = "OpenAI" },
            ],
            [
                "Inicia nueva conversacion o cambia modelo.",
                "Activa busqueda web o analisis de imagen.",
                "Acciones utiles sin tocar la PC.",
            ],
            [
                new()
                {
                    Title = "Conversacion",
                    Fields =
                    [
                        new() { Type = "toggle", Key = "new_chat", Label = "Nueva conversacion", Value = false },
                        new() { Type = "select", Key = "switch_model", Label = "Cambiar modelo", Value = "GPT-4o", Options = ["GPT-4o", "GPT-4", "o1", "GPT-3.5"] },
                        new() { Type = "toggle", Key = "web_search", Label = "Activar busqueda web", Value = false },
                    ]
                },
                new()
                {
                    Title = "Herramientas",
                    Fields =
                    [
                        new() { Type = "toggle", Key = "upload_image", Label = "Subir imagen", Value = false },
                        new() { Type = "toggle", Key = "memory_toggle", Label = "Activar memoria", Value = true },
                    ]
                },
            ]),

        // ── CODEX ────────────────────────────────────────────────────────────
        // Dashboard: cambiar modo agente, aprobar/rechazar cambio pendiente, ejecutar
        new(
            "codex",
            "Codex",
            "#6366F1",
            "Code",
            ["codex", "openai codex"],
            "Agente de codigo activo en el desktop.",
            "IA",
            "ACCIONES",
            "Code",
            "Aprobar",
            ["Aprobar cambio", "Rechazar", "Ejecutar", "Cancelar"],
            [
                new() { Label = "MODO", Value = "Ask" },
                new() { Label = "AREA", Value = "IA" },
            ],
            [
                "Aprueba o rechaza cambios propuestos por el agente.",
                "Cambia modo entre Ask, Edit y Generate.",
                "Controla la ejecucion sin tocar la PC.",
            ],
            [
                new()
                {
                    Title = "Agente",
                    Fields =
                    [
                        new() { Type = "select", Key = "switch_mode", Label = "Modo agente", Value = "Ask", Options = ["Ask", "Edit", "Generate", "Debug"] },
                        new() { Type = "toggle", Key = "approve_change", Label = "Aprobar cambio pendiente", Value = false },
                        new() { Type = "toggle", Key = "reject_change", Label = "Rechazar cambio", Value = false },
                    ]
                },
                new()
                {
                    Title = "Ejecucion",
                    Fields =
                    [
                        new() { Type = "toggle", Key = "run_agent", Label = "Ejecutar agente", Value = false },
                        new() { Type = "toggle", Key = "cancel_agent", Label = "Cancelar ejecucion", Value = false },
                        new() { Type = "toggle", Key = "repo_context", Label = "Incluir contexto de repo", Value = true },
                    ]
                },
            ]),

        // ── POSTMAN ──────────────────────────────────────────────────────────
        // Dashboard: cambiar ambiente, correr coleccion, cancelar request en curso
        new(
            "postman",
            "Postman",
            "#F97316",
            "Package",
            ["postman"],
            "Cliente de API activo en el desktop.",
            "API",
            "ACCIONES",
            "Package",
            "Enviar",
            ["Enviar request", "Cancelar", "Correr coleccion", "Limpiar cookies"],
            [
                new() { Label = "AMBIENTE", Value = "Local" },
                new() { Label = "AREA", Value = "Testing" },
            ],
            [
                "Cambia ambiente activo sin buscar en menus.",
                "Corre o cancela colecciones al vuelo.",
                "Limpia cookies o historial de requests.",
            ],
            [
                new()
                {
                    Title = "Ambiente",
                    Fields =
                    [
                        new() { Type = "select", Key = "switch_env", Label = "Cambiar ambiente", Value = "Local", Options = ["Local", "Dev", "QA", "Staging", "Prod"] },
                        new() { Type = "toggle", Key = "send_request", Label = "Enviar request activo", Value = false },
                        new() { Type = "toggle", Key = "cancel_request", Label = "Cancelar request", Value = false },
                    ]
                },
                new()
                {
                    Title = "Coleccion",
                    Fields =
                    [
                        new() { Type = "toggle", Key = "run_collection", Label = "Correr coleccion", Value = false },
                        new() { Type = "toggle", Key = "clear_cookies", Label = "Limpiar cookies", Value = false },
                        new() { Type = "toggle", Key = "clear_history", Label = "Limpiar historial", Value = false },
                    ]
                },
            ]),

        // ── EXPLORADOR DE ARCHIVOS ────────────────────────────────────────────
        // Dashboard: ir a carpeta frecuente, mostrar/ocultar archivos ocultos, nueva carpeta
        new(
            "explorador_archivos",
            "Explorador de archivos",
            "#EAB308",
            "Folder",
            ["explorador de archivos", "file explorer", "explorer"],
            "Explorador activo en el desktop.",
            "Archivos",
            "NAVEGAR",
            "Folder",
            "Descargas",
            ["Descargas", "Documentos", "Escritorio", "Proyecto actual"],
            [
                new() { Label = "AREA", Value = "Local" },
                new() { Label = "PERFIL", Value = "Windows" },
            ],
            [
                "Navega a carpetas frecuentes sin tocar la PC.",
                "Muestra u oculta archivos del sistema al vuelo.",
                "Abre una nueva ventana del explorador.",
            ],
            [
                new()
                {
                    Title = "Navegar a",
                    Fields =
                    [
                        new() { Type = "select", Key = "go_to_folder", Label = "Ir a carpeta", Value = "Inicio", Options = ["Inicio", "Escritorio", "Descargas", "Documentos", "Imagenes", "Videos"] },
                        new() { Type = "toggle", Key = "new_window", Label = "Abrir nueva ventana", Value = false },
                        new() { Type = "toggle", Key = "new_folder", Label = "Nueva carpeta aqui", Value = false },
                    ]
                },
                new()
                {
                    Title = "Vista",
                    Fields =
                    [
                        new() { Type = "toggle", Key = "show_hidden", Label = "Mostrar archivos ocultos", Value = false },
                        new() { Type = "select", Key = "view_mode", Label = "Modo de vista", Value = "Detalles", Options = ["Iconos", "Lista", "Detalles", "Mosaico"] },
                    ]
                },
            ]),

        // ── SPOTIFY ──────────────────────────────────────────────────────────
        // Dashboard: control de reproduccion real, volumen, dispositivo activo
        new(
            "spotify",
            "Spotify",
            "#1DB954",
            "MusicNotes",
            ["spotify"],
            "Reproduccion activa en el desktop.",
            "Musica",
            "REPRODUCCION",
            "MusicNotes",
            "Play/Pause",
            ["Play/Pause", "Siguiente", "Anterior", "Silenciar"],
            [
                new() { Label = "AREA", Value = "Streaming" },
                new() { Label = "PERFIL", Value = "Audio" },
            ],
            [
                "Controla reproduccion sin desbloquear la PC.",
                "Ajusta volumen o cambia dispositivo de salida.",
                "Activa modo shuffle o repeat al vuelo.",
            ],
            [
                new()
                {
                    Title = "Reproduccion",
                    Fields =
                    [
                        new() { Type = "toggle", Key = "play_pause", Label = "Play / Pause", Value = false },
                        new() { Type = "toggle", Key = "next_track", Label = "Siguiente cancion", Value = false },
                        new() { Type = "toggle", Key = "prev_track", Label = "Cancion anterior", Value = false },
                        new() { Type = "toggle", Key = "shuffle", Label = "Shuffle", Value = false },
                        new() { Type = "toggle", Key = "repeat", Label = "Repeat", Value = false },
                    ]
                },
                new()
                {
                    Title = "Audio",
                    Fields =
                    [
                        new() { Type = "slider", Key = "volume", Label = "Volumen", Value = 68, Min = 0, Max = 100, Unit = "%" },
                        new() { Type = "toggle", Key = "mute", Label = "Silenciar", Value = false },
                        new() { Type = "select", Key = "output_device", Label = "Dispositivo de salida", Value = "Desktop", Options = ["Desktop", "Auriculares", "Bluetooth", "TV"] },
                    ]
                },
            ]),

        // ── BRAVE ────────────────────────────────────────────────────────────
        // Dashboard: abrir URL, nueva pestaña, activar/desactivar shields, cambiar perfil
        new(
            "brave",
            "Brave",
            "#FB542B",
            "ShieldCheck",
            ["brave", "brave browser"],
            "Navegador Brave activo en el desktop.",
            "Navegador",
            "ACCIONES",
            "ShieldCheck",
            "Nueva pestaña",
            ["Nueva pestaña", "Incognito", "Shields ON", "Shields OFF"],
            [
                new() { Label = "MOTOR", Value = "Chromium" },
                new() { Label = "PERFIL", Value = "Privacidad" },
            ],
            [
                "Abre pestanas, activa shields o cambia perfil.",
                "Navega a URL frecuente sin tocar la PC.",
                "Control rapido de privacidad desde el movil.",
            ],
            [
                new()
                {
                    Title = "Pestanas",
                    Fields =
                    [
                        new() { Type = "toggle", Key = "new_tab", Label = "Nueva pestaña", Value = false },
                        new() { Type = "toggle", Key = "new_incognito", Label = "Nueva ventana incognito", Value = false },
                        new() { Type = "toggle", Key = "close_current_tab", Label = "Cerrar pestaña activa", Value = false },
                        new() { Type = "select", Key = "go_to_url", Label = "Abrir URL frecuente", Value = "Ninguna", Options = ["Ninguna", "localhost:3000", "localhost:8000", "github.com", "vercel.com"] },
                    ]
                },
                new()
                {
                    Title = "Privacidad",
                    Fields =
                    [
                        new() { Type = "toggle", Key = "shields_toggle", Label = "Alternar Shields", Value = true },
                        new() { Type = "select", Key = "switch_profile", Label = "Cambiar perfil", Value = "Principal", Options = ["Principal", "Trabajo", "Personal", "Invitado"] },
                    ]
                },
            ]),

        // ── RESTO DEL CATALOGO (sin cambios de logica, mejorados en acciones) ──

        new(
            "configuracion",
            "Configuración",
            "#3B82F6",
            "Gear",
            ["configuracion", "configuracion de windows", "settings", "windows settings", "systemsettings"],
            "Panel de ajustes del sistema en el desktop.",
            "Sistema",
            "ACCIONES",
            "Gear",
            "Pantalla",
            ["Pantalla", "Bluetooth", "Wi-Fi", "Windows Update"],
            [
                new() { Label = "AREA", Value = "Windows" },
                new() { Label = "PERFIL", Value = "Sistema" },
            ],
            [
                "Abre secciones de configuracion directamente.",
                "Activa o desactiva bluetooth y wifi al vuelo.",
                "Acceso rapido a Windows Update desde el movil.",
            ],
            [
                new()
                {
                    Title = "Conectividad",
                    Fields =
                    [
                        new() { Type = "toggle", Key = "bluetooth", Label = "Bluetooth", Value = true },
                        new() { Type = "toggle", Key = "wifi", Label = "Wi-Fi", Value = true },
                        new() { Type = "toggle", Key = "airplane_mode", Label = "Modo avion", Value = false },
                    ]
                },
                new()
                {
                    Title = "Ir a seccion",
                    Fields =
                    [
                        new() { Type = "select", Key = "open_section", Label = "Abrir seccion", Value = "Ninguna", Options = ["Ninguna", "Pantalla", "Sonido", "Notificaciones", "Almacenamiento", "Windows Update"] },
                        new() { Type = "toggle", Key = "night_light", Label = "Luz nocturna", Value = false },
                    ]
                },
            ]),

        new(
            "arduino_ide",
            "Arduino IDE",
            "#0EA5A4",
            "Wrench",
            ["arduino ide", "arduinoide", "arduino"],
            "IDE de Arduino activo en el desktop.",
            "Embedded",
            "ACCIONES",
            "Wrench",
            "Verificar",
            ["Verificar", "Subir", "Monitor serial", "Detener"],
            [
                new() { Label = "AREA", Value = "Maker" },
                new() { Label = "PLACA", Value = "Arduino" },
            ],
            [
                "Verifica o sube el sketch sin tocar la PC.",
                "Abre el monitor serial al vuelo.",
                "Cambia puerto o placa desde el movil.",
            ],
            [
                new()
                {
                    Title = "Sketch",
                    Fields =
                    [
                        new() { Type = "toggle", Key = "verify", Label = "Verificar sketch", Value = false },
                        new() { Type = "toggle", Key = "upload", Label = "Subir a placa", Value = false },
                        new() { Type = "toggle", Key = "open_serial", Label = "Abrir monitor serial", Value = false },
                        new() { Type = "toggle", Key = "stop", Label = "Detener subida", Value = false },
                    ]
                },
                new()
                {
                    Title = "Placa y puerto",
                    Fields =
                    [
                        new() { Type = "select", Key = "board", Label = "Placa activa", Value = "Arduino Uno", Options = ["Arduino Uno", "Nano", "ESP32", "Mega"] },
                        new() { Type = "select", Key = "port", Label = "Puerto", Value = "COM3", Options = ["COM3", "COM4", "COM5", "COM6"] },
                    ]
                },
            ]),

        new(
            "mongodb_compass",
            "MongoDB Compass",
            "#10B981",
            "Database",
            ["mongodb compass", "compass"],
            "Cliente MongoDB activo en el desktop.",
            "Datos",
            "ACCIONES",
            "Database",
            "Conectar",
            ["Conectar", "Desconectar", "Refrescar", "Nueva query"],
            [
                new() { Label = "CLUSTER", Value = "Local" },
                new() { Label = "AREA", Value = "MongoDB" },
            ],
            [
                "Conecta o desconecta del cluster activo.",
                "Refresca coleccion o ejecuta nueva query.",
                "Cambia entre ambientes local, dev y prod.",
            ],
            [
                new()
                {
                    Title = "Conexion",
                    Fields =
                    [
                        new() { Type = "select", Key = "switch_cluster", Label = "Cambiar cluster", Value = "Local", Options = ["Local", "Dev", "Staging", "Prod"] },
                        new() { Type = "toggle", Key = "connect", Label = "Conectar", Value = false },
                        new() { Type = "toggle", Key = "disconnect", Label = "Desconectar", Value = false },
                    ]
                },
                new()
                {
                    Title = "Coleccion activa",
                    Fields =
                    [
                        new() { Type = "toggle", Key = "refresh", Label = "Refrescar documentos", Value = false },
                        new() { Type = "toggle", Key = "new_query", Label = "Nueva query", Value = false },
                        new() { Type = "toggle", Key = "export_data", Label = "Exportar resultados", Value = false },
                    ]
                },
            ]),

        new(
            "powertoys_preview",
            "PowerToys (Preview)",
            "#F59E0B",
            "Wrench",
            ["powertoys preview", "powertoys", "microsoft powertoys"],
            "Utilidades PowerToys activas en el desktop.",
            "Utilidad",
            "ACCIONES",
            "Wrench",
            "FancyZones",
            ["FancyZones", "Awake ON", "Awake OFF", "Color picker"],
            [
                new() { Label = "AREA", Value = "Windows" },
                new() { Label = "PERFIL", Value = "Power user" },
            ],
            [
                "Activa o desactiva modulos clave al vuelo.",
                "Pon el PC en modo Awake sin buscarlo.",
                "Lanza el color picker o el editor de zonas.",
            ],
            [
                new()
                {
                    Title = "Modulos rapidos",
                    Fields =
                    [
                        new() { Type = "toggle", Key = "awake", Label = "Awake (mantener despierto)", Value = false },
                        new() { Type = "toggle", Key = "fancyzones_editor", Label = "Abrir editor FancyZones", Value = false },
                        new() { Type = "toggle", Key = "color_picker", Label = "Lanzar Color Picker", Value = false },
                        new() { Type = "toggle", Key = "power_run", Label = "Abrir PowerToys Run", Value = false },
                    ]
                },
            ]),

        new(
            "bloc_notas",
            "Bloc de notas",
            "#38BDF8",
            "FileDoc",
            ["bloc de notas", "notepad"],
            "Editor de texto activo en el desktop.",
            "Texto",
            "ACCIONES",
            "FileDoc",
            "Nuevo",
            ["Nuevo archivo", "Guardar", "Buscar", "Reemplazar"],
            [
                new() { Label = "AREA", Value = "Notas" },
                new() { Label = "PERFIL", Value = "Ligero" },
            ],
            [
                "Crea un nuevo archivo o guarda el actual.",
                "Lanza busqueda o reemplazar sin tocar la PC.",
                "Acciones de edicion rapidas desde el movil.",
            ],
            [
                new()
                {
                    Title = "Archivo",
                    Fields =
                    [
                        new() { Type = "toggle", Key = "new_file", Label = "Nuevo archivo", Value = false },
                        new() { Type = "toggle", Key = "save_file", Label = "Guardar (Ctrl+S)", Value = false },
                        new() { Type = "toggle", Key = "save_as", Label = "Guardar como...", Value = false },
                    ]
                },
                new()
                {
                    Title = "Edicion",
                    Fields =
                    [
                        new() { Type = "toggle", Key = "find", Label = "Buscar (Ctrl+F)", Value = false },
                        new() { Type = "toggle", Key = "replace", Label = "Reemplazar (Ctrl+H)", Value = false },
                        new() { Type = "toggle", Key = "select_all", Label = "Seleccionar todo", Value = false },
                    ]
                },
            ]),

        new(
            "google_chrome",
            "Google Chrome",
            "#4285F4",
            "Globe",
            ["google chrome", "chrome"],
            "Navegador Chrome activo en el desktop.",
            "Navegador",
            "ACCIONES",
            "Globe",
            "Nueva pestaña",
            ["Nueva pestaña", "Incognito", "Recargar", "Cerrar pestaña"],
            [
                new() { Label = "MOTOR", Value = "Chromium" },
                new() { Label = "PERFIL", Value = "Sincronizado" },
            ],
            [
                "Abre pestanas o navega a URLs frecuentes.",
                "Cambia perfil de Chrome al vuelo.",
                "Recarga o cierra la pestaña activa.",
            ],
            [
                new()
                {
                    Title = "Pestanas",
                    Fields =
                    [
                        new() { Type = "toggle", Key = "new_tab", Label = "Nueva pestaña", Value = false },
                        new() { Type = "toggle", Key = "new_incognito", Label = "Nueva ventana incognito", Value = false },
                        new() { Type = "toggle", Key = "reload", Label = "Recargar pestaña", Value = false },
                        new() { Type = "toggle", Key = "close_tab", Label = "Cerrar pestaña activa", Value = false },
                    ]
                },
                new()
                {
                    Title = "Perfil",
                    Fields =
                    [
                        new() { Type = "select", Key = "switch_profile", Label = "Cambiar perfil", Value = "Principal", Options = ["Principal", "Trabajo", "Personal", "Invitado"] },
                        new() { Type = "select", Key = "go_to_url", Label = "Abrir URL frecuente", Value = "Ninguna", Options = ["Ninguna", "localhost:3000", "localhost:8000", "github.com", "gmail.com"] },
                    ]
                },
            ]),

        new(
            "edge",
            "Microsoft Edge",
            "#0078D7",
            "Globe",
            ["microsoft edge", "edge", "msedge"],
            "Navegador Edge activo en el desktop.",
            "Navegador",
            "ACCIONES",
            "Globe",
            "Nueva pestaña",
            ["Nueva pestaña", "Copilot", "Lector", "Cerrar pestaña"],
            [
                new() { Label = "MOTOR", Value = "Chromium" },
                new() { Label = "PERFIL", Value = "Microsoft" },
            ],
            [
                "Abre Copilot o modo lectura inmersiva.",
                "Navega a URLs frecuentes desde el movil.",
                "Cambia perfil o abre nueva pestaña.",
            ],
            [
                new()
                {
                    Title = "Pestanas",
                    Fields =
                    [
                        new() { Type = "toggle", Key = "new_tab", Label = "Nueva pestaña", Value = false },
                        new() { Type = "toggle", Key = "open_copilot", Label = "Abrir Copilot sidebar", Value = false },
                        new() { Type = "toggle", Key = "immersive_reader", Label = "Modo lectura inmersiva", Value = false },
                        new() { Type = "toggle", Key = "close_tab", Label = "Cerrar pestaña activa", Value = false },
                    ]
                },
                new()
                {
                    Title = "Perfil",
                    Fields =
                    [
                        new() { Type = "select", Key = "switch_profile", Label = "Cambiar perfil", Value = "Principal", Options = ["Principal", "Trabajo", "Invitado"] },
                        new() { Type = "select", Key = "go_to_url", Label = "Abrir URL frecuente", Value = "Ninguna", Options = ["Ninguna", "localhost:3000", "outlook.com", "github.com"] },
                    ]
                },
            ]),

        new(
            "whatsapp",
            "WhatsApp",
            "#22C55E",
            "ChatsCircle",
            ["whatsapp"],
            "WhatsApp activo en el desktop.",
            "Comunicacion",
            "ACCIONES",
            "ChatsCircle",
            "Nuevo chat",
            ["Nuevo chat", "Silenciar", "Marcar leido", "Archivar"],
            [
                new() { Label = "AREA", Value = "Chat" },
                new() { Label = "PERFIL", Value = "Escritorio" },
            ],
            [
                "Inicia un nuevo chat o marca conversaciones.",
                "Silencia notificaciones desde el movil.",
                "Archiva chats sin abrir la app.",
            ],
            [
                new()
                {
                    Title = "Conversaciones",
                    Fields =
                    [
                        new() { Type = "toggle", Key = "new_chat", Label = "Nuevo chat", Value = false },
                        new() { Type = "toggle", Key = "mark_all_read", Label = "Marcar todo como leido", Value = false },
                        new() { Type = "toggle", Key = "mute_notifications", Label = "Silenciar notificaciones", Value = false },
                    ]
                },
                new()
                {
                    Title = "Estado",
                    Fields =
                    [
                        new() { Type = "select", Key = "presence", Label = "Estado de presencia", Value = "Disponible", Options = ["Disponible", "Ocupado", "Ausente"] },
                    ]
                },
            ]),

        new(
            "discord",
            "Discord",
            "#5865F2",
            "ChatTeardrop",
            ["discord"],
            "Discord activo en el desktop.",
            "Comunicacion",
            "ACCIONES",
            "ChatTeardrop",
            "Silenciar",
            ["Silenciar mic", "Deafen", "Desconectar", "Cambiar estado"],
            [
                new() { Label = "AREA", Value = "Gaming" },
                new() { Label = "PERFIL", Value = "Comunidad" },
            ],
            [
                "Silencia mic o desconecta de canal de voz.",
                "Cambia estado sin abrir Discord.",
                "Activa modo streamer al vuelo.",
            ],
            [
                new()
                {
                    Title = "Voz",
                    Fields =
                    [
                        new() { Type = "toggle", Key = "mute_mic", Label = "Silenciar microfono", Value = false },
                        new() { Type = "toggle", Key = "deafen", Label = "Deafen (silenciar audio)", Value = false },
                        new() { Type = "toggle", Key = "disconnect_voice", Label = "Desconectar de canal", Value = false },
                    ]
                },
                new()
                {
                    Title = "Estado",
                    Fields =
                    [
                        new() { Type = "select", Key = "status", Label = "Cambiar estado", Value = "En linea", Options = ["En linea", "Inactivo", "No molestar", "Invisible"] },
                        new() { Type = "toggle", Key = "streamer_mode", Label = "Activar modo streamer", Value = false },
                    ]
                },
            ]),

        new(
            "telegram",
            "Telegram",
            "#2AABEE",
            "PaperPlaneTilt",
            ["telegram", "telegram desktop"],
            "Telegram activo en el desktop.",
            "Comunicacion",
            "ACCIONES",
            "PaperPlaneTilt",
            "Nuevo mensaje",
            ["Nuevo mensaje", "Marcar leido", "Silenciar", "Archivos"],
            [
                new() { Label = "AREA", Value = "Chat" },
                new() { Label = "PERFIL", Value = "Seguro" },
            ],
            [
                "Abre nuevo mensaje o silencia chats.",
                "Marca todo como leido sin tocar la PC.",
                "Acceso rapido a archivos recibidos.",
            ],
            [
                new()
                {
                    Title = "Mensajes",
                    Fields =
                    [
                        new() { Type = "toggle", Key = "new_message", Label = "Nuevo mensaje", Value = false },
                        new() { Type = "toggle", Key = "mark_all_read", Label = "Marcar todo como leido", Value = false },
                        new() { Type = "toggle", Key = "mute_notifications", Label = "Silenciar notificaciones", Value = false },
                    ]
                },
            ]),

        new(
            "notion",
            "Notion",
            "#FFFFFF",
            "NotePencil",
            ["notion"],
            "Notion activo en el desktop.",
            "Productividad",
            "ACCIONES",
            "NotePencil",
            "Nueva pagina",
            ["Nueva pagina", "Buscar", "Inbox", "Ultima pagina"],
            [
                new() { Label = "AREA", Value = "Notas" },
                new() { Label = "PERFIL", Value = "All-in-one" },
            ],
            [
                "Crea nueva pagina o abre el inbox.",
                "Busca entre tus notas sin tocar la PC.",
                "Navega a la ultima pagina visitada.",
            ],
            [
                new()
                {
                    Title = "Navegacion",
                    Fields =
                    [
                        new() { Type = "toggle", Key = "new_page", Label = "Nueva pagina", Value = false },
                        new() { Type = "toggle", Key = "open_search", Label = "Abrir busqueda (Ctrl+P)", Value = false },
                        new() { Type = "toggle", Key = "open_inbox", Label = "Abrir Inbox", Value = false },
                        new() { Type = "toggle", Key = "go_back", Label = "Pagina anterior", Value = false },
                    ]
                },
            ]),

        new(
            "obsidian",
            "Obsidian",
            "#7C3AED",
            "Diamond",
            ["obsidian"],
            "Obsidian activo en el desktop.",
            "Productividad",
            "ACCIONES",
            "Diamond",
            "Nueva nota",
            ["Nueva nota", "Buscar", "Grafo", "Comando"],
            [
                new() { Label = "AREA", Value = "PKM" },
                new() { Label = "PERFIL", Value = "Markdown" },
            ],
            [
                "Crea nueva nota o abre el buscador.",
                "Abre el grafo de conocimiento al vuelo.",
                "Lanza la paleta de comandos sin tocar el teclado.",
            ],
            [
                new()
                {
                    Title = "Notas",
                    Fields =
                    [
                        new() { Type = "toggle", Key = "new_note", Label = "Nueva nota", Value = false },
                        new() { Type = "toggle", Key = "quick_switcher", Label = "Abrir buscador (Ctrl+O)", Value = false },
                        new() { Type = "toggle", Key = "open_graph", Label = "Abrir vista de grafo", Value = false },
                        new() { Type = "toggle", Key = "command_palette", Label = "Paleta de comandos", Value = false },
                    ]
                },
            ]),

        new(
            "word",
            "Microsoft Word",
            "#2B579A",
            "FileDoc",
            ["microsoft word", "word", "winword"],
            "Word activo en el desktop.",
            "Ofimática",
            "ACCIONES",
            "FileDoc",
            "Guardar",
            ["Guardar", "Nuevo doc", "Buscar", "Exportar PDF"],
            [
                new() { Label = "SUITE", Value = "Microsoft 365" },
                new() { Label = "AREA", Value = "Office" },
            ],
            [
                "Guarda o exporta el documento activo.",
                "Abre buscar y reemplazar sin tocar la PC.",
                "Crea nuevo documento al vuelo.",
            ],
            [
                new()
                {
                    Title = "Documento activo",
                    Fields =
                    [
                        new() { Type = "toggle", Key = "save", Label = "Guardar (Ctrl+S)", Value = false },
                        new() { Type = "toggle", Key = "save_as_pdf", Label = "Exportar como PDF", Value = false },
                        new() { Type = "toggle", Key = "new_document", Label = "Nuevo documento", Value = false },
                    ]
                },
                new()
                {
                    Title = "Edicion",
                    Fields =
                    [
                        new() { Type = "toggle", Key = "find_replace", Label = "Buscar y reemplazar", Value = false },
                        new() { Type = "toggle", Key = "track_changes", Label = "Activar control de cambios", Value = false },
                        new() { Type = "toggle", Key = "word_count", Label = "Mostrar conteo de palabras", Value = false },
                    ]
                },
            ]),

        new(
            "excel",
            "Microsoft Excel",
            "#217346",
            "Table",
            ["microsoft excel", "excel", "xlsx"],
            "Excel activo en el desktop.",
            "Ofimática",
            "ACCIONES",
            "Table",
            "Guardar",
            ["Guardar", "Calcular", "Filtrar", "Exportar PDF"],
            [
                new() { Label = "SUITE", Value = "Microsoft 365" },
                new() { Label = "AREA", Value = "Office" },
            ],
            [
                "Guarda o fuerza recalculo del libro activo.",
                "Activa filtros o exporta a PDF.",
                "Acciones de hoja rapidas desde el movil.",
            ],
            [
                new()
                {
                    Title = "Libro activo",
                    Fields =
                    [
                        new() { Type = "toggle", Key = "save", Label = "Guardar (Ctrl+S)", Value = false },
                        new() { Type = "toggle", Key = "force_calculate", Label = "Forzar recalculo (F9)", Value = false },
                        new() { Type = "toggle", Key = "save_as_pdf", Label = "Exportar como PDF", Value = false },
                    ]
                },
                new()
                {
                    Title = "Hoja",
                    Fields =
                    [
                        new() { Type = "toggle", Key = "toggle_filters", Label = "Activar / quitar filtros", Value = false },
                        new() { Type = "toggle", Key = "freeze_panes", Label = "Inmovilizar paneles", Value = false },
                        new() { Type = "toggle", Key = "new_sheet", Label = "Nueva hoja", Value = false },
                    ]
                },
            ]),

        new(
            "powerpoint",
            "Microsoft PowerPoint",
            "#B7472A",
            "Presentation",
            ["microsoft powerpoint", "powerpoint", "pptx"],
            "PowerPoint activo en el desktop.",
            "Ofimática",
            "ACCIONES",
            "Presentation",
            "Presentar",
            ["Presentar", "Detener", "Guardar", "Exportar PDF"],
            [
                new() { Label = "SUITE", Value = "Microsoft 365" },
                new() { Label = "AREA", Value = "Office" },
            ],
            [
                "Inicia o detiene la presentacion desde el movil.",
                "Guarda o exporta a PDF el archivo activo.",
                "Avanza o retrocede diapositivas al vuelo.",
            ],
            [
                new()
                {
                    Title = "Presentacion",
                    Fields =
                    [
                        new() { Type = "toggle", Key = "start_slideshow", Label = "Iniciar presentacion (F5)", Value = false },
                        new() { Type = "toggle", Key = "stop_slideshow", Label = "Detener presentacion (Esc)", Value = false },
                        new() { Type = "toggle", Key = "next_slide", Label = "Siguiente diapositiva", Value = false },
                        new() { Type = "toggle", Key = "prev_slide", Label = "Diapositiva anterior", Value = false },
                    ]
                },
                new()
                {
                    Title = "Archivo",
                    Fields =
                    [
                        new() { Type = "toggle", Key = "save", Label = "Guardar (Ctrl+S)", Value = false },
                        new() { Type = "toggle", Key = "export_pdf", Label = "Exportar como PDF", Value = false },
                    ]
                },
            ]),

        new(
            "outlook",
            "Microsoft Outlook",
            "#0078D4",
            "EnvelopeSimple",
            ["microsoft outlook", "outlook"],
            "Outlook activo en el desktop.",
            "Ofimática",
            "ACCIONES",
            "EnvelopeSimple",
            "Nuevo email",
            ["Nuevo email", "Responder", "Bandeja", "Calendario"],
            [
                new() { Label = "SUITE", Value = "Microsoft 365" },
                new() { Label = "AREA", Value = "Office" },
            ],
            [
                "Redacta nuevo email o responde el activo.",
                "Abre el calendario sin buscar en menus.",
                "Marca todos los correos como leidos.",
            ],
            [
                new()
                {
                    Title = "Correo",
                    Fields =
                    [
                        new() { Type = "toggle", Key = "new_email", Label = "Nuevo correo (Ctrl+N)", Value = false },
                        new() { Type = "toggle", Key = "reply", Label = "Responder activo (Ctrl+R)", Value = false },
                        new() { Type = "toggle", Key = "mark_all_read", Label = "Marcar todo como leido", Value = false },
                    ]
                },
                new()
                {
                    Title = "Navegacion",
                    Fields =
                    [
                        new() { Type = "select", Key = "go_to", Label = "Ir a", Value = "Bandeja", Options = ["Bandeja", "Enviados", "Borradores", "Calendario", "Tareas"] },
                    ]
                },
            ]),

        new(
            "teams",
            "Microsoft Teams",
            "#6264A7",
            "Users",
            ["microsoft teams", "teams"],
            "Teams activo en el desktop.",
            "Comunicacion",
            "ACCIONES",
            "Users",
            "Silenciar",
            ["Silenciar mic", "Activar camara", "Levantar mano", "Salir"],
            [
                new() { Label = "SUITE", Value = "Microsoft 365" },
                new() { Label = "AREA", Value = "Office" },
            ],
            [
                "Silencia mic o activa camara en llamada activa.",
                "Levanta la mano o abandona la reunion.",
                "Cambia estado de presencia al vuelo.",
            ],
            [
                new()
                {
                    Title = "Llamada activa",
                    Fields =
                    [
                        new() { Type = "toggle", Key = "mute_mic", Label = "Silenciar microfono", Value = false },
                        new() { Type = "toggle", Key = "toggle_camera", Label = "Activar / apagar camara", Value = false },
                        new() { Type = "toggle", Key = "raise_hand", Label = "Levantar mano", Value = false },
                        new() { Type = "toggle", Key = "leave_meeting", Label = "Salir de la reunion", Value = false },
                    ]
                },
                new()
                {
                    Title = "Estado",
                    Fields =
                    [
                        new() { Type = "select", Key = "status", Label = "Cambiar estado", Value = "Disponible", Options = ["Disponible", "Ocupado", "No molestar", "Ausente"] },
                    ]
                },
            ]),

        new(
            "vlc",
            "VLC Media Player",
            "#FF8800",
            "Play",
            ["vlc", "vlc media player", "videolan"],
            "VLC activo en el desktop.",
            "Multimedia",
            "REPRODUCCION",
            "Play",
            "Play/Pause",
            ["Play/Pause", "Siguiente", "Anterior", "Pantalla completa"],
            [
                new() { Label = "AREA", Value = "Video" },
                new() { Label = "PERFIL", Value = "Universal" },
            ],
            [
                "Controla reproduccion sin tocar la PC.",
                "Ajusta volumen o activa pantalla completa.",
                "Siguiente o anterior en la lista de reproduccion.",
            ],
            [
                new()
                {
                    Title = "Reproduccion",
                    Fields =
                    [
                        new() { Type = "toggle", Key = "play_pause", Label = "Play / Pause", Value = false },
                        new() { Type = "toggle", Key = "next", Label = "Siguiente", Value = false },
                        new() { Type = "toggle", Key = "prev", Label = "Anterior", Value = false },
                        new() { Type = "toggle", Key = "stop", Label = "Detener", Value = false },
                    ]
                },
                new()
                {
                    Title = "Pantalla y audio",
                    Fields =
                    [
                        new() { Type = "toggle", Key = "fullscreen", Label = "Pantalla completa", Value = false },
                        new() { Type = "slider", Key = "volume", Label = "Volumen", Value = 100, Min = 0, Max = 200, Unit = "%" },
                        new() { Type = "toggle", Key = "mute", Label = "Silenciar", Value = false },
                    ]
                },
            ]),

        new(
            "calculadora",
            "Calculadora",
            "#A78BFA",
            "Calculator",
            ["calculadora", "calculator", "calc"],
            "Calculadora activa en el desktop.",
            "Utilidad",
            "ACCIONES",
            "Calculator",
            "Limpiar",
            ["Limpiar", "Copiar resultado", "Historial", "Cambiar modo"],
            [
                new() { Label = "AREA", Value = "Windows" },
                new() { Label = "PERFIL", Value = "Utilidad" },
            ],
            [
                "Limpia la pantalla o copia el resultado.",
                "Cambia entre modo estandar, cientifico y programador.",
                "Accede al historial de operaciones.",
            ],
            [
                new()
                {
                    Title = "Acciones",
                    Fields =
                    [
                        new() { Type = "toggle", Key = "clear", Label = "Limpiar pantalla (C)", Value = false },
                        new() { Type = "toggle", Key = "copy_result", Label = "Copiar resultado", Value = false },
                        new() { Type = "select", Key = "switch_mode", Label = "Cambiar modo", Value = "Estándar", Options = ["Estándar", "Científico", "Programador", "Fecha"] },
                    ]
                },
            ]),

        new(
            "camara",
            "Cámara",
            "#F43F5E",
            "Camera",
            ["camara", "camera", "windows camera"],
            "Camara activa en el desktop.",
            "Multimedia",
            "ACCIONES",
            "Camera",
            "Foto",
            ["Tomar foto", "Grabar video", "Cambiar camara", "Detener"],
            [
                new() { Label = "AREA", Value = "Windows" },
                new() { Label = "PERFIL", Value = "Camara" },
            ],
            [
                "Toma foto o inicia grabacion sin tocar la PC.",
                "Cambia entre camaras disponibles.",
                "Detiene la grabacion en curso.",
            ],
            [
                new()
                {
                    Title = "Captura",
                    Fields =
                    [
                        new() { Type = "toggle", Key = "take_photo", Label = "Tomar foto", Value = false },
                        new() { Type = "toggle", Key = "start_recording", Label = "Iniciar grabacion", Value = false },
                        new() { Type = "toggle", Key = "stop_recording", Label = "Detener grabacion", Value = false },
                        new() { Type = "toggle", Key = "switch_camera", Label = "Cambiar camara", Value = false },
                    ]
                },
                new()
                {
                    Title = "Modo",
                    Fields =
                    [
                        new() { Type = "select", Key = "mode", Label = "Modo de captura", Value = "Foto", Options = ["Foto", "Video", "QR", "Documento"] },
                    ]
                },
            ]),

        new(
            "microsoft_store",
            "Microsoft Store",
            "#0078D4",
            "ShoppingBag",
            ["microsoft store", "tienda microsoft", "store"],
            "Microsoft Store activa en el desktop.",
            "Tienda",
            "ACCIONES",
            "ShoppingBag",
            "Actualizar todo",
            ["Actualizar todo", "Buscar app", "Mis apps", "Biblioteca"],
            [
                new() { Label = "AREA", Value = "Windows" },
                new() { Label = "PERFIL", Value = "Tienda" },
            ],
            [
                "Actualiza todas las apps con un tap.",
                "Navega a biblioteca o busca una app.",
                "Acceso rapido sin navegar por la tienda.",
            ],
            [
                new()
                {
                    Title = "Acciones",
                    Fields =
                    [
                        new() { Type = "toggle", Key = "update_all", Label = "Actualizar todas las apps", Value = false },
                        new() { Type = "toggle", Key = "open_library", Label = "Abrir biblioteca", Value = false },
                        new() { Type = "toggle", Key = "open_search", Label = "Abrir busqueda", Value = false },
                    ]
                },
            ]),

        new(
            "copias_seguridad",
            "Copias de seguridad de Windows",
            "#38BDF8",
            "CloudArrowUp",
            ["copias de seguridad de windows", "windows backup", "backup windows"],
            "Backup de Windows activo en el desktop.",
            "Sistema",
            "ACCIONES",
            "CloudArrowUp",
            "Hacer copia",
            ["Hacer copia", "Ver historial", "Restaurar archivo", "Estado"],
            [
                new() { Label = "AREA", Value = "Windows" },
                new() { Label = "PERFIL", Value = "Backup" },
            ],
            [
                "Lanza una copia de seguridad manual.",
                "Revisa el historial o restaura un archivo.",
                "Ve el estado del ultimo backup.",
            ],
            [
                new()
                {
                    Title = "Backup",
                    Fields =
                    [
                        new() { Type = "toggle", Key = "run_backup", Label = "Iniciar copia ahora", Value = false },
                        new() { Type = "toggle", Key = "open_history", Label = "Ver historial de archivos", Value = false },
                        new() { Type = "toggle", Key = "restore_file", Label = "Restaurar archivo", Value = false },
                    ]
                },
            ]),

        new(
            "chatgpt",
            "ChatGPT",
            "#10A37F",
            "Brain",
            ["chatgpt", "chat gpt", "openai chatgpt"],
            "Cliente ChatGPT activo en el desktop.",
            "IA",
            "ACCIONES",
            "Brain",
            "Nueva chat",
            ["Nueva chat", "Subir imagen", "Busqueda web", "Copiar respuesta"],
            [
                new() { Label = "MODELO", Value = "GPT-4o" },
                new() { Label = "PROVEEDOR", Value = "OpenAI" },
            ],
            [
                "Inicia nueva conversacion o cambia modelo.",
                "Activa busqueda web o analisis de imagen.",
                "Acciones utiles sin tocar la PC.",
            ],
            [
                new()
                {
                    Title = "Conversacion",
                    Fields =
                    [
                        new() { Type = "toggle", Key = "new_chat", Label = "Nueva conversacion", Value = false },
                        new() { Type = "select", Key = "switch_model", Label = "Cambiar modelo", Value = "GPT-4o", Options = ["GPT-4o", "GPT-4", "o1", "GPT-3.5"] },
                        new() { Type = "toggle", Key = "web_search", Label = "Activar busqueda web", Value = false },
                    ]
                },
                new()
                {
                    Title = "Herramientas",
                    Fields =
                    [
                        new() { Type = "toggle", Key = "upload_image", Label = "Subir imagen", Value = false },
                        new() { Type = "toggle", Key = "memory_toggle", Label = "Activar memoria", Value = true },
                    ]
                },
            ]),

        new(
            "codex",
            "Codex",
            "#6366F1",
            "Code",
            ["codex", "openai codex"],
            "Agente Codex activo en el desktop.",
            "IA",
            "ACCIONES",
            "Code",
            "Aprobar",
            ["Aprobar cambio", "Rechazar", "Ejecutar", "Cancelar"],
            [
                new() { Label = "MODO", Value = "Ask" },
                new() { Label = "AREA", Value = "IA" },
            ],
            [
                "Aprueba o rechaza cambios propuestos por el agente.",
                "Cambia modo entre Ask, Edit y Generate.",
                "Controla la ejecucion sin tocar la PC.",
            ],
            [
                new()
                {
                    Title = "Agente",
                    Fields =
                    [
                        new() { Type = "select", Key = "switch_mode", Label = "Modo agente", Value = "Ask", Options = ["Ask", "Edit", "Generate", "Debug"] },
                        new() { Type = "toggle", Key = "approve_change", Label = "Aprobar cambio pendiente", Value = false },
                        new() { Type = "toggle", Key = "reject_change", Label = "Rechazar cambio", Value = false },
                    ]
                },
                new()
                {
                    Title = "Ejecucion",
                    Fields =
                    [
                        new() { Type = "toggle", Key = "run_agent", Label = "Ejecutar agente", Value = false },
                        new() { Type = "toggle", Key = "cancel_agent", Label = "Cancelar ejecucion", Value = false },
                        new() { Type = "toggle", Key = "repo_context", Label = "Incluir contexto de repo", Value = true },
                    ]
                },
            ]),
    ];

    // ── PUBLIC API ────────────────────────────────────────────────────────────

    public static string GetCanonicalAppName(AppInfo app)
        => ResolveCatalogEntry(app)?.DisplayName ?? app.Name;

    public static AppInfo NormalizeAppInfo(AppInfo app)
    {
        var canonicalName = GetCanonicalAppName(app);
        return new AppInfo
        {
            Name = canonicalName,
            ExecutablePath = app.ExecutablePath,
            IconPath = app.IconPath,
            Publisher = app.Publisher,
            Version = app.Version,
            IsRunning = app.IsRunning,
            ProcessId = app.ProcessId,
            WindowTitle = app.WindowTitle,
            InstallDate = app.InstallDate,
        };
    }

    public static List<SyncCategory> BuildCategories(List<AppInfo> apps, AppInfo? activeApp = null)
    {
        var catalogApps = MergeApps(apps, activeApp);
        var categories = new List<SyncCategory>();

        if (activeApp != null)
        {
            categories.Add(BuildActiveCategory(activeApp, catalogApps));
        }

        foreach (var entry in Catalog)
        {
            var app = FindCatalogApp(catalogApps, entry);
            if (app != null)
            {
                categories.Add(entry.Id == "spotify"
                    ? BuildSpotifyCategory(app)
                    : BuildCategory(entry, app));
            }
        }

        return categories;
    }

    private static SyncCategory BuildSpotifyCategory(AppInfo app)
    {
        return new SyncCategory
        {
            Id = "spotify",
            Name = "SPOTIFY",
            Color = "#1DB954",
            Icon = "MusicNotes",
            Shortcuts =
            [
                new()
                {
                    Id = "MED_NOW",
                    Label = "NOW PLAYING",
                    Icon = "MusicNotes",
                    Size = "big",
                    Subtitle = app.IsRunning ? (app.WindowTitle ?? "Spotify abierto") : "Sin reproducción activa",
                    Stats =
                    [
                        new() { Label = "PISTA",   Value = "—" },
                        new() { Label = "ARTISTA", Value = "—" },
                        new() { Label = "ÁLBUM",   Value = "—" },
                        new() { Label = "ESTADO",  Value = app.IsRunning ? "Abierto" : "Cerrado" },
                    ],
                    ProgressValue = 0,
                    ProgressLabel = ["0:00", "0:00"],
                    Command = new() { Action = ActionTypes.MediaPlay },
                },
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
                    Command = new() { Action = ActionTypes.SetVolume, Target = "spotify", Params = new() { ["value"] = (object)72 } },
                },
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
                    Command = new() { Action = ActionTypes.SetVolume, Params = new() { ["value"] = (object)70 } },
                },
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
            ]
        };
    }

    // ── PRIVATE BUILDERS ─────────────────────────────────────────────────────

    private static SyncCategory BuildActiveCategory(AppInfo activeApp, List<AppInfo> apps)
    {
        var normalized = NormalizeAppInfo(activeApp);
        var entry = ResolveCatalogEntry(activeApp);
        var metadata = entry != null ? FindCatalogApp(apps, entry) ?? normalized : normalized;
        var detail = string.IsNullOrWhiteSpace(normalized.WindowTitle)
            ? "Ventana en foco detectada en desktop."
            : normalized.WindowTitle!;

        var stats = new List<SyncStat>
        {
            new() { Label = "ESTADO", Value = "Focused" },
            new() { Label = "TIPO", Value = entry?.Kind ?? "Desktop" },
            new() { Label = "VERSION", Value = metadata.Version ?? "Detectada" },
        };

        if (metadata.ProcessId.HasValue)
        {
            stats.Add(new() { Label = "PID", Value = metadata.ProcessId.Value.ToString() });
        }

        return new SyncCategory
        {
            Id = "active_app",
            Name = "App Activa",
            Color = entry?.Color ?? "#22C55E",
            Icon = "CursorClick",
            Shortcuts =
            [
                new()
                {
                    Id = "active_app_now",
                    Label = "APP ACTIVA",
                    Icon = entry?.Icon ?? "CursorClick",
                    Size = "big",
                    Subtitle = normalized.Name,
                    Detail = detail,
                    Value = "FOCUSED",
                    ActionType = "status",
                    Stats = stats,
                    ProgressValue = 100,
                    SettingsGroups = BuildOverviewSettings(entry, metadata),
                }
            ]
        };
    }

    private static SyncCategory BuildCategory(AppCatalogEntry entry, AppInfo app)
    {
        var normalized = NormalizeAppInfo(app);

        return new SyncCategory
        {
            Id = entry.Id,
            Name = entry.DisplayName,
            Color = entry.Color,
            Icon = entry.Icon,
            Shortcuts =
            [
                BuildOverviewShortcut(entry, normalized),
                BuildQuickActionsShortcut(entry),
                BuildContextShortcut(entry, normalized),
            ]
        };
    }

    private static SyncShortcut BuildOverviewShortcut(AppCatalogEntry entry, AppInfo app)
    {
        var stats = new List<SyncStat>
        {
            new() { Label = "ESTADO", Value = app.IsRunning ? "Abierta" : "Instalada" },
            new() { Label = "TIPO", Value = entry.Kind },
            new() { Label = "VERSION", Value = app.Version ?? "Detectada" },
        };

        stats.AddRange(entry.BaseStats.Select(stat => new SyncStat
        {
            Label = stat.Label,
            Value = stat.Value,
        }));

        if (app.ProcessId.HasValue)
        {
            stats.Add(new() { Label = "PID", Value = app.ProcessId.Value.ToString() });
        }

        return new SyncShortcut
        {
            Id = $"{entry.Id}_overview",
            Label = entry.DisplayName.ToUpperInvariant(),
            Icon = entry.Icon,
            Size = "big",
            Subtitle = app.IsRunning ? "Abierta en esta PC" : "Disponible en esta PC",
            Detail = ResolveDetail(entry, app),
            Value = app.IsRunning ? "OPEN" : "READY",
            ActionType = "status",
            Stats = stats,
            ProgressValue = app.IsRunning ? 100 : 45,
            SettingsGroups = BuildOverviewSettings(entry, app),
        };
    }

    private static SyncShortcut BuildQuickActionsShortcut(AppCatalogEntry entry)
    {
        return new SyncShortcut
        {
            Id = $"{entry.Id}_actions",
            Label = entry.QuickActionLabel,
            Icon = entry.QuickActionIcon,
            Size = "wide",
            Detail = "Acciones rapidas para ejecutar desde el movil.",
            ActionType = "chips",
            Value = entry.QuickActionValue,
            Options = entry.QuickActionOptions.ToList(),
        };
    }

    private static SyncShortcut BuildContextShortcut(AppCatalogEntry entry, AppInfo app)
    {
        return new SyncShortcut
        {
            Id = $"{entry.Id}_context",
            Label = "CONTEXTO",
            Icon = "Clock",
            Size = "tall",
            Detail = string.IsNullOrWhiteSpace(app.ExecutablePath)
                ? entry.Detail
                : $"Exe: {Path.GetFileName(app.ExecutablePath)}",
            Stats =
            [
                new() { Label = "FOCO", Value = app.IsRunning ? "Activo" : "En espera" },
                new() { Label = "APP", Value = entry.DisplayName },
            ],
            Logs = BuildContextLogs(entry, app),
        };
    }

    private static List<SyncSettingsGroup> BuildOverviewSettings(AppCatalogEntry? entry, AppInfo app)
    {
        var groups = new List<SyncSettingsGroup>
        {
            new()
            {
                Title = "Identidad",
                Fields =
                [
                    new() { Type = "info", Key = "name", Label = "Nombre", Value = GetCanonicalAppName(app) },
                    new() { Type = "info", Key = "state", Label = "Estado", Value = app.IsRunning ? "Abierta" : "Instalada" },
                    new() { Type = "info", Key = "version", Label = "Version", Value = app.Version ?? "Detectada" },
                    new() { Type = "info", Key = "publisher", Label = "Publisher", Value = app.Publisher ?? "Desconocido" },
                    new() { Type = "info", Key = "path", Label = "Ruta", Value = string.IsNullOrWhiteSpace(app.ExecutablePath) ? "N/D" : app.ExecutablePath },
                ]
            }
        };

        if (entry != null)
        {
            groups.AddRange(entry.SettingsGroups.Select(CloneSettingsGroup));
        }

        return groups;
    }

    private static List<string> BuildContextLogs(AppCatalogEntry entry, AppInfo app)
    {
        var logs = new List<string>();

        if (!string.IsNullOrWhiteSpace(app.WindowTitle))
        {
            logs.Add($"Ventana: {TrimText(app.WindowTitle!, 64)}");
        }

        logs.Add(app.IsRunning
            ? "Estado actual: abierta y lista para foco."
            : "Estado actual: detectada como instalada.");

        if (!string.IsNullOrWhiteSpace(app.ExecutablePath))
        {
            logs.Add($"Binario: {Path.GetFileName(app.ExecutablePath)}");
        }

        logs.AddRange(entry.Logs);
        return logs.Take(5).ToList();
    }

    private static string ResolveDetail(AppCatalogEntry entry, AppInfo app)
    {
        if (app.IsRunning && !string.IsNullOrWhiteSpace(app.WindowTitle))
        {
            return TrimText(app.WindowTitle!, 72);
        }

        return entry.Detail;
    }

    private static List<AppInfo> MergeApps(List<AppInfo> apps, AppInfo? activeApp)
    {
        var merged = apps.ToList();
        if (activeApp == null)
        {
            return merged;
        }

        var alreadyPresent = merged.Any(existing =>
            (!string.IsNullOrWhiteSpace(existing.ExecutablePath)
             && !string.IsNullOrWhiteSpace(activeApp.ExecutablePath)
             && existing.ExecutablePath.Equals(activeApp.ExecutablePath, StringComparison.OrdinalIgnoreCase))
            || existing.Name.Equals(activeApp.Name, StringComparison.OrdinalIgnoreCase)
            || (existing.ProcessId.HasValue && activeApp.ProcessId.HasValue && existing.ProcessId == activeApp.ProcessId));

        if (!alreadyPresent)
        {
            merged.Add(activeApp);
        }

        return merged;
    }

    private static AppInfo? FindCatalogApp(IEnumerable<AppInfo> apps, AppCatalogEntry entry)
    {
        return apps
            .Where(app => MatchesEntry(app, entry))
            .OrderByDescending(app => app.IsRunning)
            .ThenByDescending(app => !string.IsNullOrWhiteSpace(app.WindowTitle))
            .ThenByDescending(app => !string.IsNullOrWhiteSpace(app.Version))
            .FirstOrDefault();
    }

    private static AppCatalogEntry? ResolveCatalogEntry(AppInfo app)
        => Catalog.FirstOrDefault(entry => MatchesEntry(app, entry));

    private static bool MatchesEntry(AppInfo app, AppCatalogEntry entry)
    {
        var corpus = BuildSearchCorpus(app);

        if (entry.Id == "vs_code" && corpus.Contains("insiders", StringComparison.Ordinal))
            return false;

        // Apps de IA web-only: solo matchean si el ejecutable no es un navegador
        if (entry.Id is "chatgpt" or "codex")
        {
            if (!string.IsNullOrWhiteSpace(app.ExecutablePath))
            {
                var exe = Path.GetFileNameWithoutExtension(app.ExecutablePath).ToLowerInvariant();
                if (exe is "chrome" or "brave" or "msedge" or "firefox" or "opera")
                    return false;
            }
        }

        return entry.Aliases.Any(alias => corpus.Contains(NormalizeKey(alias), StringComparison.Ordinal));
    }

    private static string BuildSearchCorpus(AppInfo app)
    {
        var parts = new[]
        {
            app.Name,
            app.ExecutablePath,
            app.WindowTitle,
            app.Publisher,
            Path.GetFileNameWithoutExtension(app.ExecutablePath ?? string.Empty),
        };

        return string.Join(' ', parts.Select(NormalizeKey));
    }

    private static string NormalizeKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Normalize(NormalizationForm.FormD);
        var buffer = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(character))
            {
                buffer.Append(char.ToLowerInvariant(character));
                continue;
            }

            if (char.IsWhiteSpace(character))
            {
                buffer.Append(' ');
            }
        }

        return string.Join(' ', buffer
            .ToString()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static SyncSettingsGroup CloneSettingsGroup(SyncSettingsGroup group)
    {
        return new SyncSettingsGroup
        {
            Title = group.Title,
            Fields = group.Fields.Select(field => new SyncField
            {
                Type = field.Type,
                Key = field.Key,
                Label = field.Label,
                Value = field.Value,
                Min = field.Min,
                Max = field.Max,
                Unit = field.Unit,
                Options = field.Options?.ToList(),
            }).ToList()
        };
    }

    private static string TrimText(string value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length <= maxLength)
        {
            return value;
        }

        return value[..Math.Max(maxLength - 3, 1)] + "...";
    }
}
