using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YGraphML
{
    //this was added by hand, because xsd.exe couldn't seem to figure out the yEd extensions to the data element.
    //(or I don't know how to use xsd.exe... that's a possibility too)
    public partial class datatype
    {
        private object[] itemsField;

        [System.Xml.Serialization.XmlElementAttribute("ShapeNode", typeof(ShapeNodetype), Namespace = "http://www.yworks.com/xml/graphml")]
        [System.Xml.Serialization.XmlElementAttribute("ArcEdge", typeof(ArcEdgetype), Namespace = "http://www.yworks.com/xml/graphml")]
        [System.Xml.Serialization.XmlElementAttribute("SplineEdge", typeof(Edgetype), Namespace = "http://www.yworks.com/xml/graphml")]
        public object[] Items
        {
            get
            {
                return this.itemsField;
            }
            set
            {
                this.itemsField = value;
            }
        }
    }

    public partial class nodetype
    {
        public override string ToString()
        {
            return string.Format("{0}: {1}", id, this.GetLabelText());
        }
    }

    public partial class edgetype
    {
        public override string ToString()
        {
            return string.Format("{0} = {1} -> {2}: {3}", id, source, target, this.GetLabelText());
        }
    }
}
