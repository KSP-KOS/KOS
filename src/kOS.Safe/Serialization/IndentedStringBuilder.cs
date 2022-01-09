using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;

namespace kOS.Safe
{
    public interface IIndentedStringBuilderIndent : IDisposable { }

    public class IndentedStringBuilder
    {
        private class IndentedStringBuilderIndent : IIndentedStringBuilderIndent
        {
            private IndentedStringBuilder stringBuilder;
            private int indentSize;
            private bool disposed = false;
            public IndentedStringBuilderIndent(IndentedStringBuilder sb, int indentSize)
            {
                this.stringBuilder = sb;
                this.indentSize = indentSize;

                sb.indentation += indentSize;
            }

            public void Dispose()
            {
                if (this.disposed)
                    return;
                stringBuilder.indentation -= indentSize;
                this.disposed = true;
            }
        }

        protected StringBuilder stringBuilder;
        private int indentation;
        private bool startOfLine;

        public IndentedStringBuilder()
        {
            stringBuilder = new StringBuilder();
            startOfLine = true;
        }

        public IIndentedStringBuilderIndent Indent(int size = 2)
        {
            return new IndentedStringBuilderIndent(this, size);
        }

        protected virtual void AppendSingleLineOnly(string line)
        {
            if (startOfLine)
            {
                for (int i = 0; i < indentation; i++)
                    stringBuilder.Append(' ');
                startOfLine = false;
            }

            stringBuilder.Append(line);

        }
        public void Append(string value)
        {
            var lines = value.Split('\n');
            AppendSingleLineOnly(lines[0]);
            
            foreach (var line in lines.Skip(1)) {
                AppendLine();
                AppendSingleLineOnly(line);
            }
        }
        public virtual void AppendLine()
        {
            stringBuilder.AppendLine();
            startOfLine = true;
        }

        public override string ToString()
        {
            return stringBuilder.ToString();
        }
    }

    public class SingleLineIndentedStringBuilder : IndentedStringBuilder
    {
        private bool eol = false;
        public bool IsSingleLine { get { return !eol; } }

        protected override void AppendSingleLineOnly(string line)
        {
            if (eol)
                return;
            stringBuilder.Append(line);
        }

        public override void AppendLine()
        {
            eol = true;
        }
    }
}

