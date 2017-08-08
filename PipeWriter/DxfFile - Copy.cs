using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using netDxf;
using netDxf.Entities;
using netDxf.Tables;
using System.Xml;
using System.Xml.Linq;

namespace PipeWriter
{

    public class DxfFile
    {
        /// <summary>
        /// This method is used to create/Generate DXF file based on data provided under product detail.
        /// </summary>
        /// <param name="productDetails">Product Detail.</param>
        /// <param name="fileName">name of dxf file to be saved</param>
        /// <param name="outputFilePath"> location under where file to be saved</param>
        /// <remarks>
        /// If the file with same name already exists it will be replaced.
        /// </remarks>
        public static void GenerateDXF(Product productDetails, string fileName, string outputFilePath)
        {
            //call validation
            if (Validate(productDetails, fileName))
            {
                //arrance items according to sequence number. sequence number must be unique
                var sequenceItems = GetItemsSequence(productDetails, fileName);
                if (sequenceItems.Any())
                {
                    sequenceItems = sequenceItems.OrderBy(x => x).ToList();
                    DxfDocument dxf = new DxfDocument();
                    //create directory if not exists
                    if (!Directory.Exists(outputFilePath))
                        Directory.CreateDirectory(outputFilePath);

                    //define dimenstion value that can be assigned if dimension is defined for individual entityitems
                    var dimStyle = new DimensionStyle("dimStyle")
                    {
                        TextHeight = 3,
                        ArrowSize = 3,
                        DimSuffix = " mm",
                        TextOffset = 3 * 0.4,
                        DimLineColor = AciColor.Blue,
                        ExtLineColor = AciColor.Yellow
                    };

                    //define prevObject as null for first time.
                    var entityObjectCollection = new List<EntityObject>();
                    EntityObject previousObject = null;

                    for (int i = 0; i < sequenceItems.Count; i++)
                    {
                        var layer = AddLayer("Layer" + i);
                        var docLineEntity = productDetails.PartList.Lines.FirstOrDefault(x => x.EntityID == sequenceItems[i]);
                        if (docLineEntity != null)
                        {
                            var line = new Line();
                            var isClockWise = false;
                            if (previousObject != null)
                                if (previousObject.Type == EntityType.Arc)
                                    isClockWise = productDetails.PartList.Arcs.FirstOrDefault(x => x.EntityID == sequenceItems[i - 1]).IsClockwise;
                            line = DrawAndAttachLine(previousObject, isClockWise, docLineEntity.Length, productDetails.ProductWidth, layer);
                            //if(productDetails.startangle != 0 && i==0)
                            //    AddStartOrEnd(productDetails.startangle);
                            bool showWidth = false;
                            bool sAngleClockwise = false;
                            bool eAngleClockwise = false;
                            double sCutAngle = 0;
                            double eCutAngle = 0;
                            if (i == 0)
                                showWidth = true;
                            else
                                showWidth = false;
                            if (i == 0)
                            {
                                sCutAngle = productDetails.StartCutAngle;
                                eCutAngle = 0;
                                sAngleClockwise = productDetails.StartAngleClockWise;
                                eAngleClockwise = false;
                            }
                            else if (i == sequenceItems.Count - 1)
                            {
                                sCutAngle = 0;
                                eCutAngle = productDetails.EndCutAngle;
                                sAngleClockwise = false;
                                eAngleClockwise = productDetails.EndAngleClockWise;
                            }
                            //var cutAngle = 0;
                            var items = Create2DPipeLine(line, layer, productDetails.ProductWidth, showWidth, dimStyle, sCutAngle, eCutAngle, sAngleClockwise, eAngleClockwise, docLineEntity.Length);

                            //show dimensionValue 
                            var linearDimension = ShowLength(line, layer, productDetails.ProductWidth * 1.5, dimStyle);

                            //assign previous object
                            previousObject = line;

                            //add entity items to dxf file.
                            dxf.AddEntity(items);
                            dxf.AddEntity(line);
                            dxf.AddEntity(linearDimension);

                            entityObjectCollection.Add(line);
                        }
                        var docArcEntity = productDetails.PartList.Arcs.FirstOrDefault(x => x.EntityID == sequenceItems[i]);
                        if (docArcEntity != null)
                        {
                            var arc = new Arc();
                            var isClockWise = false;
                            if (previousObject != null)
                                if (previousObject.Type == EntityType.Arc)
                                    isClockWise = productDetails.PartList.Arcs.FirstOrDefault(x => x.EntityID == sequenceItems[i - 1]).IsClockwise;
                            arc = DrawAndAttachArc(previousObject, isClockWise, layer, docArcEntity.Angle, docArcEntity.Radius, docArcEntity.IsClockwise);
                            double sAngle = 0;
                            double eAngle = 0;
                            bool sAngeleClockwise = false;
                            bool eAngleClosewise = false;
                            bool showWidth = false;
                            if (i == 0)
                                showWidth = true;
                            else
                                showWidth = false;
                            if (i == 0)
                            {
                                sAngle = productDetails.StartCutAngle;
                                sAngeleClockwise = productDetails.StartAngleClockWise;
                                eAngle = 0;
                                eAngleClosewise = false;
                            }
                            else if (i == sequenceItems.Count - 1)
                            {
                                sAngle = 0;
                                sAngeleClockwise = false;
                                eAngle = productDetails.EndCutAngle;
                                eAngleClosewise = productDetails.EndAngleClockWise;
                            }
                            var cutangle = showWidth == true ? 60 : 0;
                            var items = Create2DPipeArc(arc, layer, previousObject, productDetails.ProductWidth, docArcEntity.IsClockwise, showWidth, dimStyle, sAngle, sAngeleClockwise, eAngle, eAngleClosewise);

                            //show dimensionValue
                            var radialDimension = ShowRadius(arc, layer, productDetails.ProductWidth * 0.75, dimStyle);

                            //assign previous object
                            previousObject = arc;

                            //add entity items to dxf file.
                            dxf.AddEntity(items);
                            dxf.AddEntity(arc);
                            dxf.AddEntity(radialDimension);
                            entityObjectCollection.Add(arc);
                        }

                    }
                    //drawScale(entityObjectCollection);
                    //draw customer label
                    var textCollection = AddText(entityObjectCollection, productDetails);
                    dxf.AddEntity(textCollection);

                    if (fileName.EndsWith(".dxf"))
                        dxf.Save(Path.Combine(outputFilePath, fileName));
                    else
                        dxf.Save(Path.Combine(outputFilePath, string.Concat(fileName, ".dxf")));
                }
            }
        }
        public static bool Test()
        {
            DxfDocument doc = new DxfDocument();
            var centerPoint = new Vector2(5, 5);
            var linex = new Line(new Vector2(0, 0), new Vector2(10, 0));
            linex.Color = AciColor.Blue;
            var liney = new Line(new Vector2(0, 10), new Vector2(0, -10));
            liney.Color = AciColor.Blue;
            doc.AddEntity(linex);
            doc.AddEntity(liney);

            var originalLine = new Line(new Vector2(-2, -2), new Vector2(12, 2));
            var originalLine1 = new Line(new Vector2(-2, -2), new Vector2(2, 4));
            originalLine1.Color = AciColor.Green;
            originalLine.Color = AciColor.Green;
            doc.AddEntity(originalLine);
            doc.AddEntity(originalLine1);

            for (int i = 30; i < 360; i = i + 30)
            {
                var rotatedLine = new Line();
                var rotationAngle = i * Math.PI / 180;
                var sX = (Math.Cos(rotationAngle) * (originalLine.StartPoint.X - centerPoint.X)) - (Math.Sin(rotationAngle) * (originalLine.StartPoint.Y - centerPoint.Y)) + centerPoint.X;
                var sY = (Math.Sin(rotationAngle) * (originalLine.StartPoint.X - centerPoint.X)) + (Math.Cos(rotationAngle) * (originalLine.StartPoint.Y - centerPoint.Y)) + centerPoint.Y;

                var eX = (Math.Cos(rotationAngle) * (originalLine.EndPoint.X - centerPoint.X)) - (Math.Sin(rotationAngle) * (originalLine.EndPoint.Y - centerPoint.Y)) + centerPoint.X;
                var eY = (Math.Sin(rotationAngle) * (originalLine.EndPoint.X - centerPoint.X)) + (Math.Cos(rotationAngle) * (originalLine.EndPoint.Y - centerPoint.Y)) + centerPoint.Y;
                rotatedLine.StartPoint = new Vector3(sX, sY, 0);
                rotatedLine.EndPoint = new Vector3(eX, eY, 0);
                if (i % 90 == 0)
                    rotatedLine.Color = AciColor.Green;
                else
                    rotatedLine.Color = AciColor.Red;
                doc.AddEntity(rotatedLine);


                var rotatedLine1 = new Line();
                var sX1 = (Math.Cos(rotationAngle) * (originalLine1.StartPoint.X - centerPoint.X)) - (Math.Sin(rotationAngle) * (originalLine1.StartPoint.Y - centerPoint.Y)) + centerPoint.X;
                var sY1 = (Math.Sin(rotationAngle) * (originalLine1.StartPoint.X - centerPoint.X)) + (Math.Cos(rotationAngle) * (originalLine1.StartPoint.Y - centerPoint.Y)) + centerPoint.Y;

                var eX1 = (Math.Cos(rotationAngle) * (originalLine1.EndPoint.X - centerPoint.X)) - (Math.Sin(rotationAngle) * (originalLine1.EndPoint.Y - centerPoint.Y)) + centerPoint.X;
                var eY1 = (Math.Sin(rotationAngle) * (originalLine1.EndPoint.X - centerPoint.X)) + (Math.Cos(rotationAngle) * (originalLine1.EndPoint.Y - centerPoint.Y)) + centerPoint.Y;
                rotatedLine1.StartPoint = new Vector3(sX1, sY1, 0);
                rotatedLine1.EndPoint = new Vector3(eX1, eY1, 0);
                if (i % 90 == 0)
                    rotatedLine1.Color = AciColor.Green;
                else
                    rotatedLine1.Color = AciColor.Red;
                doc.AddEntity(rotatedLine1);
                //break;
            }


            //for (int i = 30; i < 360; i = i + 30)
            //{
            //    var rotatedLine = new Line();
            //    var rotationAngle = i * Math.PI / 180;
            //    var sX = (Math.Cos(rotationAngle) * (originalLine.StartPoint.X - centerPoint.X)) - (Math.Sin(rotationAngle) * (originalLine.StartPoint.Y - centerPoint.Y)) + centerPoint.X;
            //    var sY = (Math.Sin(rotationAngle) * (originalLine.StartPoint.X - centerPoint.X)) + (Math.Cos(rotationAngle) * (originalLine.StartPoint.Y - centerPoint.Y)) + centerPoint.Y;

            //    var eX = (Math.Cos(rotationAngle) * (originalLine.EndPoint.X - centerPoint.X)) - (Math.Sin(rotationAngle) * (originalLine.EndPoint.Y - centerPoint.Y)) + centerPoint.X;
            //    var eY = (Math.Sin(rotationAngle) * (originalLine.EndPoint.X - centerPoint.X)) + (Math.Cos(rotationAngle) * (originalLine.EndPoint.Y - centerPoint.Y)) + centerPoint.Y;
            //    rotatedLine.StartPoint = new Vector3(sX, sY, 0);
            //    rotatedLine.EndPoint = new Vector3(eX, eY, 0);
            //    if (i % 90 == 0)
            //        rotatedLine.Color = AciColor.Green;
            //    else
            //        rotatedLine.Color = AciColor.Red;
            //    doc.AddEntity(rotatedLine);
            //}

            //for(int i=0; i<360; i= i+10)
            //{
            //    var rotationAngle = i * Math.PI / 180;

            //    var coordinate = new Vector2(10, 0);
            //    var X = (Math.Cos(rotationAngle) * (coordinate.X - centerPoint.X)) - (Math.Sin(rotationAngle) * (coordinate.Y - centerPoint.Y)) + centerPoint.X;
            //    var Y = (Math.Sin(rotationAngle) * (coordinate.X - centerPoint.X)) + (Math.Cos(rotationAngle) * (coordinate.Y - centerPoint.Y)) + centerPoint.Y;
            //    var point = new Vector2(X, Y);

            //    var line2 = new Line(centerPoint, point);
            //    doc.AddEntity(line2);

            //}
            doc.Save("E:\\krishna\\testRotation.dxf");

            return true;
        }

