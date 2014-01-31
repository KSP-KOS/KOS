using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace kOS
{
    public static class RTUtil
    {
        public static void DrawProgressBar(this ExecutionContext context, double elapsed, double total, String text)
        {
            int bars = (int)((ExecutionContext.COLUMNS) * elapsed / total);
            int spaces = (ExecutionContext.COLUMNS) - bars;
            var time = new DateTime(System.TimeSpan.FromSeconds(total - elapsed + 0.5).Ticks).ToString("H:mm:ss");
            context.Put(text + new String(' ', ExecutionContext.COLUMNS - time.Length - text.Length) + time, 0, ExecutionContext.ROWS - 2);
            context.Put(new String('|', bars) + new String(' ', spaces), 0, ExecutionContext.ROWS - 1);
        }
    }
}
