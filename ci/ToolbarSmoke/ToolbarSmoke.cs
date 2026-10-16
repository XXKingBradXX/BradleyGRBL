using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using LaserGRBL;
using LaserGRBL.Icons;
using LaserGRBL.UserControls;

internal static class ToolbarSmoke
{
    private static IEnumerable<Control> Descendants(Control root)
    {
        foreach (Control child in root.Controls)
        {
            yield return child;
            foreach (Control nested in Descendants(child)) yield return nested;
        }
    }
    private static IEnumerable<ToolStripItem> MenuItems(ToolStripItemCollection items)
    {
        foreach (ToolStripItem item in items)
        {
            yield return item;
            ToolStripDropDownItem dropdown = item as ToolStripDropDownItem;
            if (dropdown != null) foreach (ToolStripItem child in MenuItems(dropdown.DropDownItems)) yield return child;
        }
    }
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
    [STAThread]
    private static void Main(string[] args)
    {
        int exit = 1;
        try { Run(Path.GetFullPath(args[0])); exit = 0; }
        catch (Exception error) { Console.Error.WriteLine(error); }
        Environment.Exit(exit);
    }
    private static void Run(string output)
    {
        Directory.CreateDirectory(output);
        // Use only a disposable hosted Windows runner. Do not call Program.Main,
        // connect a port, open the machine-run action, or emulate a controller.
        Type program = typeof(MainForm).Assembly.GetType("LaserGRBL.Program", true);
        program.GetProperty("CurrentVersion", BindingFlags.Public | BindingFlags.Static)
            .SetValue(null, typeof(MainForm).Assembly.GetName().Version, null);
        Settings.ForcedGraphicMode = Settings.GraphicMode.GDI;
        Settings.SetObject("Auto Update", false);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        ThemeMgr.Initialize();
        IconsMgr.Initialize();
        CustomButtons.LoadFile();
        using (MainForm form = new MainForm())
        {
            // Skip only first-run dialogs, auto-update and command-line processing
            // in this test process, not the application build or its safety gates.
            MethodInfo load = typeof(MainForm).GetMethod("MainForm_Load", BindingFlags.Instance | BindingFlags.NonPublic);
            form.Load -= (EventHandler)Delegate.CreateDelegate(typeof(EventHandler), form, load);
            form.StartPosition = FormStartPosition.Manual;
            form.Location = new Point(0, 0);
            form.Size = new Size(1280, 850);
            form.Show();
            Application.DoEvents();
            form.Refresh();
            using (Bitmap image = new Bitmap(form.Width, form.Height))
            {
                form.DrawToBitmap(image, new Rectangle(Point.Empty, image.Size));
                image.Save(Path.Combine(output, "BradleyGRBL-toolbar.png"), ImageFormat.Png);
            }
            ImageButton outline = Descendants(form).OfType<ImageButton>()
                .SingleOrDefault(control => control.Name.IndexOf("Outline", StringComparison.OrdinalIgnoreCase) >= 0);
            Require(outline != null, "The outline ImageButton must exist in the actual Windows control tree.");
            Require(!outline.Enabled, "Outline must remain disabled while disconnected.");
            Require(outline.Image != null, "Outline must render a real themed icon.");
            Control blink = Descendants(form).OfType<PreviewForm.CustomButtonIB>().FirstOrDefault(button => button.Caption == "Blink");
            Require(blink != null, "Expected default lightbulb/Blink button for layout verification.");
            Point outlinePoint = outline.PointToScreen(Point.Empty), blinkPoint = blink.PointToScreen(Point.Empty);
            Require(outlinePoint.X >= blinkPoint.X + blink.Width, "Outline must appear to the right of the lightbulb.");
            Require(Math.Abs(outlinePoint.Y - blinkPoint.Y) <= 3, "Outline must share the lightbulb's horizontal row.");
            Require(outline.Size == blink.Size, "Outline must match the neighboring toolbar button size.");
            ToolStripItem speed = Descendants(form).OfType<MenuStrip>().SelectMany(menu => MenuItems(menu.Items))
                .SingleOrDefault(item => item.Text.IndexOf("Outline", StringComparison.OrdinalIgnoreCase) >= 0 && item.Text.IndexOf("Speed", StringComparison.OrdinalIgnoreCase) >= 0);
            Require(speed != null && speed.Enabled, "Outline speed setting must be available while disconnected.");
            ToolStripDropDown parent = speed.Owner as ToolStripDropDown;
            if (parent != null && parent.OwnerItem != null) Require(parent.OwnerItem.Available, "Speed menu must not be hidden in GDI mode.");
            string result = "PASS: real WinForms control tree; same-size outline icon after Blink; disconnected run disabled; top speed setting enabled and accessible in GDI. No controller connection or machine commands.\n";
            File.WriteAllText(Path.Combine(output, "toolbar-smoke.txt"), result);
            Console.WriteLine(result);
        }
    }
}
