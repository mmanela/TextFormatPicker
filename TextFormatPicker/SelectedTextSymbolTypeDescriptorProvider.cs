using System;
using System.ComponentModel;

namespace MattManela.TextFormatPicker
{
    /// <summary>
    /// A type description provider which will be registered with the TypeDescriptor.  This provider will be called
    /// whenever the properties are accessed.
    /// </summary>
    public class SelectedTextSymbolTypeDescriptorProvider : TypeDescriptionProvider
    {
        private readonly TypeDescriptionProvider baseProvider;

        public SelectedTextSymbolTypeDescriptorProvider(Type type)
        {
            baseProvider = TypeDescriptor.GetProvider(type);
        }


        public override ICustomTypeDescriptor GetTypeDescriptor(Type objectType, object instance)
        {
            return new SelectedTextSymbolTypeDescriptor(
                this, baseProvider.GetTypeDescriptor(objectType, instance), objectType, instance);
        }

        /// <summary>
        /// Our custom type provider will return this type descriptor which will filter the properties
        /// </summary>
        private class SelectedTextSymbolTypeDescriptor : CustomTypeDescriptor
        {
            private readonly Type objType;
            private readonly object instance;

            public SelectedTextSymbolTypeDescriptor(SelectedTextSymbolTypeDescriptorProvider provider, ICustomTypeDescriptor descriptor, Type objType, object instance)
                : base(descriptor)
            {
                this.objType = objType;
                this.instance = instance;
            }

            public override string GetClassName()
            {
                var val = objType.GetProperty("ClassificationName").GetValue(instance, null);
                return (val ?? "").ToString();
            }

            public override string GetComponentName()
            {
                return "Text Information";
            }
        }
    }
}