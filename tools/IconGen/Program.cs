using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

string outputDir = args.Length > 0 ? args[0] : @"..\..\src\Defenestra\Assets";
Directory.CreateDirectory(outputDir);

int[] sizes = [16, 24, 32, 48, 64, 128, 256];
var pngPaths = new List<string>();

foreach (int size in sizes)
{
    using var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
    using var g = Graphics.FromImage(bmp);
    g.SmoothingMode = SmoothingMode.AntiAlias;
    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
    g.Clear(Color.Transparent);

    float s = size;
    float thick = Math.Max(1.2f, s * 0.045f);

    // Colors (Catppuccin Mocha)
    var bgColor = Color.FromArgb(30, 30, 46);         // #1e1e2e
    var frameColor = Color.FromArgb(137, 180, 250);    // #89b4fa blue
    var frameLight = Color.FromArgb(180, 210, 255);    // lighter blue for highlights
    var sillColor = Color.FromArgb(166, 227, 161);     // #a6e3a1 green
    var paneColor = Color.FromArgb(40, 203, 166, 247); // translucent mauve for glass
    var paneShine = Color.FromArgb(60, 255, 255, 255); // glass reflection
    var dimColor = Color.FromArgb(69, 71, 90);         // #45475a

    // === Background rounded rect ===
    float bgPad = s * 0.04f;
    float bgRadius = s * 0.18f;
    using var bgBrush = new SolidBrush(bgColor);
    using var bgPath = RoundedRect(bgPad, bgPad, s - bgPad * 2, s - bgPad * 2, bgRadius);
    g.FillPath(bgBrush, bgPath);

    // === Classic window shape ===
    float winPad = s * 0.14f;
    float winX = winPad;
    float winY = winPad + s * 0.04f;
    float winW = s - winPad * 2;
    float winH = s - winPad * 2 - s * 0.08f;

    // Window sill (bottom ledge, slightly wider)
    float sillExtra = s * 0.04f;
    float sillH = s * 0.06f;
    using var sillBrush = new SolidBrush(sillColor);
    g.FillRectangle(sillBrush, winX - sillExtra, winY + winH, winW + sillExtra * 2, sillH);

    // Main window frame (outer rectangle)
    using var framePen = new Pen(frameColor, thick);
    float frameR = s * 0.04f;
    using var framePath = RoundedRect(winX, winY, winW, winH, frameR);
    g.DrawPath(framePen, framePath);

    // Vertical divider (center mullion)
    float mullionX = winX + winW / 2;
    using var mullionPen = new Pen(frameColor, thick);
    g.DrawLine(mullionPen, mullionX, winY + thick / 2, mullionX, winY + winH - thick / 2);

    // Horizontal divider (transom bar, upper third)
    float transomY = winY + winH * 0.42f;
    g.DrawLine(mullionPen, winX + thick / 2, transomY, winX + winW - thick / 2, transomY);

    // === Glass panes (4 panes) ===
    float paneInset = thick / 2 + s * 0.02f;
    float pLeftX = winX + paneInset;
    float pRightX = mullionX + paneInset;
    float pTopY = winY + paneInset;
    float pMidY = transomY + paneInset;
    float paneW = (winW / 2) - paneInset * 2 + thick / 2;
    float paneTopH = (transomY - winY) - paneInset * 2 + thick / 2;
    float paneBotH = (winY + winH - transomY) - paneInset * 2 + thick / 2;

    using var paneBrush = new SolidBrush(paneColor);

    // Top-left pane
    g.FillRectangle(paneBrush, pLeftX, pTopY, paneW, paneTopH);
    // Top-right pane
    g.FillRectangle(paneBrush, pRightX, pTopY, paneW, paneTopH);
    // Bottom-left pane
    g.FillRectangle(paneBrush, pLeftX, pMidY, paneW, paneBotH);
    // Bottom-right pane
    g.FillRectangle(paneBrush, pRightX, pMidY, paneW, paneBotH);

    // === Glass reflections (diagonal shine on each pane) ===
    using var shinePen = new Pen(paneShine, Math.Max(1f, s * 0.025f));
    shinePen.StartCap = LineCap.Round;
    shinePen.EndCap = LineCap.Round;

    // Shine on top-left pane
    float shineOff = s * 0.04f;
    g.DrawLine(shinePen,
        pLeftX + shineOff, pTopY + paneTopH * 0.3f,
        pLeftX + paneW * 0.35f, pTopY + shineOff);

    // Shine on top-right pane
    g.DrawLine(shinePen,
        pRightX + shineOff, pTopY + paneTopH * 0.3f,
        pRightX + paneW * 0.35f, pTopY + shineOff);

    // === Arch detail at top (optional, for larger sizes) ===
    if (size >= 32)
    {
        // Small decorative keystone / arch hint at top center
        float archW = s * 0.08f;
        float archH = s * 0.04f;
        using var archPen = new Pen(frameLight, Math.Max(1f, thick * 0.6f));
        g.DrawLine(archPen,
            mullionX - archW, winY,
            mullionX, winY - archH);
        g.DrawLine(archPen,
            mullionX, winY - archH,
            mullionX + archW, winY);
    }

    // === Motion lines (being thrown!) ===
    float lineStartX = winX - s * 0.02f;
    float lineLen = s * 0.08f;
    using var motionPen = new Pen(Color.FromArgb(120, frameColor), Math.Max(1f, thick * 0.5f));
    motionPen.StartCap = LineCap.Round;
    motionPen.EndCap = LineCap.Round;

    // Three motion lines on the left side
    float lineY1 = winY + winH * 0.25f;
    float lineY2 = winY + winH * 0.50f;
    float lineY3 = winY + winH * 0.75f;
    g.DrawLine(motionPen, lineStartX, lineY1, lineStartX - lineLen, lineY1);
    g.DrawLine(motionPen, lineStartX - s * 0.01f, lineY2, lineStartX - lineLen * 1.3f, lineY2);
    g.DrawLine(motionPen, lineStartX, lineY3, lineStartX - lineLen, lineY3);

    string pngPath = Path.Combine(outputDir, $"icon_{size}.png");
    bmp.Save(pngPath, ImageFormat.Png);
    pngPaths.Add(pngPath);
    Console.WriteLine($"Generated {size}x{size} PNG");
}

