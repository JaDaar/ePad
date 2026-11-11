using System.Reflection;

namespace ePad;

/// <summary>Help > About dialog. All text comes from the assembly attributes set in ePad.csproj.</summary>
public sealed class AboutForm : Form
{
    public AboutForm()
    {
        var asm = Assembly.GetExecutingAssembly();
        var product = asm.GetCustomAttribute<AssemblyProductAttribute>()?.Product ?? "ePad";
        var company = asm.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company ?? "";
        var copyright = asm.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright ?? "";
        var description = Strings.T("AppDescription");
        var version = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                      ?? asm.GetName().Version?.ToString() ?? "";

        Text = Strings.T("AboutTitle", product);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ShowIcon = false;
        StartPosition = FormStartPosition.CenterParent;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(20);

        Label Line(string text, float size = 9f, FontStyle style = FontStyle.Regular, int top = 2) => new()
        {
            Text = text,
            AutoSize = true,
            Font = new Font("Segoe UI", size, style),
            Margin = new Padding(0, top, 0, 2),
        };

        var ok = new Button { Text = Strings.T("OK"), DialogResult = DialogResult.OK, AutoSize = true, Anchor = AnchorStyles.Right, Margin = new Padding(0, 16, 0, 0) };

        var layout = new TableLayoutPanel { AutoSize = true, ColumnCount = 1, Dock = DockStyle.Fill };
        layout.Controls.Add(Line(product, 16f, FontStyle.Bold, 0));
        layout.Controls.Add(Line(Strings.T("Version", version)));
        layout.Controls.Add(Line(description, top: 8));
        layout.Controls.Add(Line(company, 9f, FontStyle.Bold, 12));
        layout.Controls.Add(Line(copyright));
        layout.Controls.Add(ok);

        Controls.Add(layout);
        AcceptButton = ok;
        CancelButton = ok;
    }
}