        /// <summary>
        /// Merge 2 or more dxf file in single file 
        /// </summary>
        /// <param name="originalDXFFileNamne">original dxf file</param>
        /// <param name="originalFileLocation">original dxf file location</param>
        /// <param name="destinationFileName">file name to be saved</param>
        /// <param name="destinationFileLocation">file destination location to be saved</param>
        /// <param name="originalStartX">Start point X of dummy line created while converting to svg file</param>
        /// <param name="originalStartY">Start point Y of dummy line created while converting to svg file</param>
        /// <param name="originalEndX">End point X of dummy line created while converting to svg file</param>
        /// <param name="originalEndY">End point Y of dummy line created while converting to svg file</param>
        /// <param name="addedContents"> List of files to be added to original file which contains Filename to merge, X and Y start point of dummy line created while converting to svg, Rotation angle and other information. </param>
        /// <returns>
        /// If  file with same name and same format already exists in destination Folder, it will be replaced.
        /// </returns>
        public static string MergeDXF(string originalDXFFileNamne, string originalFileLocation, string destinationFileName, string destinationFileLocation, double originalStartX, double originalStartY, double originalEndX, double originalEndY, List<MergeContent> addedContents)
        {
            string returnvalue = "";
            try
            {
                foreach(var _contents in addedContents)
                {
                    returnvalue = returnvalue + "FileLocation: " + _contents.FileLocation;
                    returnvalue = returnvalue + "FileName: " + _contents.FileName;
                    returnvalue = returnvalue + "RCX: " + _contents.RCX;
                    returnvalue = returnvalue + "RCY: " + _contents.RCY;
                    returnvalue = returnvalue + "Rotation: " + _contents.Rotation;
                    returnvalue = returnvalue + "StartPointX: " + _contents.StartPointX;
                    returnvalue = returnvalue + "StartPointY: " + _contents.StartPointY;

                }
                //var driveLocation = "E:\\krishna";
                if (!originalDXFFileNamne.EndsWith(".dxf"))
                    originalDXFFileNamne = string.Concat(originalDXFFileNamne, ".dxf");
                var originalStart = new Vector2(originalStartX, originalStartY);
                var originalEnd = new Vector2(originalEndX, originalEndY);
                var originalDXF = DxfDocument.Load(Path.Combine(originalFileLocation, originalDXFFileNamne));
                foreach (var _list in addedContents)
                {
                    var point = new Vector2(_list.StartPointX, _list.StartPointY);

                    var coordinateForDXF = GetPointForDXF(originalStart, originalEnd, Path.Combine(originalFileLocation, originalDXFFileNamne), point);
                    var rotationAngle = _list.Rotation;
                    //rotationAngle = 540 - rotationAngle;
                    //if (rotationAngle > 360)
                    //    rotationAngle = rotationAngle - 360;
                    rotationAngle = (360 - rotationAngle) * Math.PI / 180;
                    var centerOfRotation = new Vector2(_list.RCX, _list.RCY); // this will be somewhere in middle of dxf coordinate.
                    if (_list.Rotation > 0 && _list.Rotation < 360)
                    {
                        centerOfRotation = GetPointForDXF(originalStart, originalEnd, Path.Combine(originalFileLocation, originalDXFFileNamne), centerOfRotation);
                        //angle is provided clockwise and in degree. we need to convert it to anticlockwise and degree to radian. and rotation starts from third quadrant in clockwise.

                        //rotationAngle = getAngle(coordinateForDXF, centerOfRotation, rotationAngle);
                        //var coordinate = new Vector2(0, 0);
                        //coordinate.X = (Math.Cos(rotationAngle) * (coordinateForDXF.X - centerOfRotation.X)) - (Math.Sin(rotationAngle) * (coordinateForDXF.Y - centerOfRotation.Y)) + centerOfRotation.X;
                        //coordinate.Y = (Math.Sin(rotationAngle) * (coordinateForDXF.X - centerOfRotation.X)) + (Math.Cos(rotationAngle) * (coordinateForDXF.Y - centerOfRotation.Y)) + centerOfRotation.Y;
                        //coordinateForDXF = coordinate;
                    }

                    if (!_list.FileName.EndsWith(".dxf"))
                        _list.FileName = string.Concat(_list.FileName, ".dxf");
                    var addedDXF = DxfDocument.Load(Path.Combine(_list.FileLocation, _list.FileName));

                    #region line
                    foreach (var _dxfline in addedDXF.Lines)
                    {
                        var startPoint = new Vector3(_dxfline.StartPoint.X + coordinateForDXF.X, _dxfline.StartPoint.Y + coordinateForDXF.Y, 0);
                        var endPoint = new Vector3(_dxfline.EndPoint.X + coordinateForDXF.X, _dxfline.EndPoint.Y + coordinateForDXF.Y, 0);
                        //_dxfline.StartPoint = new Vector3(_dxfline.StartPoint.X + coordinateForDXF.X, _dxfline.StartPoint.Y + coordinateForDXF.Y, 0);
                        // _dxfline.EndPoint = new Vector3(_dxfline.EndPoint.X + coordinateForDXF.X, _dxfline.EndPoint.Y + coordinateForDXF.Y, 0);

                        if (_list.Rotation > 0 && _list.Rotation < 360)
                        {
                            //rotationAngle = getAngle(startPoint, centerOfRotation, rotationAngle);
                            var sX = (Math.Cos(rotationAngle) * (startPoint.X - centerOfRotation.X)) - (Math.Sin(rotationAngle) * (startPoint.Y - centerOfRotation.Y)) + centerOfRotation.X;
                            var sY = (Math.Sin(rotationAngle) * (startPoint.X - centerOfRotation.X)) + (Math.Cos(rotationAngle) * (startPoint.Y - centerOfRotation.Y)) + centerOfRotation.Y;
                            startPoint.X = sX;
                            startPoint.Y = sY;
                            //rotationAngle = getAngle(endPoint, centerOfRotation, rotationAngle);
                            var eX = (Math.Cos(rotationAngle) * (endPoint.X - centerOfRotation.X)) - (Math.Sin(rotationAngle) * (endPoint.Y - centerOfRotation.Y)) + centerOfRotation.X;
                            var eY = (Math.Sin(rotationAngle) * (endPoint.X - centerOfRotation.X)) + (Math.Cos(rotationAngle) * (endPoint.Y - centerOfRotation.Y)) + centerOfRotation.Y;
                            endPoint.X = eX;
                            endPoint.Y = eY;
                        }
                        _dxfline.StartPoint = startPoint;
                        _dxfline.EndPoint = endPoint;

                        var line = _dxfline.Clone() as Line;
                        originalDXF.AddEntity(line);
                    }
                    #endregion

                    #region Polyline
                    foreach (var _dxfployline in addedDXF.Polylines)
                    {
                        foreach (var _vertices in _dxfployline.Vertexes)
                        {
                            //_vertices.Position = new Vector3(_vertices.Position.X + coordinateForDXF.X, _vertices.Position.Y + coordinateForDXF.Y, 0);
                            var position = new Vector3(_vertices.Position.X + coordinateForDXF.X, _vertices.Position.Y + coordinateForDXF.Y, 0);
                            if (_list.Rotation > 0 && _list.Rotation < 360)
                            {
                                //rotationAngle = getAngle(position, centerOfRotation, rotationAngle);
                                var pX = (Math.Cos(rotationAngle) * (position.X - centerOfRotation.X)) - (Math.Sin(rotationAngle) * (position.Y - centerOfRotation.Y)) + centerOfRotation.X;
                                var pY = (Math.Sin(rotationAngle) * (position.X - centerOfRotation.X)) + (Math.Cos(rotationAngle) * (position.Y - centerOfRotation.Y)) + centerOfRotation.Y;
                                position.X = pX;
                                position.Y = pY;
                            }
                            _vertices.Position = position;

                        }
                        var polyline = _dxfployline.Clone() as Polyline;
                        originalDXF.AddEntity(polyline);
                    }
                    #endregion

                    #region LwPolyline 
                    foreach (var _dxfployline in addedDXF.LwPolylines)
                    {
                        foreach (var _vertices in _dxfployline.Vertexes)
                        {
                            //_vertices.Position = new Vector2(_vertices.Position.X + coordinateForDXF.X, _vertices.Position.Y + coordinateForDXF.Y);
                            var position = new Vector2(_vertices.Position.X + coordinateForDXF.X, _vertices.Position.Y + coordinateForDXF.Y);
                            if (_list.Rotation > 0 && _list.Rotation < 360)
                            {
                                //rotationAngle = getAngle(position, centerOfRotation, rotationAngle);
                                var pX = (Math.Cos(rotationAngle) * (position.X - centerOfRotation.X)) - (Math.Sin(rotationAngle) * (position.Y - centerOfRotation.Y)) + centerOfRotation.X;
                                var pY = (Math.Sin(rotationAngle) * (position.X - centerOfRotation.X)) + (Math.Cos(rotationAngle) * (position.Y - centerOfRotation.Y)) + centerOfRotation.Y;
                                position.X = pX;
                                position.Y = pY;

                            }
                            _vertices.Position = position;

                        }
                        var polyline = _dxfployline.Clone() as LwPolyline;
                        originalDXF.AddEntity(polyline);
                    }

                    #endregion

                    #region Arc
                    foreach (var _dxfArc in addedDXF.Arcs)
                    {
                        //_dxfArc.Center = new Vector3(_dxfArc.Center.X + coordinateForDXF.X, _dxfArc.Center.Y + coordinateForDXF.Y, 0);
                        var center = new Vector3(_dxfArc.Center.X + coordinateForDXF.X, _dxfArc.Center.Y + coordinateForDXF.Y, 0);
                        if (_list.Rotation > 0 && _list.Rotation < 360)
                        {
                            //rotationAngle = getAngle(center, centerOfRotation, rotationAngle);
                            var cX = (Math.Cos(rotationAngle) * (center.X - centerOfRotation.X)) - (Math.Sin(rotationAngle) * (center.Y - centerOfRotation.Y)) + centerOfRotation.X;
                            var cY = (Math.Sin(rotationAngle) * (center.X - centerOfRotation.X)) + (Math.Cos(rotationAngle) * (center.Y - centerOfRotation.Y)) + centerOfRotation.Y;
                            center.X = cX;
                            center.Y = cY;
                        }
                        _dxfArc.Center = center;
                        var arc = _dxfArc.Clone() as Arc;
                        originalDXF.AddEntity(arc);
                    }
                    #endregion

                    #region Circle
                    foreach (var _dxfCircle in addedDXF.Circles)
                    {
                        //_dxfCircle.Center = new Vector3(_dxfCircle.Center.X + coordinateForDXF.X, _dxfCircle.Center.Y + coordinateForDXF.Y, 0);
                        var center = new Vector3(_dxfCircle.Center.X + coordinateForDXF.X, _dxfCircle.Center.Y + coordinateForDXF.Y, 0);
                        if (_list.Rotation > 0 && _list.Rotation < 360)
                        {
                            //rotationAngle = getAngle(center, centerOfRotation, rotationAngle);
                            var cX = (Math.Cos(rotationAngle) * (center.X - centerOfRotation.X)) - (Math.Sin(rotationAngle) * (center.Y - centerOfRotation.Y)) + centerOfRotation.X;
                            var cY = (Math.Sin(rotationAngle) * (center.X - centerOfRotation.X)) + (Math.Cos(rotationAngle) * (center.Y - centerOfRotation.Y)) + centerOfRotation.Y;
                            center.X = cX;
                            center.Y = cY;

                        }
                        _dxfCircle.Center = center;
                    }

                    #endregion


                    #region Ellipse
                    foreach (var _dxfEllipse in addedDXF.Ellipses)
                    {
                        //_dxfEllipse.Center = new Vector3(_dxfEllipse.Center.X + coordinateForDXF.X, _dxfEllipse.Center.Y + coordinateForDXF.Y, 0);
                        var center = new Vector3(_dxfEllipse.Center.X + coordinateForDXF.X, _dxfEllipse.Center.Y + coordinateForDXF.Y, 0);
                        if (_list.Rotation > 0 && _list.Rotation < 360)
                        {
                            //rotationAngle = getAngle(center, centerOfRotation, rotationAngle);
                            var cX = (Math.Cos(rotationAngle) * (center.X - centerOfRotation.X)) - (Math.Sin(rotationAngle) * (center.Y - centerOfRotation.Y)) + centerOfRotation.X;
                            var cY = (Math.Sin(rotationAngle) * (center.X - centerOfRotation.X)) + (Math.Cos(rotationAngle) * (center.Y - centerOfRotation.Y)) + centerOfRotation.Y;
                            center.X = cX;
                            center.Y = cY;

                        }
                        _dxfEllipse.Center = center;
                        var ellipse = _dxfEllipse.Clone() as Ellipse;
                        originalDXF.AddEntity(ellipse);
                    }
                    #endregion

                    #region Image
                    foreach (var _dxfImage in addedDXF.Images)
                    {
                        //_dxfImage.Position = new Vector3(_dxfImage.Position.X + coordinateForDXF.X, _dxfImage.Position.Y + coordinateForDXF.Y, 0);
                        var position = new Vector3(_dxfImage.Position.X + coordinateForDXF.X, _dxfImage.Position.Y + coordinateForDXF.Y, 0);
                        if (_list.Rotation > 0 && _list.Rotation < 360)
                        {
                            //rotationAngle = getAngle(point, centerOfRotation, rotationAngle);
                            var pX = (Math.Cos(rotationAngle) * (position.X - centerOfRotation.X)) - (Math.Sin(rotationAngle) * (position.Y - centerOfRotation.Y)) + centerOfRotation.X;
                            var pY = (Math.Sin(rotationAngle) * (position.X - centerOfRotation.X)) + (Math.Cos(rotationAngle) * (position.Y - centerOfRotation.Y)) + centerOfRotation.Y;
                            position.X = pX;
                            position.Y = pY;

                        }
                        _dxfImage.Position = position;
                        var img = _dxfImage.Clone() as Image;
                        originalDXF.AddEntity(img);
                    }
                    #endregion

                    #region Insert 
                    foreach (var _dxfInsert in addedDXF.Inserts)
                    {
                        //_dxfInsert.Position = new Vector3(_dxfInsert.Position.X + coordinateForDXF.X, _dxfInsert.Position.Y + coordinateForDXF.Y, 0);
                        var position = new Vector3(_dxfInsert.Position.X + coordinateForDXF.X, _dxfInsert.Position.Y + coordinateForDXF.Y, 0);
                        if (_list.Rotation > 0 && _list.Rotation < 360)
                        {
                            //rotationAngle = getAngle(position, centerOfRotation, rotationAngle);
                            var pX = (Math.Cos(rotationAngle) * (position.X - centerOfRotation.X)) - (Math.Sin(rotationAngle) * (position.Y - centerOfRotation.Y)) + centerOfRotation.X;
                            var pY = (Math.Sin(rotationAngle) * (position.X - centerOfRotation.X)) + (Math.Cos(rotationAngle) * (position.Y - centerOfRotation.Y)) + centerOfRotation.Y;
                            position.X = pX;
                            position.Y = pY;

                        }
                        _dxfInsert.Position = position;
                        var entity = _dxfInsert.Clone() as Insert;
                        originalDXF.AddEntity(entity);
                    }
                    #endregion

                    #region MLine
                    foreach (var _dxfMline in addedDXF.MLines)
                    {
                        foreach (var _vertexes in _dxfMline.Vertexes)
                        {
                            //_vertexes.Location = new Vector2(_vertexes.Location.X + coordinateForDXF.X, _vertexes.Location.Y + coordinateForDXF.Y);
                            var position = new Vector2(_vertexes.Location.X + coordinateForDXF.X, _vertexes.Location.Y + coordinateForDXF.Y);
                            if (_list.Rotation > 0 && _list.Rotation < 360)
                            {
                                //rotationAngle = getAngle(position, centerOfRotation, rotationAngle);
                                var pX = (Math.Cos(rotationAngle) * (position.X - centerOfRotation.X)) - (Math.Sin(rotationAngle) * (position.Y - centerOfRotation.Y)) + centerOfRotation.X;
                                var pY = (Math.Sin(rotationAngle) * (position.X - centerOfRotation.X)) + (Math.Cos(rotationAngle) * (position.Y - centerOfRotation.Y)) + centerOfRotation.Y;
                                position.X = pX;
                                position.Y = pY;

                            }
                            _vertexes.Location = position;
                        }
                        var entity = _dxfMline.Clone() as MLine;
                        originalDXF.AddEntity(entity);
                    }
                    #endregion

                    #region MText
                    foreach (var _dxfMText in addedDXF.MTexts)
                    {
                        ////_dxfMText.Position = new Vector3(_dxfMText.Position.X + coordinateForDXF.X, _dxfMText.Position.Y + coordinateForDXF.Y, 0);
                        //var position = new Vector3(_dxfMText.Position.X + coordinateForDXF.X, _dxfMText.Position.Y + coordinateForDXF.Y, 0);
                        //if (_list.Rotation > 0 && _list.Rotation < 360)
                        //{
                        //    var pX = (Math.Cos(rotationAngle) * (position.X - centerOfRotation.X)) - (Math.Sin(rotationAngle) * (position.Y - centerOfRotation.Y)) + centerOfRotation.X;
                        //    var pY = (Math.Sin(rotationAngle) * (position.X - centerOfRotation.X)) + (Math.Cos(rotationAngle) * (position.Y - centerOfRotation.Y)) + centerOfRotation.Y;
                        //    position.X = pX;
                        //    position.Y = pY;

                        //}
                        //_dxfMText.Position = position;
                        //var entity = _dxfMText.Clone() as MText;
                        //originalDXF.AddEntity(entity);
                    }

                    #endregion                    

                    #region Ray
                    foreach (var _ray in addedDXF.Rays)
                    {
                        //_ray.Origin = new Vector3(_ray.Origin.X + coordinateForDXF.X, _ray.Origin.Y + coordinateForDXF.Y, 0);
                        var position = new Vector3(_ray.Origin.X + coordinateForDXF.X, _ray.Origin.Y + coordinateForDXF.Y, 0);
                        if (_list.Rotation > 0 && _list.Rotation < 360)
                        {
                            //rotationAngle = getAngle(position, centerOfRotation, rotationAngle);
                            var pX = (Math.Cos(rotationAngle) * (position.X - centerOfRotation.X)) - (Math.Sin(rotationAngle) * (position.Y - centerOfRotation.Y)) + centerOfRotation.X;
                            var pY = (Math.Sin(rotationAngle) * (position.X - centerOfRotation.X)) + (Math.Cos(rotationAngle) * (position.Y - centerOfRotation.Y)) + centerOfRotation.Y;
                            position.X = pX;
                            position.Y = pY;

                        }
                        _ray.Origin = position;
                        var entity = _ray.Clone() as Ray;
                        originalDXF.AddEntity(entity);
                    }
                    #endregion

                    #region Solid
                    foreach (var _solid in addedDXF.Solids)
                    {
                        //_solid.FirstVertex = new Vector2(_solid.FirstVertex.X + coordinateForDXF.X, _solid.FirstVertex.Y + coordinateForDXF.Y);
                        //_solid.SecondVertex = new Vector2(_solid.SecondVertex.X + coordinateForDXF.X, _solid.SecondVertex.Y + coordinateForDXF.Y);
                        //_solid.ThirdVertex = new Vector2(_solid.ThirdVertex.X + coordinateForDXF.X, _solid.ThirdVertex.Y + coordinateForDXF.Y);
                        //_solid.FourthVertex = new Vector2(_solid.FourthVertex.X + coordinateForDXF.X, _solid.FourthVertex.Y + coordinateForDXF.Y);

                        var position = new Vector2(_solid.FirstVertex.X + coordinateForDXF.X, _solid.FirstVertex.Y + coordinateForDXF.Y);
                        if (_list.Rotation > 0 && _list.Rotation < 360)
                        {
                            //rotationAngle = getAngle(position, centerOfRotation, rotationAngle);
                            var pX = (Math.Cos(rotationAngle) * (position.X - centerOfRotation.X)) - (Math.Sin(rotationAngle) * (position.Y - centerOfRotation.Y)) + centerOfRotation.X;
                            var pY = (Math.Sin(rotationAngle) * (position.X - centerOfRotation.X)) + (Math.Cos(rotationAngle) * (position.Y - centerOfRotation.Y)) + centerOfRotation.Y;
                            position.X = pX;
                            position.Y = pY;

                        }
                        _solid.FirstVertex = position;
                        position = new Vector2(_solid.SecondVertex.X + coordinateForDXF.X, _solid.SecondVertex.Y + coordinateForDXF.Y);
                        if (_list.Rotation > 0 && _list.Rotation < 360)
                        {
                            //rotationAngle = getAngle(position, centerOfRotation, rotationAngle);
                            var pX = (Math.Cos(rotationAngle) * (position.X - centerOfRotation.X)) - (Math.Sin(rotationAngle) * (position.Y - centerOfRotation.Y)) + centerOfRotation.X;
                            var pY = (Math.Sin(rotationAngle) * (position.X - centerOfRotation.X)) + (Math.Cos(rotationAngle) * (position.Y - centerOfRotation.Y)) + centerOfRotation.Y;
                            position.X = pX;
                            position.Y = pY;

                        }
                        _solid.SecondVertex = position;
                        position = new Vector2(_solid.ThirdVertex.X + coordinateForDXF.X, _solid.ThirdVertex.Y + coordinateForDXF.Y);
                        if (_list.Rotation > 0 && _list.Rotation < 360)
                        {
                            //rotationAngle = getAngle(position, centerOfRotation, rotationAngle);
                            var pX = (Math.Cos(rotationAngle) * (position.X - centerOfRotation.X)) - (Math.Sin(rotationAngle) * (position.Y - centerOfRotation.Y)) + centerOfRotation.X;
                            var pY = (Math.Sin(rotationAngle) * (position.X - centerOfRotation.X)) + (Math.Cos(rotationAngle) * (position.Y - centerOfRotation.Y)) + centerOfRotation.Y;
                            position.X = pX;
                            position.Y = pY;

                        }
                        _solid.ThirdVertex = position;
                        position = new Vector2(_solid.FourthVertex.X + coordinateForDXF.X, _solid.FourthVertex.Y + coordinateForDXF.Y);
                        if (_list.Rotation > 0 && _list.Rotation < 360)
                        {
                            //rotationAngle = getAngle(position, centerOfRotation, rotationAngle);
                            var pX = (Math.Cos(rotationAngle) * (position.X - centerOfRotation.X)) - (Math.Sin(rotationAngle) * (position.Y - centerOfRotation.Y)) + centerOfRotation.X;
                            var pY = (Math.Sin(rotationAngle) * (position.X - centerOfRotation.X)) + (Math.Cos(rotationAngle) * (position.Y - centerOfRotation.Y)) + centerOfRotation.Y;
                            position.X = pX;
                            position.Y = pY;

                        }
                        _solid.FourthVertex = position;

                        var entity = _solid.Clone() as Solid;
                        originalDXF.AddEntity(entity);
                    }

                    #endregion

                    #region Text
                    foreach (var _text in addedDXF.Texts)
                    {
                        ////_text.Position = new Vector3(_text.Position.X + coordinateForDXF.X, _text.Position.Y + coordinateForDXF.Y, 0);
                        //var position = new Vector3(_text.Position.X + coordinateForDXF.X, _text.Position.Y + coordinateForDXF.Y, 0);
                        //if (_list.Rotation > 0 && _list.Rotation < 360)
                        //{
                        //    var pX = (Math.Cos(rotationAngle) * (position.X - centerOfRotation.X)) - (Math.Sin(rotationAngle) * (position.Y - centerOfRotation.Y)) + centerOfRotation.X;
                        //    var pY = (Math.Sin(rotationAngle) * (position.X - centerOfRotation.X)) + (Math.Cos(rotationAngle) * (position.Y - centerOfRotation.Y)) + centerOfRotation.Y;
                        //    position.X = pX;
                        //    position.Y = pY;

                        //}
                        //_text.Position = position;
                        //var entity = _text.Clone() as Text;
                        //originalDXF.AddEntity(entity);
                    }
                    #endregion

                    //only linear and radial dimension is added.
                    #region Dimensions
                    foreach (var dim in addedDXF.Dimensions)
                    {
                        //if (dim.DimensionType == DimensionType.Linear)
                        //{
                        //    var dimension = dim as LinearDimension;
                        //    //dimension.FirstReferencePoint = new Vector2(dimension.FirstReferencePoint.X + coordinateForDXF.X, dimension.FirstReferencePoint.Y + coordinateForDXF.Y);
                        //    //dimension.SecondReferencePoint = new Vector2(dimension.SecondReferencePoint.X + coordinateForDXF.X, dimension.SecondReferencePoint.Y + coordinateForDXF.Y);
                        //    var firstRefPoint = new Vector2(dimension.FirstReferencePoint.X + coordinateForDXF.X, dimension.FirstReferencePoint.Y + coordinateForDXF.Y);
                        //    var secondRefPoint = new Vector2(dimension.SecondReferencePoint.X + coordinateForDXF.X, dimension.SecondReferencePoint.Y + coordinateForDXF.Y);
                        //    if (_list.Rotation > 0 && _list.Rotation < 360)
                        //    {
                        //        var fX = (Math.Cos(rotationAngle) * (firstRefPoint.X - centerOfRotation.X)) - (Math.Sin(rotationAngle) * (firstRefPoint.Y - centerOfRotation.Y)) + centerOfRotation.X;
                        //        var fY = (Math.Sin(rotationAngle) * (firstRefPoint.X - centerOfRotation.X)) + (Math.Cos(rotationAngle) * (firstRefPoint.Y - centerOfRotation.Y)) + centerOfRotation.Y;
                        //        var sX = (Math.Cos(rotationAngle) * (secondRefPoint.X - centerOfRotation.X)) - (Math.Sin(rotationAngle) * (secondRefPoint.Y - centerOfRotation.Y)) + centerOfRotation.X;
                        //        var sY = (Math.Sin(rotationAngle) * (secondRefPoint.X - centerOfRotation.X)) + (Math.Cos(rotationAngle) * (secondRefPoint.Y - centerOfRotation.Y)) + centerOfRotation.Y;
                        //        firstRefPoint.X = fX;
                        //        firstRefPoint.Y = fY;
                        //        secondRefPoint.X = sX;
                        //        secondRefPoint.Y = sY;
                        //    }
                        //    dimension.FirstReferencePoint = firstRefPoint;
                        //    dimension.SecondReferencePoint = secondRefPoint;
                        //    var linearDimension = dimension.Clone() as LinearDimension;
                        //    originalDXF.AddEntity(linearDimension);
                        //}
                        //if (dim.DimensionType == DimensionType.Radius)
                        //{
                        //    var dimension = dim as RadialDimension;
                        //    //dimension.ReferencePoint = new Vector2(dimension.ReferencePoint.X + coordinateForDXF.X, dimension.ReferencePoint.Y + coordinateForDXF.Y);
                        //    //dimension.CenterPoint = new Vector2(dimension.CenterPoint.X + coordinateForDXF.X, dimension.CenterPoint.Y + coordinateForDXF.Y);
                        //    var referencePoint = new Vector2(dimension.ReferencePoint.X + coordinateForDXF.X, dimension.ReferencePoint.Y + coordinateForDXF.Y);
                        //    var centerPoint = new Vector2(dimension.CenterPoint.X + coordinateForDXF.X, dimension.CenterPoint.Y + coordinateForDXF.Y);
                        //    if (_list.Rotation > 0 && _list.Rotation < 360)
                        //    {
                        //        var rX = (Math.Cos(rotationAngle) * (referencePoint.X - centerOfRotation.X)) - (Math.Sin(rotationAngle) * (referencePoint.Y - centerOfRotation.Y)) + centerOfRotation.X;
                        //        var rY = (Math.Sin(rotationAngle) * (referencePoint.X - centerOfRotation.X)) + (Math.Cos(rotationAngle) * (referencePoint.Y - centerOfRotation.Y)) + centerOfRotation.Y;
                        //        var cX = (Math.Cos(rotationAngle) * (centerPoint.X - centerOfRotation.X)) - (Math.Sin(rotationAngle) * (centerPoint.Y - centerOfRotation.Y)) + centerOfRotation.X;
                        //        var cY = (Math.Sin(rotationAngle) * (centerPoint.X - centerOfRotation.X)) + (Math.Cos(rotationAngle) * (centerPoint.Y - centerOfRotation.Y)) + centerOfRotation.Y;
                        //        referencePoint.X = rX;
                        //        referencePoint.Y = rY;
                        //        centerPoint.X = cX;
                        //        centerPoint.Y = cY;
                        //    }
                        //    dimension.ReferencePoint = referencePoint;
                        //    dimension.CenterPoint = centerPoint;
                        //    if (dimension.Measurement > 40)
                        //    {
                        //        var endpoint = Coordinate.CalculateLineEndPoint(new Vector3(dimension.CenterPoint.X, dimension.CenterPoint.Y, 0), new Vector3(dimension.ReferencePoint.X, dimension.ReferencePoint.Y, 0), 40 - dimension.Measurement);
                        //        dimension.SetDimensionLinePosition(new Vector2(endpoint.X, endpoint.Y));
                        //        dimension.Color = AciColor.DarkGray;
                        //    }
                        //    var radialDimensin = dimension.Clone() as RadialDimension;
                        //    originalDXF.AddEntity(radialDimensin);
                        //}
                    }

                    #endregion

                    //not implemented
                    #region Leader

                    #endregion
                    //Not implemented.
                    #region Mesh

                    #endregion
                    //not implemented
                    #region PolyfaceMesh
                    #endregion
                    //not implemented
                    #region Spline
                    #endregion
                    //not implemented
                    #region Hatch
                    #endregion
                    //not implemented
                    #region Tolerance
                    #endregion
                    //not implemented
                    #region Trace

                    #endregion
                    //not implemented
                    #region Underlay
                    #endregion
                    //not implemented
                    #region Wipeout

                    #endregion
                    //not implemented
                    #region XLine (aka construction line)

                    #endregion



                }
                if (!destinationFileName.EndsWith(".dxf"))
                    destinationFileName = string.Concat(destinationFileName, ".dxf");
                originalDXF.Save(Path.Combine(destinationFileLocation, destinationFileName));

                //return true;
                return returnvalue;
            }
            catch(Exception ex)
            {
                return ex.Message;
                //return false;
            }


        }

