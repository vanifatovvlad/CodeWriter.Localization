using System.Text;

namespace CodeWriter.Localization
{
    static class StringUtils
    {
        private static StringBuilder builder = new StringBuilder();

        public static string Replace(string line, object[] args)
        {
            if (string.IsNullOrEmpty(line))
                return string.Empty;

            if (args.Length == 0)
                return line;

            builder.Length = 0;

            string key;
            int prev = 0, len = line.Length, start, end;
            while (prev < len && (start = line.IndexOf('<', prev)) != -1)
            {
                builder.Append(line, prev, start - prev);

                for (int i = 0; i < args.Length; i += 2)
                {
                    if ((key = args[i] as string) != null &&
                        (end = start + key.Length + 1) < len &&
                        (line[end] == '>') &&
                        (string.Compare(line, start + 1, key, 0, key.Length) == 0))
                    {
                        builder.Append(args[i + 1]);
                        prev = end + 1;
                        goto replaced;
                    }
                }

                builder.Append('<');
                prev = start + 1;

                replaced:;
            }

            if (prev < line.Length)
                builder.Append(line, prev, line.Length - prev);

            return builder.ToString();
        }
    }
}