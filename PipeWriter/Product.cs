using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace PipeWriter
{
    [Serializable]
    public class Parts
    {
        [XmlArray("lines")]
        [XmlArrayItem("straight")]
        public  List<Straight> Lines { get; set; }
        [XmlArray("arcs")]
        [XmlArrayItem("arc")]
        public List<Arc1> Arcs { get; set; }
    }

    [Serializable]
    public class Straight
    {
        [XmlElement("sequence_num")]
        public int EntityID { get; set; }
        [XmlElement("length")]
        public double Length { get; set; }
    }

    [Serializable]
    public class Arc1
    {
        [XmlElement("sequence_num")]
        public int EntityID { get; set; }

        [XmlElement("radius")]
        public double Radius { get; set; }
        [XmlElement("angle")]
        public int Angle { get; set; }

        [XmlElement("is_clockwise")]
        public bool IsClockwise { get; set; }
    }

    [Serializable]
    [XmlRoot("product")]

    public class Product
    {
        [XmlElement("product_width")]
        public double ProductWidth { get; set; }
        [XmlElement("show_dimension_lines")]
        public bool ShowDimensionLines { get; set; }
        [XmlElement("font_size_dimension")]
        public double FontSizeDimension { get; set; }
        [XmlElement("order_text")]
        public  string OrderText { get; set; }
        [XmlElement("customer")]
        public string Customer { get; set; }
        //[XmlAttribute("product_name")]
        //public string ProductName { get; set; }

        [XmlElement("part_list")]
        public Parts PartList { get; set; }
    }
}
