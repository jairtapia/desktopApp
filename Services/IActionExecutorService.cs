using System.Threading.Tasks;
using DesktopAssistant.Models;

namespace DesktopAssistant.Services;

/// <summary>
/// Service for executing system actions (open/close apps, volume, brightness, etc.).
/// </summary>
public interface IActionExecutorService
{
    Task<ActionResponse> ExecuteAsync(ActionCommand command);
}
