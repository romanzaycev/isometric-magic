using System.Text;

namespace IsometricMagic.RuntimeEditor
{
    internal static class RuntimeEditorPage
    {
        private static readonly Lazy<string> HtmlContent = new(LoadHtml);

        public static string Html => HtmlContent.Value;

        private static string LoadHtml()
        {
            var assembly = typeof(RuntimeEditorPage).Assembly;
            var resourceName = assembly
                .GetManifestResourceNames()
                .FirstOrDefault(name => name.EndsWith("Web.dist.index.html", StringComparison.Ordinal));

            if (string.IsNullOrWhiteSpace(resourceName))
            {
                return "<!doctype html><html><body><h1>Runtime Editor</h1><p>Missing embedded SPA.</p></body></html>";
            }

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                return "<!doctype html><html><body><h1>Runtime Editor</h1><p>Missing embedded SPA stream.</p></body></html>";
            }

            using var reader = new StreamReader(stream, Encoding.UTF8);
            return reader.ReadToEnd();
        }
    }
}
