using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using IO.Milvus.Utils;
using IO.Milvus.Workbench.Models;
using System;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace IO.Milvus.Workbench.DocumentViews
{
    /// <summary>
    /// CollectionPage.xaml 的交互逻辑
    /// </summary>
    public partial class CollectionPage : Page
    {
        private CompletionWindow _completionWindow;
        private DrawingImage _fieldImage;

        public CollectionPage()
        {
            LoadHighlighting();
            InitializeComponent();

            _fieldImage = Application.Current.FindResource("field") as DrawingImage;
            textEditor.TextArea.TextEntered += TextArea_TextEntered;
        }

        private void TextArea_TextEntered(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            var collectionNode = DataContext as CollectionNode;
            if (collectionNode == null || collectionNode.Fields.IsEmpty())
            {
                return;
            }

            var fields = collectionNode.Fields.Where(p => p.Name.StartsWith(e.Text));
            if (fields.Any())
            {
                _completionWindow = new CompletionWindow(textEditor.TextArea);
                var completionData = _completionWindow.CompletionList.CompletionData;
                foreach (var field in fields)
                {
                    completionData.Add(new FieldCompletionData(field,_fieldImage));                    
                }
                _completionWindow.Show();
                _completionWindow.Closed += _completionWindow_Closed;
            }
            else
            {
                if (e.Text == " ")
                {
                    _completionWindow = new CompletionWindow(textEditor.TextArea);
                    var completionData = _completionWindow.CompletionList.CompletionData;
                    foreach (var keyword in KeyWordCompletionData.KeyWords())
                    {
                        completionData.Add(keyword);
                    }
                    _completionWindow.Show();
                    _completionWindow.Closed += _completionWindow_Closed;
                }
            }
        }

        private void _completionWindow_Closed(object sender, EventArgs e)
        {
            _completionWindow.Closed -= _completionWindow_Closed;
            _completionWindow = null;
        }

        public void LoadHighlighting()
        {
            var url = Assembly.GetExecutingAssembly().GetName().Name + ".Assets.Editor.Query.xshd";
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(url))
            {
                using (var reader = new System.Xml.XmlTextReader(stream))
                {
                    var sqlDefinition = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                    HighlightingManager.Instance.RegisterHighlighting("milvusquery", new string[] { ".mq" }, sqlDefinition);
                }
            }
        }
    }
}
