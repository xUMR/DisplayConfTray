namespace DisplayConfTray;

public class DarkModeRenderer() : ToolStripProfessionalRenderer(new DarkModeColorTable())
{
    protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        // Create a custom rect for the checkmark area
        var rect = new Rectangle(e.ImageRectangle.Location, e.ImageRectangle.Size);
        rect.Inflate(-1, -1);

        // Draw a subtle dark background for the "checked" state instead of bright blue
        using var brush = new SolidBrush(Color.FromArgb(80, 80, 80));
        g.FillRoundedRectangle(brush, rect, 3); // Helper for rounded corners

        // Draw the white checkmark tick
        using var pen = new Pen(Color.White, 2);
        var points = new Point[]
        {
            new Point(rect.Left + 3, rect.Top + 6),
            new Point(rect.Left + 6, rect.Top + 9),
            new Point(rect.Left + 11, rect.Top + 3)
        };
        g.DrawLines(pen, points);
    }
}

public class DarkModeColorTable : ProfessionalColorTable
{
    // The main background of the dropdown
    public override Color ToolStripDropDownBackground => Color.FromArgb(32, 32, 32);

    // This fixes the "White Bar" - make both ends of the gradient the same as the background
    public override Color ImageMarginGradientBegin => Color.FromArgb(32, 32, 32);
    public override Color ImageMarginGradientMiddle => Color.FromArgb(32, 32, 32);
    public override Color ImageMarginGradientEnd => Color.FromArgb(32, 32, 32);

    // The border around the entire menu
    public override Color MenuBorder => Color.FromArgb(45, 45, 45);

    // The separator line color
    public override Color SeparatorDark => Color.FromArgb(60, 60, 60);
    public override Color SeparatorLight => Color.Transparent;

    // Hover / Selected item colors
    public override Color MenuItemSelected => Color.FromArgb(50, 50, 50);
    public override Color MenuItemSelectedGradientBegin => Color.FromArgb(50, 50, 50);
    public override Color MenuItemSelectedGradientEnd => Color.FromArgb(50, 50, 50);
    public override Color MenuItemBorder => Color.FromArgb(65, 65, 65);
}
