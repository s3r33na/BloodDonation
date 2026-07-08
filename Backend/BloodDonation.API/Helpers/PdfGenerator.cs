using PdfSharp.Pdf;
using PdfSharp.Drawing;
using System;
using System.Collections.Generic;
using System.IO;

namespace BloodDonation.API.Helpers
{
    public static class PdfGenerator
    {
        public static byte[] GenerateTablePdf(string title, string[] headers, List<string[]> rows)
        {
            using (var document = new PdfDocument())
            {
                document.Info.Title = title;
                
                var page = document.AddPage();
                page.Size = PdfSharp.PageSize.A4;
                page.Orientation = PdfSharp.PageOrientation.Portrait;
                
                var gfx = XGraphics.FromPdfPage(page);
                
                // Fonts
                var titleFont = new XFont("Arial", 18, XFontStyleEx.Bold);
                var headerFont = new XFont("Arial", 10, XFontStyleEx.Bold);
                var regularFont = new XFont("Arial", 9, XFontStyleEx.Regular);
                var footerFont = new XFont("Arial", 8, XFontStyleEx.Italic);
                
                // Draw Title
                gfx.DrawString(title, titleFont, XBrushes.DarkRed, new XPoint(40, 50));
                gfx.DrawString($"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss} (Luminus Giving)", footerFont, XBrushes.Gray, new XPoint(40, 70));
                
                // Draw Line
                gfx.DrawLine(new XPen(XColors.DarkRed, 2), 40, 80, page.Width - 40, 80);
                
                double yPoint = 105;
                double margin = 40;
                double tableWidth = page.Width - (margin * 2);
                double colWidth = tableWidth / headers.Length;
                
                // Draw Headers
                for (int i = 0; i < headers.Length; i++)
                {
                    var rect = new XRect(margin + (i * colWidth), yPoint - 15, colWidth, 20);
                    gfx.DrawRectangle(new XPen(XColors.LightGray, 1), XBrushes.LightGray, rect);
                    gfx.DrawString(headers[i], headerFont, XBrushes.Black, new XRect(rect.X + 4, rect.Y + 2, rect.Width - 8, rect.Height), XStringFormats.TopLeft);
                }
                
                yPoint += 10;
                
                // Draw Rows
                int rowCount = 0;
                foreach (var row in rows)
                {
                    // If table exceeds page height, add a new page (limit of page height)
                    if (yPoint > page.Height - 60)
                    {
                        page = document.AddPage();
                        page.Size = PdfSharp.PageSize.A4;
                        gfx = XGraphics.FromPdfPage(page);
                        
                        gfx.DrawString($"{title} (Continued)", titleFont, XBrushes.DarkRed, new XPoint(40, 50));
                        gfx.DrawLine(new XPen(XColors.DarkRed, 2), 40, 65, page.Width - 40, 65);
                        
                        yPoint = 90;
                        
                        // Draw Headers again
                        for (int i = 0; i < headers.Length; i++)
                        {
                            var rect = new XRect(margin + (i * colWidth), yPoint - 15, colWidth, 20);
                            gfx.DrawRectangle(new XPen(XColors.LightGray, 1), XBrushes.LightGray, rect);
                            gfx.DrawString(headers[i], headerFont, XBrushes.Black, new XRect(rect.X + 4, rect.Y + 2, rect.Width - 8, rect.Height), XStringFormats.TopLeft);
                        }
                        yPoint += 10;
                    }
                    
                    var bgBrush = (rowCount % 2 == 0) ? XBrushes.White : XBrushes.FloralWhite;
                    
                    for (int i = 0; i < headers.Length; i++)
                    {
                        var val = i < row.Length ? row[i] ?? "" : "";
                        var rect = new XRect(margin + (i * colWidth), yPoint - 15, colWidth, 18);
                        
                        // Draw cell border and background
                        gfx.DrawRectangle(new XPen(XColors.LightGray, 0.5), bgBrush, rect);
                        
                        // Draw text (clip to fit)
                        var strFormat = XStringFormats.TopLeft;
                        gfx.DrawString(Truncate(val, 25), regularFont, XBrushes.Black, new XRect(rect.X + 4, rect.Y + 2, rect.Width - 8, rect.Height), strFormat);
                    }
                    
                    yPoint += 18;
                    rowCount++;
                }
                
                // Draw Footer
                gfx.DrawString("Luminus Giving Initiative - National Donation Registry - Jordan", footerFont, XBrushes.Gray, new XPoint(40, page.Height - 30));
                
                using (var ms = new MemoryStream())
                {
                    document.Save(ms);
                    return ms.ToArray();
                }
            }
        }
        
        private static string Truncate(string val, int maxLen)
        {
            if (string.IsNullOrEmpty(val)) return "";
            return val.Length <= maxLen ? val : val.Substring(0, maxLen - 3) + "...";
        }
    }
}