        //private static double getAngle(Vector2 coordinateForDXF, Vector2 centerOfRotation, double rotationAngle)
        //{
        //    //throw new NotImplementedException();
        //    //var rotation = 0.0;
        //    ////first Quadrant
        //    //if (coordinateForDXF.X >= centerOfRotation.X && coordinateForDXF.Y <= centerOfRotation.Y)
        //    //{
        //    //    //rotation = 180 - rotationAngle;

        //    //}
        //    ////second Quadrant
        //    //else if (coordinateForDXF.X <= centerOfRotation.X && coordinateForDXF.Y <= centerOfRotation.Y)
        //    //{
        //    //    rotation = 180 - rotationAngle;
        //    //}
        //    ////third Quadrant
        //    //else if (coordinateForDXF.X <= centerOfRotation.X && coordinateForDXF.Y >= centerOfRotation.Y)
        //    //{
        //    //    rotation = 360 - rotationAngle;
        //    //}
        //    ////fourth Quadrant
        //    //else if (coordinateForDXF.X >= centerOfRotation.X && coordinateForDXF.Y >= centerOfRotation.Y)
        //    //{
        //    //    rotation = 360 - rotationAngle;
        //    //}
        //    //return rotation * Math.PI / 180;

        //    return rotationAngle * Math.PI / 180;

