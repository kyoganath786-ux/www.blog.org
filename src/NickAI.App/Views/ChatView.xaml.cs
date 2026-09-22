using System.Windows.Controls;
using System.Windows.Input;
using NickAI.App.ViewModels;

namespace NickAI.App.Views;

public partial class ChatView : UserControl
{
    public ChatView() => InitializeComponent();

    /// <summary>Ctrl+Enter sends the prompt; plain Enter inserts a new line.</summary>
    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control) return;

        if (DataContext is ChatViewModel viewModel && viewModel.SendCommand.CanExecute(null))
            viewModel.SendCommand.Execute(null);

        e.Handled = true;
    }
}
