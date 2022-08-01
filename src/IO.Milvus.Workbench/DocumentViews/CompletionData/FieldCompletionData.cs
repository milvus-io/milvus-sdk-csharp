using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using IO.Milvus.Workbench.Models;
using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace IO.Milvus.Workbench.DocumentViews
{
    public class FieldCompletionData : ICompletionData
    {
        public FieldCompletionData(FieldModel field, DrawingImage _fieldImage)
        {
            Text = field.Name;
            Image = _fieldImage;
            Description = field.Description;
        }

        public ImageSource Image { get; }

        public string Text { get; }

        public object Content => Text;

        public object Description { get; }

        public double Priority { get; }

        public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
        {
            //int length = 0;
            //int offset = completionSegment.Offset;
            //for (int i = textArea.Document.Text.Length -1 ; i >= 0; i--)
            //{
            //    if (Text.StartsWith(textArea.Document.Text.Remove(i)))
            //    {
            //        length++;
            //        offset = i;
            //    }
            //    else
            //    {
            //        break;
            //    }
            //}

            textArea.Document.Replace(completionSegment.Offset-1,completionSegment.Length+1,Text);
        }
    }
}
