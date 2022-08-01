using IO.Milvus.Param.Collection;
using System;

namespace IO.Milvus.Workbench.Models.Fields
{
    public abstract class Field
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public abstract FieldType ToFieldType();
    }
}
