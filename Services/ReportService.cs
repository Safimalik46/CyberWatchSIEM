using System.Globalization;
using CyberWatchSIEM.Database;
using CyberWatchSIEM.Models;
using CyberWatchSIEM.Repositories;
using iText.IO.Font.Constants;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace CyberWatchSIEM.Services;

public class ReportService
{
    private readonly LogRepository _logs;
    private readonly AlertRepository _alerts;
    private readonly IncidentRepository _incidents;
    private readonly ThreatIntelRepository _threatIntel;

    public ReportService(ApplicationDbContext context)
    {
        _logs = new LogRepository(context);
        _alerts = new AlertRepository(context);
        _incidents = new IncidentRepository(context);
        _threatIntel = new ThreatIntelRepository(context);
        ExcelPackage.License.SetNonCommercialPersonal("CyberWatch SIEM");
    }

    public async Task ExportSecurityReportPdfAsync(string filePath)
    {
        var logs = await _logs.GetAllAsync();
        var alerts = await _alerts.GetAllAsync();

        await using var writer = new PdfWriter(filePath);
        using var pdf = new PdfDocument(writer);
        var document = new Document(pdf);

        document.Add(new Paragraph("CyberWatch SIEM - Security Report")
            .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD))
            .SetFontSize(18));
        document.Add(new Paragraph($"Generated: {DateTime.Now:G}"));
        document.Add(new Paragraph($"Total Logs: {logs.Count}"));
        document.Add(new Paragraph($"Total Alerts: {alerts.Count}"));
        document.Add(new Paragraph($"Critical Alerts: {alerts.Count(a => a.Severity == "Critical")}"));

        document.Add(new Paragraph("\nTop Event Types:")
            .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD)));
        foreach (var group in logs.GroupBy(l => l.EventType).OrderByDescending(g => g.Count()).Take(5))
            document.Add(new Paragraph($"- {group.Key}: {group.Count()}"));

        document.Close();
    }

    public async Task ExportIncidentReportExcelAsync(string filePath)
    {
        var incidents = await _incidents.GetAllAsync();
        using var package = new ExcelPackage();
        var sheet = package.Workbook.Worksheets.Add("Incidents");

        sheet.Cells[1, 1].Value = "Code";
        sheet.Cells[1, 2].Value = "Alert Name";
        sheet.Cells[1, 3].Value = "Severity";
        sheet.Cells[1, 4].Value = "Status";
        sheet.Cells[1, 5].Value = "Assigned To";
        sheet.Cells[1, 6].Value = "Created";

        var row = 2;
        foreach (var i in incidents)
        {
            sheet.Cells[row, 1].Value = i.IncidentCode;
            sheet.Cells[row, 2].Value = i.AlertName;
            sheet.Cells[row, 3].Value = i.Severity;
            sheet.Cells[row, 4].Value = i.Status;
            sheet.Cells[row, 5].Value = i.AssignedTo;
            sheet.Cells[row, 6].Value = i.CreatedAt.ToString("g");
            row++;
        }

        sheet.Cells.AutoFitColumns();
        await package.SaveAsAsync(new FileInfo(filePath));
    }

    public async Task ExportAlertReportCsvAsync(string filePath)
    {
        var alerts = await _alerts.GetAllAsync();
        await using var writer = new StreamWriter(filePath);
        await writer.WriteLineAsync("AlertCode,Severity,Time,Description,Status,AlertType");
        foreach (var a in alerts)
        {
            await writer.WriteLineAsync(
                $"\"{a.AlertCode}\",\"{a.Severity}\",\"{a.Time:O}\",\"{a.Description.Replace("\"", "'")}\",\"{a.Status}\",\"{a.AlertType}\"");
        }
    }

    public async Task ExportThreatIntelReportPdfAsync(string filePath)
    {
        var ips = await _threatIntel.GetAllIPsAsync();
        var domains = await _threatIntel.GetAllDomainsAsync();
        var sigs = await _threatIntel.GetAllSignaturesAsync();

        await using var writer = new PdfWriter(filePath);
        using var pdf = new PdfDocument(writer);
        var document = new Document(pdf);

        document.Add(new Paragraph("Threat Intelligence Report")
            .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD))
            .SetFontSize(18));
        document.Add(new Paragraph($"Malicious IPs: {ips.Count}"));
        document.Add(new Paragraph($"Suspicious Domains: {domains.Count}"));
        document.Add(new Paragraph($"Malware Signatures: {sigs.Count}"));

        document.Close();
    }
}

public class NotificationService
{
    public event Action<string, string, string>? NotificationRaised;

    public void Notify(string title, string message, string severity)
    {
        NotificationRaised?.Invoke(title, message, severity);
    }

    public void NotifyAlert(Alert alert)
    {
        Notify(alert.AlertType, alert.Description, alert.Severity);
    }
}

public class GlobalSearchService
{
    private readonly ApplicationDbContext _context;

    public GlobalSearchService(ApplicationDbContext context) => _context = context;

    public async Task<List<SearchResult>> SearchAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];

        var q = query.ToLower();
        var results = new List<SearchResult>();

        var logs = await _context.Logs
            .Where(l => l.Message.ToLower().Contains(q) || l.EventId.ToLower().Contains(q))
            .Take(10).ToListAsync();
        results.AddRange(logs.Select(l => new SearchResult("Log", l.EventId, l.Message)));

        var alerts = await _context.Alerts
            .Where(a => a.Description.ToLower().Contains(q) || a.AlertCode.ToLower().Contains(q))
            .Take(10).ToListAsync();
        results.AddRange(alerts.Select(a => new SearchResult("Alert", a.AlertCode, a.Description)));

        var incidents = await _context.Incidents
            .Where(i => i.AlertName.ToLower().Contains(q))
            .Take(10).ToListAsync();
        results.AddRange(incidents.Select(i => new SearchResult("Incident", i.IncidentCode, i.AlertName)));

        return results;
    }
}

public record SearchResult(string Type, string Title, string Description);
