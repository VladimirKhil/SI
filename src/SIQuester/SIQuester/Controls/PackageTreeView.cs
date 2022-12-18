using System.Windows.Controls;
using System.Windows.Input;

namespace SIQuester.Controls;

public sealed class PackageTreeView : TreeView
{
    public PackageTreeView()
    {
        PreviewKeyDown += PackageTreeView_PreviewKeyDown;
    }

    private void PackageTreeView_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Add || e.Key == Key.Subtract)
        {
            if (e.OriginalSource is TextBox textBox)
            {
                int caretIndex = textBox.CaretIndex;
                textBox.Text = textBox.Text.Insert(caretIndex, e.Key == Key.Add ? "+" : "-");
                textBox.CaretIndex = caretIndex + 1;
                e.Handled = true;
            }
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Multiply || e.Key == Key.Add || e.Key == Key.Subtract)
        {
            return;
        }

        base.OnKeyDown(e);
    }
}
