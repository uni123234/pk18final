using System.Windows;
using Project.Infra.Display;

namespace Project.GUI.Services;

/// <summary>
/// Displayer adapter routing domain console messages, combat logs, and entity details to the WPF UI thread.
/// </summary>
public class GuiDisplayer : IDisplayer
{
    private readonly Action<string> _logSink;

    public GuiDisplayer(Action<string> logSink)
    {
        _logSink = logSink ?? throw new ArgumentNullException(nameof(logSink));
    }

    public void Display(string line)
    {
        SendToUi(line);
    }

    public void Display(IDisplayable displayable)
    {
        if (displayable == null) return;
        var formatted = displayable.Format();
        SendToUi(formatted);
    }

    private void SendToUi(string text)
    {
        if (Application.Current != null && Application.Current.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
        {
            Application.Current.Dispatcher.BeginInvoke(() => _logSink(text));
        }
        else
        {
            _logSink(text);
        }
    }
}