        //}
        //private static double getAngle(Vector3 coordinateForDXF, Vector2 centerOfRotation, double rotationAngle)
        //{
        //    //throw new NotImplementedException();
        //    //var rotation = 0.0;
        //    ////first Quadrant
        //    //if (coordinateForDXF.X >= centerOfRotation.X && coordinateForDXF.Y <= centerOfRotation.Y)
        //    //{
        //    //    //rotation = 180 - rotationAngle;

        //    //}
        //    ////second Quadrant
        //    //else if (coordinateForDXF.X <= centerOfRotation.X && coordinateForDXF.Y <= centerOfRotation.Y)
        //    //{
        //    //    rotation = 180 - rotationAngle;
        //    //}
        //    ////third Quadrant
        //    //else if (coordinateForDXF.X <= centerOfRotation.X && coordinateForDXF.Y >= centerOfRotation.Y)
        //    //{
        //    //    rotation = 360 - rotationAngle;
        //    //}
        //    ////fourth Quadrant
        //    //else if (coordinateForDXF.X >= centerOfRotation.X && coordinateForDXF.Y >= centerOfRotation.Y)
        //    //{
        //    //    rotation = 360 - rotationAngle;
        //    //}
        //    //return rotation * Math.PI / 180;

        //    return rotationAngle * Math.PI / 180;

