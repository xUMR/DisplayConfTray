using System.Media;
using System.Runtime.InteropServices;

namespace DisplayConfTray;

public static class DarkMessageBox
{
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

    // Background colors
    private static readonly Color BackColor = Color.FromArgb(32, 32, 32);
    private static readonly Color FooterColor = Color.FromArgb(43, 43, 43);
    private static readonly Color ForeColor = Color.White;
    private static readonly Color ButtonBackColor = Color.FromArgb(51, 51, 51);
    private static readonly Color ButtonBorderColor = Color.FromArgb(112, 112, 112);
    private static readonly Color ButtonHoverColor = Color.FromArgb(65, 65, 65);
    private static readonly Color ButtonPressedColor = Color.FromArgb(40, 40, 40);
    private static readonly Color FooterBorderColor = Color.FromArgb(60, 60, 60);

    public static DialogResult Show(
        string message,
        string title,
        MessageBoxButtons buttons = MessageBoxButtons.OK,
        MessageBoxIcon icon = MessageBoxIcon.None)
    {
        var result = DialogResult.None;

        using var form = new Form
        {
            Text = title,
            BackColor = BackColor,
            ForeColor = ForeColor,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterScreen,
            MaximizeBox = false,
            MinimizeBox = false,
            ShowInTaskbar = true,
            Font = SystemFonts.MessageBoxFont ?? new Font("Segoe UI", 9F)
        };

        // Apply dark title bar via DWM — must be done after the handle is created
        form.HandleCreated += (_, _) =>
        {
            var useDark = 1;
            DwmSetWindowAttribute(form.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDark, sizeof(int));
        };

        // --- Content panel (icon + message) ---
        var contentPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(24, 20, 24, 20),
            BackColor = BackColor
        };

        var messageLabel = new Label
        {
            Text = message,
            ForeColor = ForeColor,
            AutoSize = true,
            MaximumSize = new Size(340, 0), // wrap long text
            UseCompatibleTextRendering = false
        };

        PictureBox? iconBox = null;
        var systemIcon = GetSystemIcon(icon);
        if (systemIcon != null)
        {
            iconBox = new PictureBox
            {
                Image = systemIcon.ToBitmap(),
                SizeMode = PictureBoxSizeMode.AutoSize,
                Location = new Point(24, 24),
            };
            // Place label to the right of the icon, vertically centered later
            messageLabel.Location = new Point(24 + 32 + 16, 0); // adjusted after sizing
            contentPanel.Controls.Add(iconBox);
        }
        else
        {
            messageLabel.Location = new Point(24, 0); // adjusted after sizing
        }

        contentPanel.Controls.Add(messageLabel);

