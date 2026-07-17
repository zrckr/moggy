using System.Globalization;
using Serilog;
using Serilog.Events;
using Serilog.Templates;
using Serilog.Templates.Themes;

namespace Moggy;

internal static class Logging
{
    private const string LogTemplate =
        "({UtcDateTime(@t):HH:mm:ss.fff}) {@l:u4} [{Substring(SourceContext, LastIndexOf(SourceContext, '.') + 1)}] {@m}\n{@x}";

#if DEBUG
    private static LogEventLevel Level { get; set; } = LogEventLevel.Debug;
#else
    private static LogEventLevel Level { get; set; } = LogEventLevel.Information;
#endif

    public static void Initialize()
    {
        var logFile = Path.Combine(AppContext.BaseDirectory,
            "Logs", $"[{DateTime.Now:yyyy-MM-ddTHH-mm-ss}] {Level} Log.txt");

        #region Clean old log files

        var directory = Path.GetDirectoryName(logFile)!;
        if (Directory.Exists(directory))
        {
            var cutoff = DateTime.Now.AddDays(-3);
            foreach (var file in Directory.GetFiles(directory, "*.txt"))
            {
                var name = Path.GetFileNameWithoutExtension(file);
                if (DateTime.TryParseExact(name[1..20], "yyyy-MM-ddTHH-mm-ss",
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out var fileDate) && fileDate < cutoff)
                {
                    File.Delete(file);
                }
            }
        }

        #endregion

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(Level)
            .WriteTo.Console(new ExpressionTemplate(LogTemplate, theme: TemplateTheme.Literate))
            .WriteTo.File(new ExpressionTemplate(LogTemplate), logFile)
            .CreateLogger();

        var logger = Log.ForContext<Game>();
        Foster.Framework.Log.SetCallbacks(
            message => logger.Information("{FosterMessage:l}", message.ToString()),
            message => logger.Warning("{FosterMessage:l}", message.ToString()),
            message => logger.Error("{FosterMessage:l}", message.ToString())
        );
    }
}