        //}

        /// <summary>
        /// Converts dxf file to different file format like svg, png, pdf, jpeg, etc.
        /// </summary>
        /// <param name="programPath">Location where AcmeCadConverter.exe exits. For eg: "C:\Program Files (x86)\Acme CAD Converter\AcmeCADConverter.exe"</param>
        /// <param name="dxfFilePath">Location where dxf file exits. For eg : "E:\AutoCad\PipeWriter\bin\Debug\DxfFiles\MyProduct.dxf"</param>
        /// <param name="outPutFileFormat">Desired file format.</param>
        /// <returns>void</returns>
        /// <remarks>
        /// If the file with same name and same format already exists it will be replaced.
        /// </remarks>
        public static bool Convert(string programPath, string dxfFilePath, FileFormat outPutFileFormat)
        {
            DxfDocument dxfDoc = new DxfDocument();
            Line l = new Line();
            try
            {
                dxfDoc = DxfDocument.Load(dxfFilePath);
                l.StartPoint = new Vector3(0, 0, 0);
                l.EndPoint = new Vector3(100, 0, 0);
                l.Color = new AciColor(System.Drawing.Color.Ivory);
                dxfDoc.RemoveEntity(l);
                dxfDoc.AddEntity(l);
                dxfDoc.Save(dxfFilePath);
                Process process = new Process();
                ProcessStartInfo startInfo = new ProcessStartInfo();

                process.StartInfo.FileName = programPath;
                var filePathquoted = @"""" + dxfFilePath + @"""";
                //var parameter = @"/r /ls  /p 3 /ad /b 7 /a -2 /f " + (int)outPutFileFormat + " " + dxfFilePath + " ";
                //process.StartInfo.Arguments = @"/r /p 3 /ad /b 7 /a -2 /f " + (int)outPutFileFormat + " " + dxfFilePath + "";
                process.StartInfo.Arguments = @"/r /ls  /p 3 /ad /b 7 /a -2 /f " + (int)outPutFileFormat + " " + filePathquoted + "";

                process.Start();

                process.WaitForExit();
                dxfDoc.RemoveEntity(l);
                dxfDoc.Save(dxfFilePath);
            }
            catch
            {
                dxfDoc.RemoveEntity(l);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Method to generate logo to on which we can write our own data
        /// </summary>
        /// <param name="filePath">Original .dxf File path</param>
        /// <param name="filename">Original .dxf file name</param>
        /// <param name="destinationPath">destination path under where new generated .dxf with data to save</param>
        /// <param name="destinationFileName">destination file name under where new generated .dxf with data to save</param>
        /// <param name="xmldoc"></param>
        //public void GenerateISOlogo(string filePath,string filename, string destinationPath, string destinationFileName, XmlDocument xmldoc)
        public static void GenerateISOlogo(string filePath, string filename, string destinationPath, string destinationFileName, string xmlstring)
        {
            var xdoc = XDocument.Parse(xmlstring);
            var doc = DxfDocument.Load(Path.Combine(filePath, filename));
            foreach (var _nodes in xdoc.Descendants())
            {
                var mtext = doc.MTexts.Where(x => x.Value.Contains("<" + _nodes.Name.LocalName + "/>"));
                foreach (var _mtext in mtext)
                {
                    _mtext.Value = _mtext.Value.Replace("<" + _nodes.Name.LocalName + "/>", _nodes.Value);
                }
                var text = doc.Texts.Where(x => x.Value.Contains("<" + _nodes.Name.LocalName + "/>"));
                foreach (var _text in text)
                {
                    _text.Value = _text.Value.Replace("<" + _nodes.Name.LocalName + "/>", _nodes.Value);
                }
            }
            if (destinationFileName.EndsWith(".dxf"))
                doc.Save(Path.Combine(destinationPath, destinationFileName));
            else
                doc.Save(Path.Combine(destinationPath, string.Concat(destinationFileName, ".dxf")));
        }
        private static Vector2 GetPointForDXF(Vector2 originalstartA, Vector2 originalEndB, string originalDXF, Vector2 startpointC)
        {
            var quadrant = 0;
            if (startpointC.X >= originalstartA.X && startpointC.Y <= originalstartA.Y)
                quadrant = 1;
            else if (startpointC.X <= originalstartA.X && startpointC.Y <= originalstartA.Y)
                quadrant = 2;
            else if (startpointC.X <= originalstartA.X && startpointC.Y >= originalstartA.Y)
                quadrant = 3;
            else if (startpointC.X >= originalstartA.X && startpointC.Y >= originalstartA.Y)
                quadrant = 4;
            //get length of all sides by creating triangle with dummy line we created with reference to startpointC
            //instead of getting square root get square value which is used to find angle
            var ABsquare = ((originalstartA.X - originalEndB.X) * (originalstartA.X - originalEndB.X)) + ((originalstartA.Y - originalEndB.Y) * (originalstartA.Y - originalEndB.Y)); //Math.Sqrt(((originalstartA.X - originalEndB.X) * (originalstartA.X - originalEndB.X)) + ((originalstartA.Y - originalEndB.Y) * (originalstartA.Y - originalEndB.Y)));
            var ACsquare = ((originalstartA.X - startpointC.X) * (originalstartA.X - startpointC.X)) + ((originalstartA.Y - startpointC.Y) * (originalstartA.Y - startpointC.Y)); //Math.Sqrt(((originalstartA.X - startpointC.X) * (originalstartA.X - startpointC.X)) + ((originalstartA.Y - startpointC.Y) * (originalstartA.Y - startpointC.Y)));
            var BCsquare = ((originalEndB.X - startpointC.X) * (originalEndB.X - startpointC.X)) + ((originalEndB.Y - startpointC.Y) * (originalEndB.Y - startpointC.Y)); //Math.Sqrt(((originalEndB.X - startpointC.X) * (originalEndB.X - startpointC.X)) + ((originalEndB.Y - startpointC.Y) * (originalEndB.Y - startpointC.Y)));

            //Apply cosine rule for triangle law with ourDummy line to startPoint to calcuate relative point that need to be created for our original dxfFile.
            var anglea = Math.Acos(((ABsquare) + (ACsquare) - (BCsquare)) / (2 * Math.Sqrt(ABsquare) * Math.Sqrt(ACsquare))); //Math.Acos(((AC * AC) + (BC * BC) - (AB * AB)) / (2 * BC * AC)); 
            var angleb = Math.Acos(((ABsquare) + (BCsquare) - (ACsquare)) / (2 * Math.Sqrt(ABsquare) * Math.Sqrt(BCsquare)));
            var anglec = Math.PI - (anglea + angleb);//Math.Acos(((ACsquare) + (BCsquare) - (ABsquare)) / (2 * Math.Sqrt(BCsquare) * Math.Sqrt(ACsquare))); //Math.Acos(((AB * AB) + (AC * AC) - (BC * BC)) / (2 * AB * AC));


            //apply sine rule for triangle to find lenght of sides to triangle on original dxf.
            var startPointDXF = new Vector2(0, 0);
            var endPointDXF = new Vector2(100, 0);

            var AB1 = Math.Sqrt(((startPointDXF.X - endPointDXF.X) * (startPointDXF.X - endPointDXF.X)) + ((startPointDXF.Y - endPointDXF.Y) * (startPointDXF.Y - endPointDXF.Y)));

            var AC1 = Math.Sin(angleb) * (AB1 / Math.Sin(anglec));

            var PointInDxf = new Vector2(0, 0);
            //x1 = x0 + cos(angle) * length    -- in this case length is AC1
            //y1 = y0 + sin(angle) * length    -- in this case length is AC1            
            if ((quadrant == 3 || quadrant == 4) && (anglea * 180 / Math.PI) < 180)
            {
                anglea = (2d * Math.PI) - anglea;
            }
            PointInDxf.X = startPointDXF.X + (Math.Cos(anglea) * AC1);
            PointInDxf.Y = startPointDXF.Y + (Math.Sin(anglea) * AC1);
            return PointInDxf;
        }
        private static List<EntityObject> AddText(List<EntityObject> list, Product productDetails)
        {
            List<EntityObject> textList = new List<EntityObject>();
            double height;
            double width;
            var textStartPoint = GetTextStartPoint(list, out height, out width);
            //var textStartPoint = GetTextStartPoint(list);
            //var sign = Math.Sign(textStartPoint.Y);
            textStartPoint.Y = (textStartPoint.Y - 4 * productDetails.FontSizeDimension - productDetails.ProductWidth);


            var text1 = new Text(string.Concat(" Customer :  ", productDetails.Customer), textStartPoint,
                productDetails.FontSizeDimension);
            var text2 = new Text(string.Concat(" Order    :  ", productDetails.OrderText),
                new Vector2(textStartPoint.X, textStartPoint.Y - (productDetails.FontSizeDimension * 1.67)),
                productDetails.FontSizeDimension);
            textList.Add(text1);
            textList.Add(text2);
            return textList;

        }
        private void AddScale(string dxfFileLocation, int x, int y, int Unit)
        {
            Vector3 centerPoint = new Vector3(x, y, 0);
            //DxfDocument doc = new DxfDocument();
            var loadedDoc = DxfDocument.Load(dxfFileLocation);

            Line xAxis = new Line();
            xAxis.StartPoint = centerPoint;
            xAxis.EndPoint = new Vector3(centerPoint.X, centerPoint.X + 200, 0);
            xAxis.Color = AciColor.Red;

            Line yAxis = new Line();
            yAxis.StartPoint = centerPoint;
            yAxis.EndPoint = new Vector3(centerPoint.Y + 200, centerPoint.Y, 0);
            yAxis.Color = AciColor.Red;

            loadedDoc.AddEntity(xAxis);
            loadedDoc.AddEntity(yAxis);

            for (int i = (int)centerPoint.X + 1; i <= (int)yAxis.EndPoint.X; i++)
            {
                //X segments
                Line l = new Line();
                if ((i + Unit) % Unit == 0)
                {
                    l.StartPoint = new Vector3(i, y, 0);
                    l.EndPoint = new Vector3(i, y + 5, 0);
                }
                else
                {
                    l.StartPoint = new Vector3(i, y, 0);
                    l.EndPoint = new Vector3(i, y + 2, 0);
                }
                l.Color = AciColor.Red;
                loadedDoc.AddEntity(l);
            }

            for (int i = (int)centerPoint.Y + 1; i <= (int)xAxis.EndPoint.Y; i++)
            {
                //Y segments
                Line l = new Line();
                if ((i + Unit) % Unit == 0)
                {
                    l.StartPoint = new Vector3(x, i, 0);
                    l.EndPoint = new Vector3(x + 5, i, 0);
                }
                else
                {
                    l.StartPoint = new Vector3(x, i, 0);
                    l.EndPoint = new Vector3(x + 2, i, 0);
                }

                l.Color = AciColor.Red;
                loadedDoc.AddEntity(l);

            }
            loadedDoc.Save(@"E:\krishna\testScale.dxf");

        }
        private static List<EntityObject> Create2DPipeLine(Line lineEntity, Layer layer, double width, bool showWidth, DimensionStyle dimStyle, double startCutAngle, double endCutAngle, bool startAngelClockWise, bool endAngelClockWise, double length)
        {
            List<EntityObject> objectCollection = new List<EntityObject>();
            if (startCutAngle != 0 || endCutAngle != 0)
            {
                var startorEndOffset1 = lineEntity.StartPoint;
                var startorEndOffset2 = lineEntity.StartPoint;
                var A = (width / 2);
                var extendedLength = A * Math.Tan(startCutAngle * (Math.PI / 180));

                if (startCutAngle != 0)
                {
                    if (startAngelClockWise)
                    {
                        startorEndOffset1 = Coordinate.CalculateLineEndPoint(lineEntity.EndPoint, lineEntity.StartPoint, -extendedLength);
                        startorEndOffset2 = Coordinate.CalculateLineEndPoint(lineEntity.EndPoint, lineEntity.StartPoint, extendedLength);
                    }
                    else
                    {
                        startorEndOffset1 = Coordinate.CalculateLineEndPoint(lineEntity.EndPoint, lineEntity.StartPoint, extendedLength);
                        startorEndOffset2 = Coordinate.CalculateLineEndPoint(lineEntity.EndPoint, lineEntity.StartPoint, -extendedLength);
                    }
                    var lwPolyLine1 = new LwPolyline
                    {
                        Vertexes =
                        {
                            new LwPolylineVertex(startorEndOffset1.X, startorEndOffset1.Y),
                            new LwPolylineVertex(lineEntity.EndPoint.X, lineEntity.EndPoint.Y)
                        }
                    };
                    var lwPolyLine2 = new LwPolyline
                    {
                        Vertexes =
                        {
                            new LwPolylineVertex(startorEndOffset2.X, startorEndOffset2.Y),
                            new LwPolylineVertex(lineEntity.EndPoint.X, lineEntity.EndPoint.Y)
                        }
                    };
                    var offset1 = lwPolyLine1.GetOffsetCurves(-width / 2);
                    var offset2 = lwPolyLine2.GetOffsetCurves(width / 2);

                    var endline1 = new Line(
                    new Vector2(offset1.Vertexes[0].Position.X, offset1.Vertexes[0].Position.Y),
                    new Vector2(offset2.Vertexes[0].Position.X, offset2.Vertexes[0].Position.Y));
                    var endLine2 = new Line(
                        new Vector2(offset1.Vertexes[1].Position.X, offset1.Vertexes[1].Position.Y),
                        new Vector2(offset2.Vertexes[1].Position.X, offset2.Vertexes[1].Position.Y));

                    offset1.Layer = layer;
                    offset2.Layer = layer;
                    endline1.Layer = layer;
                    endLine2.Layer = layer;

                    offset1.Color = AciColor.Blue;
                    offset2.Color = AciColor.Blue;
                    endline1.Color = AciColor.Blue;
                    endLine2.Color = AciColor.Blue;
                    objectCollection.Add(offset1);
                    objectCollection.Add(offset2);
                    objectCollection.Add(endline1);
                    objectCollection.Add(endLine2);


                    //show width dimension here





                }
                else if (endCutAngle != 0)
                {
                    if (endAngelClockWise)
                    {
                        startorEndOffset1 = Coordinate.CalculateLineEndPoint(lineEntity.StartPoint, lineEntity.EndPoint, extendedLength);
                        startorEndOffset2 = Coordinate.CalculateLineEndPoint(lineEntity.StartPoint, lineEntity.EndPoint, -extendedLength);
                    }
                    else
                    {
                        startorEndOffset1 = Coordinate.CalculateLineEndPoint(lineEntity.StartPoint, lineEntity.EndPoint, -extendedLength);
                        startorEndOffset2 = Coordinate.CalculateLineEndPoint(lineEntity.StartPoint, lineEntity.EndPoint, extendedLength);
                    }
                    var lwPolyLine1 = new LwPolyline
                    {
                        Vertexes =
                        {
                            new LwPolylineVertex(startorEndOffset1.X, startorEndOffset1.Y),
                            new LwPolylineVertex(lineEntity.EndPoint.X, lineEntity.EndPoint.Y)
                        }
                    };
                    var lwPolyLine2 = new LwPolyline
                    {
                        Vertexes =
                        {
                            new LwPolylineVertex(startorEndOffset2.X, startorEndOffset2.Y),
                            new LwPolylineVertex(lineEntity.EndPoint.X, lineEntity.EndPoint.Y)
                        }
                    };
                    var offset1 = lwPolyLine1.GetOffsetCurves(-width / 2);
                    var offset2 = lwPolyLine2.GetOffsetCurves(width / 2);

                    var endline1 = new Line(
                    new Vector2(offset1.Vertexes[0].Position.X, offset1.Vertexes[0].Position.Y),
                    new Vector2(offset2.Vertexes[0].Position.X, offset2.Vertexes[0].Position.Y));
                    var endLine2 = new Line(
                        new Vector2(offset1.Vertexes[1].Position.X, offset1.Vertexes[1].Position.Y),
                        new Vector2(offset2.Vertexes[1].Position.X, offset2.Vertexes[1].Position.Y));

                    offset1.Layer = layer;
                    offset2.Layer = layer;
                    endline1.Layer = layer;
                    endLine2.Layer = layer;

                    offset1.Color = AciColor.Blue;
                    offset2.Color = AciColor.Blue;
                    endline1.Color = AciColor.Blue;
                    endLine2.Color = AciColor.Blue;
                    objectCollection.Add(offset1);
                    objectCollection.Add(offset2);
                    objectCollection.Add(endline1);
                    objectCollection.Add(endLine2);
                }

            }

            else
            {
                var lwPolyLine = new LwPolyline
                {
                    Vertexes =
                      {
                          new LwPolylineVertex(lineEntity.StartPoint.X, lineEntity.StartPoint.Y),
                          new LwPolylineVertex(lineEntity.EndPoint.X, lineEntity.EndPoint.Y)
                      }
                };
                var offset1 = lwPolyLine.GetOffsetCurves(width / 2);
                var offset2 = lwPolyLine.GetOffsetCurves(-width / 2);
                var endLine1 = new Line(
                    new Vector2(offset1.Vertexes[0].Position.X, offset1.Vertexes[0].Position.Y),
                    new Vector2(offset2.Vertexes[0].Position.X, offset2.Vertexes[0].Position.Y));
                var endLine2 = new Line(
                    new Vector2(offset1.Vertexes[1].Position.X, offset1.Vertexes[1].Position.Y),
                    new Vector2(offset2.Vertexes[1].Position.X, offset2.Vertexes[1].Position.Y));

                offset1.Layer = layer;
                offset2.Layer = layer;
                endLine1.Layer = layer;
                endLine2.Layer = layer;

                offset1.Color = AciColor.Blue;
                offset2.Color = AciColor.Blue;
                endLine1.Color = AciColor.Blue;
                endLine2.Color = AciColor.Blue;


                objectCollection.Add(offset1);
                objectCollection.Add(offset2);
                objectCollection.Add(endLine1);
                objectCollection.Add(endLine2);

                if (showWidth)
                {
                    var shownWidth = ShowWidth(endLine1, layer, dimStyle, width * 0.75);
                    objectCollection.Add(shownWidth);
                }
            }
            return objectCollection;
        }
        private static List<EntityObject> Create2DPipeArc(Arc arcEntity, Layer layer, EntityObject previousObject, double width, bool isArcAngleClockwise, bool showWidth, DimensionStyle dimStyle, double startAngle, bool sAngleClockwise, double endAngle, bool eAngleClockwise)
        {
            List<EntityObject> objectCollection = new List<EntityObject>();
            var Offset1StartAngle = arcEntity.StartAngle;
            var offset2StartAngle = arcEntity.StartAngle;
            var offset1EndAngle = arcEntity.EndAngle;
            var offset2EndAngle = arcEntity.EndAngle;


            var offset1 = new Arc
            {
                Linetype = Linetype.Continuous,
                Center = new Vector3(arcEntity.Center.X, arcEntity.Center.Y, 0),
                StartAngle = Offset1StartAngle,// arcEntity.StartAngle,
                EndAngle = offset1EndAngle,//arcEntity.EndAngle,
                Radius = arcEntity.Radius + width / 2
            };
            var offset2 = new Arc
            {
                Linetype = Linetype.Continuous,
                Center = new Vector3(arcEntity.Center.X, arcEntity.Center.Y, 0),
                StartAngle = offset2StartAngle,// arcEntity.StartAngle,
                EndAngle = offset2EndAngle,// arcEntity.EndAngle,
                Radius = arcEntity.Radius - width / 2
            };

            if (startAngle != 0 || endAngle != 0)
            {
                //calculate length of cord C^2 = A^2 + B^2 -2abCos(c) where c is angle 
                var A = (width / 2);
                if (startAngle != 0)
                {
                    //let suport centerpoint of main arc is A Center of arc that is middle arc is point B and arc intersection point is C.
                    //than we know some sides and some angles for Triangle ABC.
                    //we can use sine rule of triangle to fine angle c.

                    //sina/a = sinb/b == sinc/c
                    if (sAngleClockwise)
                    {
                        //for offset 2
                        var sineAngleC = (Math.Sin(startAngle * (Math.PI / 180)) * (offset2.Radius + A)) / (offset2.Radius);
                        var anglec = Math.Asin(sineAngleC);
                        anglec = 180 - ((180 / Math.PI) * anglec);
                        var anglea = 180 - anglec - startAngle;
                        offset2.StartAngle = offset2.StartAngle + anglea;

                        //for offset1 
                        offset1.StartAngle = offset1.StartAngle - anglea;

                    }
                    else
                    {
                        var sineAngleC = (Math.Sin(startAngle * (Math.PI / 180)) * (offset2.Radius + (width / 2))) / (offset2.Radius);
                        var anglec = Math.Asin(sineAngleC);
                        anglec = 180 - ((180 / Math.PI) * anglec);
                        var anglea = 180 - anglec - startAngle;
                        offset2.StartAngle = offset2.StartAngle - anglea;
                        //for offset1 
                        offset1.StartAngle = offset1.StartAngle + anglea;

                    }
                }
                else
                {
                    //let suport centerpoint of main arc is A Center of arc that is middle arc is point B and arc intersection point is C.
                    //than we know some sides and some angles for Triangle ABC.
                    //we can use sine rule of triangle to fine angle c.

                    //sina/a = sinb/b == sinc/c
                    if (eAngleClockwise)
                    {
                        //for offset 2
                        var sineAngleC = (Math.Sin(endAngle * (Math.PI / 180)) * (offset2.Radius + A)) / (offset2.Radius);
                        var anglec = Math.Asin(sineAngleC);
                        anglec = 180 - ((180 / Math.PI) * anglec);
                        var anglea = 180 - anglec - startAngle;
                        offset2.EndAngle = offset2.EndAngle + anglea;

                        //for offset1 
                        offset1.EndAngle = offset1.EndAngle - anglea;

                    }
                    else
                    {
                        //for offset 2
                        var sineAngleC = (Math.Sin(endAngle * (Math.PI / 180)) * (offset2.Radius + A)) / (offset2.Radius);
                        var anglec = Math.Asin(sineAngleC);
                        anglec = 180 - ((180 / Math.PI) * anglec);
                        var anglea = 180 - anglec - startAngle;
                        offset2.EndAngle = offset2.EndAngle - anglea;

                        //for offset1 
                        offset1.EndAngle = offset1.EndAngle - anglea;
                    }
                }

            }
            var verticesOffset1 = offset1.GetVertexesOfArc();
            var verticesOffset2 = offset2.GetVertexesOfArc();
            var endLine1 = new Line(verticesOffset1[0], verticesOffset2[0]);
            var endLine2 = new Line(verticesOffset1[1], verticesOffset2[1]);
            offset1.Layer = layer;
            offset2.Layer = layer;
            endLine1.Layer = layer;
            endLine2.Layer = layer;

            offset1.Color = AciColor.Green;
            offset2.Color = AciColor.Green;
            endLine1.Color = AciColor.Green;
            endLine2.Color = AciColor.Green;



            objectCollection.Add(offset1);
            objectCollection.Add(offset2);
            objectCollection.Add(endLine1);
            objectCollection.Add(endLine2);

            if (showWidth)
            {
                var shownWidth = ShowWidth(endLine1, layer, dimStyle, width * 0.75);
                objectCollection.Add(shownWidth);
            }
            return objectCollection;
        }
        private static LinearDimension ShowWidth(Line endLine1, Layer layer, DimensionStyle dimStyle, double width)
        {
            var WDimension = new LinearDimension();

            WDimension.FirstReferencePoint = new Vector2(endLine1.StartPoint.X, endLine1.StartPoint.Y);
            WDimension.SecondReferencePoint = new Vector2(endLine1.EndPoint.X, endLine1.EndPoint.Y);
            WDimension.Offset = width; //width * 0.75;
            var slope1 = (endLine1.EndPoint.Y - endLine1.StartPoint.Y) /
                         (endLine1.EndPoint.X - endLine1.StartPoint.X);

            WDimension.Rotation = WDimension.Rotation > 180
                ? (360 - Math.Atan(slope1) * 180 / Math.PI)
                : Math.Atan(slope1) * 180 / Math.PI;
            WDimension.Layer = layer;
            WDimension.Style = dimStyle;
            //objectCollection.Add(WDimension);
            return WDimension;
        }
        private static Layer AddLayer(string layerName)
        {
            Layer layer = new Layer(layerName);
            return layer;
        }
        private static LinearDimension ShowLength(Line lineEntity, Layer layer, double offset, DimensionStyle dimStyle)
        {
            var dimension = new LinearDimension();

            dimension.FirstReferencePoint = new Vector2(lineEntity.StartPoint.X, lineEntity.StartPoint.Y);
            dimension.SecondReferencePoint = new Vector2(lineEntity.EndPoint.X, lineEntity.EndPoint.Y);
            dimension.Offset = offset;//productDetails.ProductWidth * 1.5;
            var slope = (lineEntity.EndPoint.Y - lineEntity.StartPoint.Y) /
                        (lineEntity.EndPoint.X - lineEntity.StartPoint.X);

            dimension.Rotation = dimension.Rotation > 180
                ? (360 - Math.Atan(slope) * 180 / Math.PI)
                : Math.Atan(slope) * 180 / Math.PI;
            dimension.Layer = layer;
            dimension.Style = dimStyle;
            return dimension;
        }
        private static RadialDimension ShowRadius(Arc arcEntity, Layer layer, double offset, DimensionStyle dimStyle)
        {
            var startAngle = arcEntity.StartAngle > arcEntity.EndAngle
                      ? arcEntity.StartAngle - 360
                      : arcEntity.StartAngle;
            var dimension = new RadialDimension(arcEntity, (startAngle + arcEntity.EndAngle) / 2, offset);
            //dimension.SetDimensionLinePosition(new Vector2(0, 0)); 
            if (dimension.Measurement > 40)
            {
                //Vector2 v = new Vector2(0, 0);
                var endpoint = Coordinate.CalculateLineEndPoint(new Vector3(dimension.CenterPoint.X, dimension.CenterPoint.Y, 0), new Vector3(dimension.ReferencePoint.X, dimension.ReferencePoint.Y, 0), 40 - dimension.Measurement);

                dimension.SetDimensionLinePosition(new Vector2(endpoint.X, endpoint.Y));
                dimension.Color = AciColor.DarkGray;
            }
            dimension.Layer = layer;
            dimension.Style = dimStyle;
            return dimension;
        }
        private static Line DrawAndAttachLine(EntityObject previousEntityObj, bool isPreviousArcClockWise, double length, double width, Layer layer)
        {
            Line line = new Line();
            line.Linetype = Linetype.Dashed;
            if (previousEntityObj == null)
            {
                line.StartPoint = new Vector3(0, 0, 0);
                line.EndPoint = new Vector3(length, 0, 0);
            }
            else if (previousEntityObj.Type == EntityType.Line)
            {
                var prevLineObj = previousEntityObj as Line;
                line.StartPoint = prevLineObj.EndPoint;
                line.EndPoint = Coordinate.CalculateLineEndPoint(prevLineObj.StartPoint, prevLineObj.EndPoint, length);
            }
            else if (previousEntityObj.Type == EntityType.Arc)
            {
                var prevArcObj = previousEntityObj as Arc;
                var startPoint = prevArcObj.GetVertexesOfArc(isPreviousArcClockWise)[1];
                var endPoint = Coordinate.CalculateLineEndPoint(prevArcObj.Center, startPoint,
                                length, isPreviousArcClockWise);
                line.StartPoint = startPoint;
                line.EndPoint = endPoint;
            }
            line.Layer = layer;
            line.Color = AciColor.Blue;

            return line;
        }
        private static Arc DrawAndAttachArc(EntityObject previousEntityObj, bool isPrevArcClockWise, Layer layer, double angle, double radius, bool isClockWise)
        {
            Arc arc = new Arc();
            arc.Linetype = Linetype.Dashed;
            if (previousEntityObj == null)
            {
                var center = Coordinate.CalculateArcCenter(new Vector3(0, 0, 0),
                               new Vector3(0, 0, 0), radius, isClockWise);
                var angles = Coordinate.GetArcAngles(new Vector3(0, 0, 0), center, radius,
                    angle, isClockWise);
                arc.Linetype = Linetype.Center;
                arc.Center = center;
                arc.StartAngle = angles[0];
                arc.EndAngle = angles[1];
                arc.Radius = radius;
            }
            else if (previousEntityObj.Type == EntityType.Line)
            {
                var previousLineObj = previousEntityObj as Line;
                var center = Coordinate.CalculateArcCenter(previousLineObj.StartPoint,
                                      previousLineObj.EndPoint, radius, isClockWise);
                var angles = Coordinate.GetArcAngles(previousLineObj.EndPoint, center, radius,
                    angle, isClockWise);
                arc.Linetype = Linetype.Dashed;
                arc.Center = center;
                arc.StartAngle = angles[0];
                arc.EndAngle = angles[1];
                arc.Radius = radius;

            }
            else if (previousEntityObj.Type == EntityType.Arc)
            {
                var prevArcObj = previousEntityObj as Arc;
                Vector3 contactPoint;
                var center = Coordinate.CalculateArcCenter(prevArcObj, isPrevArcClockWise, radius,
                    isClockWise, out contactPoint);
                var angles = Coordinate.GetArcAngles(contactPoint, center, radius,
                    angle, isClockWise);

                arc.Linetype = Linetype.Dashed;
                arc.Center = center;
                arc.StartAngle = angles[0];
                arc.EndAngle = angles[1];
                arc.Radius = radius;
            }
            return arc;
        }
        private static bool Validate(Product prod, string fileName)
        {
            //validate wrong input
            var worngInputs = prod.PartList.Arcs.Where(x => x.Radius <= prod.ProductWidth / 2).ToList();
            if (worngInputs.Count > 0)
            {
                foreach (var item in worngInputs)
                {
                    throw new Exception(string.Concat(DateTime.Now, "  ---  File ", "\"", fileName, ".xml\" ",
                              "contains an arc defination with radius less than or equal to half of the product width.",
                              "  ---  Sequence Num  ---  ", item.EntityID));
                }
                return false;
            }
            


            return true;
        }
        private static List<int> GetItemsSequence(Product prod, string fileName)
        {
            List<int> sequence = new List<int>();
            foreach (var _productItem in prod.PartList.Lines.Where(x=>x.Length > 0).OrderBy(x => x.EntityID))
            {
                if (sequence.Contains(_productItem.EntityID))
                {
                    throw new Exception(string.Concat(DateTime.Now, "  ---  File ", "\"", fileName, ".xml\" ",
                              "contains Multiple sequence number.",
                              "  ---  Sequence Num  ---  ", _productItem.EntityID));
                    //return;
                }
                sequence.Add(_productItem.EntityID);
            }
            foreach (var _productItem in prod.PartList.Arcs.OrderBy(x => x.EntityID))
            {
                if (sequence.Contains(_productItem.EntityID))
                {
                    throw new Exception(string.Concat(DateTime.Now, "  ---  File ", "\"", fileName, ".xml\" ",
                              "contains Multiple sequence number.",
                              "  ---  Sequence Num  ---  ", _productItem.EntityID));
                    //return;
                }
                sequence.Add(_productItem.EntityID);
            }
            return sequence;
        }
        private static List<EntityObject> GetEntityObjCollection(Parts partList)
        {
            var orgiEntityCollection = new List<EntityObject>();
            bool continueLoop = true;
            int i = 1;
            while (continueLoop)
            {
                var line = partList.Lines.FirstOrDefault(x => x.EntityID == i);
                if (line != null)
                {
                    if (i == 1)
                    {
                        orgiEntityCollection.Add(new Line
                        {
                            Linetype = Linetype.Center,
                            StartPoint = new Vector3(0, 0, 0),
                            EndPoint = new Vector3(line.Length, 0, 0)
                        });
                    }
                    else
                    {
                        var preElement = orgiEntityCollection[i - 2];

                        // if the preceeding element is Arc
                        if (preElement.Type == EntityType.Arc)
                        {
                            var preElementArc = preElement as Arc;
                            var preArc = partList.Arcs.FirstOrDefault(x => x.EntityID == i - 1);
                            var startPoint = preElementArc.GetVertexesOfArc(preArc.IsClockwise)[1];

                            var endPoint = Coordinate.CalculateLineEndPoint(preElementArc.Center, startPoint,
                                line.Length, preArc.IsClockwise);

                            orgiEntityCollection.Add(new Line
                            {
                                Linetype = Linetype.Center,
                                StartPoint = startPoint,
                                EndPoint = endPoint
                            });
                        }
                        // if the preceeding element is line
                        else
                        {
                            var preElementLine = preElement as Line;
                            var startPoint = preElementLine.EndPoint;
                            var endPoint = Coordinate.CalculateLineEndPoint(preElementLine.StartPoint, preElementLine.EndPoint, line.Length);

                            orgiEntityCollection.Add(new Line
                            {
                                Linetype = Linetype.Center,
                                StartPoint = startPoint,
                                EndPoint = endPoint
                            });
                        }


                    }
                }
                else
                {
                    var arc = partList.Arcs.FirstOrDefault(x => x.EntityID == i);
                    continueLoop = arc != null;
                    if (continueLoop)
                    {
                        if (i == 1)
                        {
                            var center = Coordinate.CalculateArcCenter(new Vector3(-1, 0, 0),
                                new Vector3(0, 0, 0), arc.Radius, arc.IsClockwise);
                            var angles = Coordinate.GetArcAngles(new Vector3(0, 0, 0), center, arc.Radius,
                                arc.Angle, arc.IsClockwise);
                            orgiEntityCollection.Add(new Arc
                            {
                                Linetype = Linetype.Center,
                                Center = center,
                                StartAngle = angles[0],
                                EndAngle = angles[1],
                                Radius = arc.Radius,
                            });
                        }
                        else
                        {
                            var preElement = orgiEntityCollection[i - 2];

                            if (preElement.Type == EntityType.Line)
                            {
                                var preElementLine = preElement as Line;
                                var center = Coordinate.CalculateArcCenter(preElementLine.StartPoint,
                                    preElementLine.EndPoint, arc.Radius, arc.IsClockwise);
                                var angles = Coordinate.GetArcAngles(preElementLine.EndPoint, center, arc.Radius,
                                    arc.Angle, arc.IsClockwise);

                                orgiEntityCollection.Add(new Arc
                                {
                                    Linetype = Linetype.Center,
                                    Center = center,
                                    StartAngle = angles[0],
                                    EndAngle = angles[1],
                                    Radius = arc.Radius,
                                });
                            }
                            else
                            {
                                var preElementArc = preElement as Arc;
                                var preArc = partList.Arcs.FirstOrDefault(x => x.EntityID == i - 1);
                                Vector3 contactPoint;
                                var center = Coordinate.CalculateArcCenter(preElementArc, preArc.IsClockwise, arc.Radius,
                                    arc.IsClockwise, out contactPoint);
                                var angles = Coordinate.GetArcAngles(contactPoint, center, arc.Radius,
                                    arc.Angle, arc.IsClockwise);

                                orgiEntityCollection.Add(new Arc
                                {
                                    Linetype = Linetype.Center,
                                    Center = center,
                                    StartAngle = angles[0],
                                    EndAngle = angles[1],
                                    Radius = arc.Radius,
                                });
                            }

                        }
                    }
                }
                i++;
            }
            return orgiEntityCollection;

        }
        private static Vector2 GetTextStartPoint(List<EntityObject> collection, out double height, out double width)
        {
            var result = new Vector2(0, 0);
            var result1 = new Vector2(0, 0);
            foreach (var item in collection)
            {

                if (item.Type == EntityType.Line)
                {
                    var thisItem = item as Line;
                    result.X = thisItem.StartPoint.X < result.X ? thisItem.StartPoint.X : result.X;
                    result.X = thisItem.EndPoint.X < result.X ? thisItem.EndPoint.X : result.X;
                    result.Y = thisItem.StartPoint.Y < result.Y ? thisItem.StartPoint.Y : result.Y;
                    result.Y = thisItem.EndPoint.Y < result.Y ? thisItem.EndPoint.Y : result.Y;

                    result1.X = thisItem.StartPoint.X > result1.X ? thisItem.StartPoint.X : result1.X;
                    result1.X = thisItem.EndPoint.X > result1.X ? thisItem.EndPoint.X : result1.X;
                    result1.Y = thisItem.StartPoint.Y > result1.Y ? thisItem.StartPoint.Y : result1.Y;
                    result1.Y = thisItem.EndPoint.Y > result1.Y ? thisItem.EndPoint.Y : result1.Y;

                }
                else
                {
                    var thisItem = item as Arc;
                    result.X = (thisItem.Center.X - thisItem.Radius) < result.X
                        ? (thisItem.Center.X - thisItem.Radius)
                        : result.X;
                    result.Y = (thisItem.Center.Y - thisItem.Radius) < result.Y
                      ? (thisItem.Center.Y - thisItem.Radius)
                      : result.Y;


                    result1.X = (thisItem.Center.X - thisItem.Radius) > result1.X
                        ? (thisItem.Center.X - thisItem.Radius)
                        : result1.X;
                    result1.Y = (thisItem.Center.Y - thisItem.Radius) > result1.Y
                      ? (thisItem.Center.Y - thisItem.Radius)
                      : result1.Y;
                }
            }
            width = result1.X - result.X;
            height = result1.Y - result.Y;

            return result;
        }
    }

    public enum FileFormat
    {
        bitmap = 1,
        Jpeg = 2,
        GIF = 3,
        PCX = 4,
        TIFF = 5,
        PNG = 6,
        TGA = 7,
        WMF = 8,
        SVG = 101,
        PLT = 102,
        HGL = 103,
        PDF = 104,
        SVGZ = 106,
        CGM = 107,
        EPS = 108
    }
}
