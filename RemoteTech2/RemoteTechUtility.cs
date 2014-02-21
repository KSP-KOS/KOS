using System;
using kOS.Context;

namespace kOS.RemoteTech2
{
    public static class RemoteTechUtility
    {
        public static void DrawProgressBar(this IExecutionContext context, double elapsed, double total, string text)
        {
            var bars = (int)((ExecutionContext.COLUMNS) * elapsed / total);
            var spaces = (ExecutionContext.COLUMNS) - bars;
            var time = new DateTime(TimeSpan.FromSeconds(total - elapsed + 0.5).Ticks).ToString("H:mm:ss");
            context.Put(text + new string(' ', ExecutionContext.COLUMNS - time.Length - text.Length) + time, 0, ExecutionContext.ROWS - 2);
            context.Put(new string('|', bars) + new string(' ', spaces), 0, ExecutionContext.ROWS - 1);
        }
    }
}
