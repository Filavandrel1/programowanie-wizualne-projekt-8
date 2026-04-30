using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using projekt8plsdzialaj.ViewModels;

namespace projekt8plsdzialaj;

/// <summary>
/// Given a view model, returns the corresponding view if possible.
/// </summary>
[RequiresUnreferencedCode(
    "Default implementation of ViewLocator involves reflection which may be trimmed away.",
    Url = "https://docs.avaloniaui.net/docs/concepts/view-locator")]
public class ViewLocator : IDataTemplate
{
    public Control? Build(object? param)
    {
        if (param is null)
            return null;

        if (param is ViewModels.WarViewModel)
        {
            return new Views.WarView();
        }

        if (param is ViewModels.GameViewModelBase)
        {
            return new Views.GameView();
        }

        // Walk up the inheritance chain so derived view-models can share a base View.
        var t = param.GetType();
        while (t != null && t != typeof(object))
        {
            var name = t.FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
            var type = Type.GetType(name);
            if (type != null)
                return (Control)Activator.CreateInstance(type)!;
            t = t.BaseType;
        }

        return new TextBlock { Text = "Not Found: " + param.GetType().FullName };
    }

    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}
