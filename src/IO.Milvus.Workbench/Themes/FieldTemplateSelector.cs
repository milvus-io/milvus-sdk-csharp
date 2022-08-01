using IO.Milvus.Workbench.Models.Fields;
using System.Windows;
using System.Windows.Controls;

namespace IO.Milvus.Workbench.Themes
{
    public class FieldTemplateSelector : DataTemplateSelector
    {
        public DataTemplate DefaultFieldTemplate { get; set; }

        public DataTemplate PrimaryKeyFieldTemplate { get; set; }

        public DataTemplate VectorFieldTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item == null)
            {
                return null;
            }

            if (item is DefaultField)
            {
                return DefaultFieldTemplate;
            }
            else if (item is PrimaryField)
            {
                return PrimaryKeyFieldTemplate;
            }
            else if (item is VectorField)
            {
                return VectorFieldTemplate;
            }

            return null;
        }
    }
}
