using System;
using System.Diagnostics;
using System.IO;

using NavisworksToolkit.Core;
using NavisworksToolkit.Shared;

namespace NavisworksToolkit.Core
{
    /// <summary>
    /// Logger de performance leve para diagnosticar ONDE o tempo é gasto na geração de
    /// viewpoints. Grava em %TEMP%\AutoViewTool_perf.log com o tempo (ms) de cada fase.
    ///
    /// TEMPORÁRIO: instrumentação para localizar o gargalo do lote. Desligável via
    /// <see cref="Enabled"/>; remover quando o ajuste de performance estiver fechado.
    /// </summary>
    public static class PerfLog
    {
        public static bool Enabled = true;

        public static readonly string LogPath =
            Path.Combine(Path.GetTempPath(), "AutoViewTool_perf.log");

        private static readonly object Gate = new object();

        /// <summary>Zera o log e marca o início de uma execução (chamar no começo do lote).</summary>
        public static void Reset()
        {
            if (!Enabled) return;
            try
            {
                lock (Gate)
                    File.WriteAllText(LogPath,
                        $"=== AutoViewTool perf run {DateTime.Now:yyyy-MM-dd HH:mm:ss} ==={Environment.NewLine}");
            }
            catch { /* logging nunca deve quebrar o fluxo */ }
        }

        public static void Info(string message)
        {
            Write($"{DateTime.Now:HH:mm:ss.fff}  {message}");
        }

        public static void Mark(string phase, long ms)
        {
            Write($"{DateTime.Now:HH:mm:ss.fff}  {phase,-28} {ms,9} ms");
        }

        /// <summary>Cronometra uma fase que retorna valor.</summary>
        public static T Time<T>(string phase, Func<T> action)
        {
            if (!Enabled) return action();
            var sw = Stopwatch.StartNew();
            try { return action(); }
            finally { sw.Stop(); Mark(phase, sw.ElapsedMilliseconds); }
        }

        /// <summary>Cronometra uma fase sem retorno.</summary>
        public static void TimeVoid(string phase, Action action)
        {
            if (!Enabled) { action(); return; }
            var sw = Stopwatch.StartNew();
            try { action(); }
            finally { sw.Stop(); Mark(phase, sw.ElapsedMilliseconds); }
        }

        private static void Write(string line)
        {
            if (!Enabled) return;
            try
            {
                lock (Gate)
                    File.AppendAllText(LogPath, line + Environment.NewLine);
            }
            catch { /* idem */ }
        }
    }
}
