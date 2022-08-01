using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace IO.Milvus.Workbench.DocumentViews
{
    public class KeyWordCompletionData : ICompletionData
    {
        private static List<ICompletionData> _keyWords;

        public KeyWordCompletionData(string text)
        {
            Text = text;
        }

        public static List<ICompletionData> KeyWords()
        {
            return _keyWords ?? (_keyWords = new List<ICompletionData>()
            {
                new KeyWordCompletionData("<"),
                new KeyWordCompletionData("<="),
                new KeyWordCompletionData(">="),
                new KeyWordCompletionData("=="),
                new KeyWordCompletionData("!="),
                new KeyWordCompletionData("in"),
                new KeyWordCompletionData("||"),
                new KeyWordCompletionData("&&"),
            });
        }

        public ImageSource Image { get; }

        public string Text { get; }

        public object Content => Text;

        public object Description { get; }

        public double Priority { get; }

        public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
        {
            textArea.Document.Replace(completionSegment, Text);
        }
    }
}