        // --- Footer panel (buttons) ---
        var footerPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 52,
            BackColor = FooterColor,
            Padding = new Padding(12, 0, 12, 0)
        };

        // Draw a subtle top border on the footer
        footerPanel.Paint += (_, e) =>
        {
            using var pen = new Pen(FooterBorderColor);
            e.Graphics.DrawLine(pen, 0, 0, footerPanel.Width, 0);
        };

        var buttonDefs = GetButtonDefinitions(buttons);
        const int buttonWidth = 88;
        const int buttonHeight = 26;
        const int buttonSpacing = 10;
        var totalButtonsWidth = buttonDefs.Count * buttonWidth + (buttonDefs.Count - 1) * buttonSpacing;

        for (var i = 0; i < buttonDefs.Count; i++)
        {
            var (text, dialogResult) = buttonDefs[i];
            var btn = CreateStyledButton(text, buttonWidth, buttonHeight);
            btn.DialogResult = dialogResult;
            // Right-align buttons in the footer, vertically centered
            btn.Location = new Point(0, (footerPanel.Height - buttonHeight) / 2); // X set after form sizing
            btn.Tag = i; // store index for positioning later

            btn.Click += (_, _) =>
            {
                result = dialogResult;
                form.Close();
            };

            footerPanel.Controls.Add(btn);
        }

        form.Controls.Add(contentPanel);
        form.Controls.Add(footerPanel);

        // --- Size the form based on content ---
        // Force label to compute its preferred size
        var preferredLabelSize = messageLabel.GetPreferredSize(new Size(340, 0));
        messageLabel.Size = preferredLabelSize;

        var contentLeft = iconBox != null ? 24 + 32 + 16 : 24;
        var contentRight = 24;
        var contentTop = 24;

        // Vertically center the label relative to the icon (if any)
        if (iconBox != null)
        {
            var iconMid = iconBox.Top + 32 / 2;
            var labelMid = preferredLabelSize.Height / 2;
            if (preferredLabelSize.Height < 32)
            {
                messageLabel.Location = new Point(contentLeft, iconMid - labelMid);
            }
            else
            {
                messageLabel.Location = new Point(contentLeft, contentTop);
            }
        }
        else
        {
            messageLabel.Location = new Point(contentLeft, contentTop);
        }

        var contentWidth = contentLeft + preferredLabelSize.Width + contentRight;
        var contentHeight = Math.Max(
            iconBox != null ? 32 : 0,
            preferredLabelSize.Height) + contentTop + 20;

        var minWidth = totalButtonsWidth + 40;
        var formClientWidth = Math.Max(Math.Max(contentWidth, minWidth), 280);
        var formClientHeight = contentHeight + footerPanel.Height;

        form.ClientSize = new Size(formClientWidth, formClientHeight);

        // Position buttons right-aligned in the footer
        var buttonStartX = formClientWidth - 12 - totalButtonsWidth;
        foreach (Control ctrl in footerPanel.Controls)
        {
            if (ctrl is Button btn && btn.Tag is int idx)
            {
                btn.Location = new Point(
                    buttonStartX + idx * (buttonWidth + buttonSpacing),
                    btn.Location.Y);
            }
        }

        // Set default/cancel buttons
        if (buttonDefs.Count > 0)
        {
            form.AcceptButton = (Button)footerPanel.Controls[0];
        }
        if (buttonDefs.Count > 1)
        {
            form.CancelButton = (Button)footerPanel.Controls[^1];
        }

        // Play the corresponding system sound
        PlaySystemSound(icon);

        form.ShowDialog();

        systemIcon?.Dispose();
        iconBox?.Image?.Dispose();

        return result;
    }

    private static Button CreateStyledButton(string text, int width, int height)
    {
        var btn = new Button
        {
            Text = text,
            Size = new Size(width, height),
            FlatStyle = FlatStyle.Flat,
            BackColor = ButtonBackColor,
            ForeColor = ForeColor,
            Cursor = Cursors.Default,
            Font = SystemFonts.MessageBoxFont ?? new Font("Segoe UI", 9F)
        };

        btn.FlatAppearance.BorderColor = ButtonBorderColor;
        btn.FlatAppearance.BorderSize = 1;
        btn.FlatAppearance.MouseOverBackColor = ButtonHoverColor;
        btn.FlatAppearance.MouseDownBackColor = ButtonPressedColor;

        return btn;
    }

    private static List<(string Text, DialogResult Result)> GetButtonDefinitions(MessageBoxButtons buttons) =>
        buttons switch
        {
            MessageBoxButtons.OK => [("OK", DialogResult.OK)],
            MessageBoxButtons.OKCancel => [("OK", DialogResult.OK), ("Cancel", DialogResult.Cancel)],
            MessageBoxButtons.YesNo => [("Yes", DialogResult.Yes), ("No", DialogResult.No)],
            MessageBoxButtons.YesNoCancel => [("Yes", DialogResult.Yes), ("No", DialogResult.No), ("Cancel", DialogResult.Cancel)],
            MessageBoxButtons.RetryCancel => [("Retry", DialogResult.Retry), ("Cancel", DialogResult.Cancel)],
            MessageBoxButtons.AbortRetryIgnore => [("Abort", DialogResult.Abort), ("Retry", DialogResult.Retry), ("Ignore", DialogResult.Ignore)],
            _ => [("OK", DialogResult.OK)]
        };

    private static Icon? GetSystemIcon(MessageBoxIcon icon)
    {
        var stockIconId = icon switch
        {
            MessageBoxIcon.Error       => StockIconId.Error,
            MessageBoxIcon.Warning     => StockIconId.Warning,
            MessageBoxIcon.Information => StockIconId.Info,
            MessageBoxIcon.Question    => StockIconId.Help,
            _                          => (StockIconId?)null
        };
        var size = SystemInformation.IconSize.Width;
        return stockIconId.HasValue ? SystemIcons.GetStockIcon(stockIconId.Value, size) : null;
    }

    private static void PlaySystemSound(MessageBoxIcon icon)
    {
        switch (icon)
        {
            case MessageBoxIcon.Error:
                SystemSounds.Hand.Play();
                break;
            case MessageBoxIcon.Warning:
                SystemSounds.Exclamation.Play();
                break;
            case MessageBoxIcon.Information:
                SystemSounds.Asterisk.Play();
                break;
            case MessageBoxIcon.Question:
                SystemSounds.Question.Play();
                break;
        }
    }
}