// Build .ico file
string icoPath = Path.Combine(outputDir, "icon.ico");
using (var icoStream = new FileStream(icoPath, FileMode.Create))
{
    var header = new byte[6];
    header[2] = 1;
    BitConverter.GetBytes((short)sizes.Length).CopyTo(header, 4);
    icoStream.Write(header);

    int dataOffset = 6 + sizes.Length * 16;
    var pngDatas = new List<byte[]>();

    foreach (string pngPath in pngPaths)
        pngDatas.Add(File.ReadAllBytes(pngPath));

    for (int i = 0; i < sizes.Length; i++)
    {
        var entry = new byte[16];
        entry[0] = sizes[i] < 256 ? (byte)sizes[i] : (byte)0;
        entry[1] = sizes[i] < 256 ? (byte)sizes[i] : (byte)0;
        BitConverter.GetBytes((short)1).CopyTo(entry, 4);
        BitConverter.GetBytes((short)32).CopyTo(entry, 6);
        BitConverter.GetBytes(pngDatas[i].Length).CopyTo(entry, 8);
        BitConverter.GetBytes(dataOffset).CopyTo(entry, 12);
        icoStream.Write(entry);
        dataOffset += pngDatas[i].Length;
    }

    foreach (var pngData in pngDatas)
        icoStream.Write(pngData);
}

Console.WriteLine($"\nIcon saved to: {icoPath}");

foreach (string pngPath in pngPaths)
    File.Delete(pngPath);

Console.WriteLine("Done!");

static GraphicsPath RoundedRect(float x, float y, float w, float h, float r)
{
    var path = new GraphicsPath();
    if (r <= 0)
    {
        path.AddRectangle(new RectangleF(x, y, w, h));
        return path;
    }
    float d = r * 2;
    path.AddArc(x, y, d, d, 180, 90);
    path.AddArc(x + w - d, y, d, d, 270, 90);
    path.AddArc(x + w - d, y + h - d, d, d, 0, 90);
    path.AddArc(x, y + h - d, d, d, 90, 90);
    path.CloseFigure();
    return path;
}
