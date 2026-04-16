using System;
using Microsoft.UI.Xaml.Controls;

namespace DesktopAssistant.Services;

/// <summary>
/// Provides navigation between pages using a Frame control.
/// </summary>
public class NavigationService
{
    private Frame? _frame;
    private Frame? _rootFrame;

    /// <summary>
    /// The main content frame (inside NavigationView for authenticated pages).
    /// </summary>
    public Frame? Frame
    {
        get => _frame;
        set => _frame = value;
    }

    /// <summary>
    /// The root frame (for switching between login and main shell).
    /// </summary>
    public Frame? RootFrame
    {
        get => _rootFrame;
        set => _rootFrame = value;
    }

    public bool CanGoBack => _frame?.CanGoBack ?? false;

    public void GoBack()
    {
        if (_frame?.CanGoBack == true)
        {
            _frame.GoBack();
        }
    }

    /// <summary>
    /// Navigate within the authenticated shell (content frame).
    /// </summary>
    public bool Navigate(Type pageType, object? parameter = null)
    {
        if (_frame == null) return false;
        return _frame.Navigate(pageType, parameter);
    }

    /// <summary>
    /// Navigate at root level (LoginPage <-> ShellPage).
    /// </summary>
    public bool NavigateRoot(Type pageType, object? parameter = null)
    {
        if (_rootFrame == null) return false;
        return _rootFrame.Navigate(pageType, parameter);
    }
}
