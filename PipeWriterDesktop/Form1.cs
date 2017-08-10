using netDxf;
using PipeWriter;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;


namespace PipeWriterDesktop
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //DialogResult res = openFileDialog1.ShowDialog();
            //if (res == DialogResult.OK)
            //{
            //    string file = openFileDialog1.FileName;
            //    try
            //    {
            //        string text = file.ReadAllText(file);
            //        size = text.Length;
            //    }
            //    catch (IOException)
            //    {
            //    }
            //}
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //DxfFile file = new DxfFile();
            //MergeContent content = new MergeContent()
            //{
            //    FileName = "line.dxf",
            //    StartPointX = 769,
            //    StartPointY = 477,
            //    Rotation = 0
            //};
            //List<MergeContent> list = new List<MergeContent>();
            //list.Add(content);
            //file.GenerateDXF("ProductDxfFile.dxf", 461.47, 493.37, 769.98, 493.37, list);

            #region new

            //var value = Math.Cos(60 * Math.PI/180);
            DialogResult res = openFileDialog1.ShowDialog();
            string filename = "";
            if(res== DialogResult.OK)
            {
                filename = openFileDialog1.FileName;
                //string filename1 = "C:\\Users\\krishna\\Desktop\\InputFiles\\" + filename;
                Product p = new Product();
                XmlDocument xdoc = new XmlDocument();
                //xdoc.Load(@"C:\Users\krishna\Desktop\InputFiles\demo1.xml");
                xdoc.Load(filename);

                XmlSerializer serializer = new XmlSerializer(typeof(Product));
                using (StringReader reader = new StringReader(xdoc.InnerXml))
                {

                    p = (Product)(serializer.Deserialize(reader));
                }
                var x = p;
                //DxfFile service = new DxfFile();
                DxfFile.GenerateDXF(p, "newProd.dxf", "E:\\krishna");
                //service.GenerateDXFBLocks(p,"test8", "E:\\krishna");
            }
            
            #endregion

            #region old

            /*

             
            Product prod = new Product();
            Parts parts = new Parts();
            //arcs
            Arc1 arc1 = new Arc1();
            arc1.Angle = 180;
            arc1.EntityID = 2;
            arc1.IsClockwise = true;
            arc1.Radius = 100;

            Arc1 arc2 = new Arc1();
            arc2.Angle = 90;
            arc2.EntityID = 4;
            arc2.IsClockwise = false;
            arc2.Radius = 110;


            //Arc1 arc3 = new Arc1();
            //arc3.Angle = 90;
            //arc3.EntityID = 3;
            //arc3.IsClockwise = true;
            //arc3.Radius = 100;

            //straight line 
            Straight straight1 = new Straight();
            straight1.EntityID = 1;
            straight1.Length = 100;

            Straight straight2 = new Straight();
            straight2.EntityID = 3;
            straight2.Length = 20;

            //Straight straight4 = new Straight();
            //straight4.EntityID = 4;
            //straight4.Length = 100;

            List<Arc1> listarc = new List<Arc1>();
            listarc.Add(arc1);
            listarc.Add(arc2);
            //listarc.Add(arc3);

            List<Straight> listStraight = new List<Straight>();
            listStraight.Add(straight1);
            listStraight.Add(straight2);
            //listStraight.Add(straight4);

            parts.Arcs = listarc;
            parts.Lines = listStraight;

            prod.Customer = "Javra Software Nepal";
            prod.FontSizeDimension = 3;
            prod.OrderText = "Custom pipe for exhaust";
            prod.ProductWidth = 20;
            prod.ShowDimensionLines = true;
            prod.PartList = parts;


            //call service
            //DxfFile serv = new DxfFile();
            DxfFile.Generate(prod, "test", "E:\\krishna");

            //DxfFile service = new DxfFile();
            //service.GenerateDXF(prod, "test1", "E:\\krishna");


            
            */
            #endregion

        }

        private void button3_Click(object sender, EventArgs e)
        {
            //DxfFile file = new DxfFile();
            //file.RotateEntity();
            //E:\Proj\PipeWriter\acmecad
            //DxfDocument doc = DxfDocument.Load(@"E:\krishna\Line3.dxf");
            //netDxf.Entities.Line l = new netDxf.Entities.Line();
            //l.StartPoint = new Vector3(0, 0, 0);
            //l.EndPoint = new Vector3(100, 0, 0);
            //l.Color = new AciColor(Color.Ivory); //AciColor.Default;
            
            //////l.XData.Add(new XData(new netDxf.Tables.ApplicationRegistry("testKrishna")));
 
            //////netDxf.Tables.Layer layer = new netDxf.Tables.Layer("Layer-1");
            //////l.Layer = layer;
            //doc.AddEntity(l);
            //doc.Save(@"E:\krishna\testtest.dxf");
            //DxfFile.Convert(@"E:\Proj\PipeWriter\acmecad\AcmeCADConverter.exe", @"E:\krishna\test1.dxf", FileFormat.SVG);
            DxfFile.Convert(@"E:\Proj\PipeWriter\acmecad\AcmeCADConverter.exe", @"E:\krishna\Product test.dxf", FileFormat.SVG, @"E:\krishna\SVGS");
            //doc.RemoveEntity(l);
            //doc.Save(@"E:\krishna\testtest.dxf");
        }
    }
}
