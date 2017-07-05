using System;
using netDxf;
using netDxf.Entities;

namespace PipeWriter
{
    internal static class Coordinate
    {
        // if the preceeding element is line.
        // here startpoint and endPoint refers to the start and end point of preceeding line.
        // And arcRadius and isArcClockwise variable refers to the current arc.
        public static Vector3 CalculateArcCenter(Vector3 startPoint, Vector3 endPoint, double arcRadius, bool isArcClockwise)
        {
            // m denotes slope of line in co-ordinate geometry
            var m = Math.Pow((endPoint.Y - startPoint.Y) / (endPoint.X - startPoint.X), 2);
           
            var n = 1 + (1 / m);   

            var p = Math.Sqrt(Math.Pow(arcRadius, 2) / n);
            var xValues = new[] { endPoint.X + p, endPoint.X - p };
            if (Math.Abs(xValues[0] - xValues[1]) < 0.1)
            {
                var q = Math.Sqrt(Math.Pow(arcRadius, 2) - Math.Pow((endPoint.X - xValues[0]), 2));
                var yValues = new[] { endPoint.Y + q, endPoint.Y - q };
                var lineVector = new Vector3(endPoint.X - startPoint.X, endPoint.Y - startPoint.Y, 0);
                var radiusVector = new Vector3(xValues[0] - endPoint.X, yValues[0] - endPoint.Y, 0);
                var crossProdResult = (lineVector.X * radiusVector.Y) - (lineVector.Y * radiusVector.X);
                if (crossProdResult > 0 && !isArcClockwise)
                {
                    return new Vector3(xValues[0], yValues[0], 0);
                }
                if (crossProdResult < 0 && !isArcClockwise)
                {
                    return new Vector3(xValues[0], yValues[1], 0);
                }
                if (crossProdResult > 0 && isArcClockwise)
                {
                    return new Vector3(xValues[0], yValues[1], 0);
                }
                if (crossProdResult < 0 && isArcClockwise)
                {
                    return new Vector3(xValues[0], yValues[0], 0);
                }
            }
            else
            {
                var c1 = -((xValues[0] - endPoint.X)/(endPoint.Y - startPoint.Y))*(endPoint.X - startPoint.X);
                var c2 = -((xValues[1] - endPoint.X) / (endPoint.Y - startPoint.Y)) * (endPoint.X - startPoint.X);
                var yValue1 = endPoint.Y + c1;
                var yValue2 = endPoint.Y + c2;


                var lineVector = new Vector3(endPoint.X - startPoint.X, endPoint.Y - startPoint.Y, 0);
                var radiusVector = new Vector3(xValues[0] - endPoint.X, yValue1 - endPoint.Y, 0);

                var crossProdResult = (lineVector.X * radiusVector.Y) - (lineVector.Y * radiusVector.X);

                if (crossProdResult > 0 && !isArcClockwise)
                {
                    return new Vector3(xValues[0], yValue1, 0);
                }
                if (crossProdResult < 0 && !isArcClockwise)
                {
                    return new Vector3(xValues[1], yValue2, 0);
                }
                if (crossProdResult > 0 && isArcClockwise)
                {
                    return new Vector3(xValues[1], yValue2, 0);
                }
                if (crossProdResult < 0 && isArcClockwise)
                {
                    return new Vector3(xValues[0], yValue1, 0);
                }

            }

            return new Vector3();
        }


        // if the preceeding element is Arc.
        // here center, contactpoint and isArcClockwise refers to the center, endpoint and direction of preceeding arc.
        // And length refers to lenght of the current line.
        public static Vector3 CalculateLineEndPoint(Vector3 center, Vector3 contactPoint, double length, bool isArcClockwise)
        {
            var tangentSlope = -((contactPoint.X - center.X) / (contactPoint.Y - center.Y));
            var c = Math.Sqrt(Math.Pow(length, 2) / (1 + Math.Pow(tangentSlope, 2)));
            var xValues = new[] { contactPoint.X + c, contactPoint.X - c };
            if (Math.Abs(xValues[0] - xValues[1]) < 0.1)
            {
                var cons = Math.Sqrt(Math.Pow(length, 2) - Math.Pow((xValues[0] - contactPoint.X), 2));
                var yValue1 = contactPoint.Y + cons;
                var yValue2 = contactPoint.Y - cons;

                var lineVector = new Vector3(xValues[0] - contactPoint.X, yValue1 - contactPoint.Y, 0);
                var radiusVector = new Vector3(center.X - contactPoint.X, center.Y - contactPoint.Y, 0);
                var crossProdResult = (lineVector.X * radiusVector.Y) - (lineVector.Y * radiusVector.X);

                if (crossProdResult > 0 && !isArcClockwise)
                {
                    return new Vector3(xValues[0], yValue1, 0);
                }
                if (crossProdResult < 0 && !isArcClockwise)
                {
                    return new Vector3(xValues[0], yValue2, 0);
                }
                if (crossProdResult > 0 && isArcClockwise)
                {
                    return new Vector3(xValues[0], yValue2, 0);
                }
                if (crossProdResult < 0 && isArcClockwise)
                {
                    return new Vector3(xValues[0], yValue1, 0);
                }
            }
            else
            {
                var yValue1 = (tangentSlope * (xValues[0] - contactPoint.X)) + contactPoint.Y;
                var yValue2 = (tangentSlope * (xValues[1] - contactPoint.X)) + contactPoint.Y;

                var lineVector = new Vector3(xValues[0] - contactPoint.X, yValue1 - contactPoint.Y, 0);
                var radiusVector = new Vector3(center.X - contactPoint.X, center.Y - contactPoint.Y, 0);
                var crossProdResult = (lineVector.X * radiusVector.Y) - (lineVector.Y * radiusVector.X);

                if (crossProdResult > 0 && !isArcClockwise)
                {
                    return new Vector3(xValues[0], yValue1, 0);
                }
                if (crossProdResult < 0 && !isArcClockwise)
                {
                    return new Vector3(xValues[1], yValue2, 0);
                }
                if (crossProdResult > 0 && isArcClockwise)
                {
                    return new Vector3(xValues[1], yValue2, 0);
                }
                if (crossProdResult < 0 && isArcClockwise)
                {
                    return new Vector3(xValues[0], yValue1, 0);
                }
            }
            return new Vector3();
        }


        // if the preceeding element is line.
        // here startpoint and endPoint refers to the start and end point of preceeding line. 
        //And length refers to the length of current line.

        public static Vector3 CalculateLineEndPoint(Vector3 startPoint, Vector3 endPoint, double length)
        {
            var lineSlope = (endPoint.Y - startPoint.Y) / (endPoint.X - startPoint.X);
            var cons = lineSlope * length / Math.Sqrt(Math.Pow(lineSlope, 2) + 1);
            var yValue1 = endPoint.Y + cons;
            var yValue2 = endPoint.Y - cons;
            var preceedingLineLength =
                    Math.Sqrt(Math.Pow(endPoint.X - startPoint.X, 2) + Math.Pow(endPoint.Y - startPoint.Y, 2));
            if (Math.Abs(yValue1 - yValue2) < 0.1)
            {
                var cons1 = Math.Sqrt(Math.Pow(length, 2) - Math.Pow(yValue1 - endPoint.Y, 2));
                var xValue1 = endPoint.X + cons1;
                var xValue2 = endPoint.X - cons1;
                var totalLineLength =
                Math.Sqrt(Math.Pow(xValue1 - startPoint.X, 2) + Math.Pow(yValue1 - startPoint.Y, 2));
                
                if (Math.Abs(preceedingLineLength + length - totalLineLength) < 0.1)
                {
                    return new Vector3(xValue1, yValue1, 0);
                }
                return new Vector3(xValue2, yValue1, 0);
            }
            else
            {
                var xValue1 = endPoint.X + (yValue1 - endPoint.Y) / lineSlope;
                var xValue2 = endPoint.X + (yValue2 - endPoint.Y) / lineSlope;

                var totalLineLength =
                    Math.Sqrt(Math.Pow(xValue1 - startPoint.X, 2) + Math.Pow(yValue1 - startPoint.Y, 2));
                
                if (Math.Abs(preceedingLineLength + length - totalLineLength) < 0.1)
                {
                    return new Vector3(xValue1, yValue1, 0);
                }
                return new Vector3(xValue2, yValue2, 0);
                
            }

        }



        // if the preceeding element is Arc.
        // here arcRadius and isArcClockwise refers to the radius and direction of current arc.
        public static Vector3 CalculateArcCenter(Arc preceedingArc, bool isPreceedingArcClockwise, double arcRadius, bool isArcClockwise, out Vector3 contactPoint)
        {
            var vertixesOfPreceedingArc = preceedingArc.GetVertexesOfArc(isPreceedingArcClockwise);
            var endPointOfVirtualLine = CalculateLineEndPoint(preceedingArc.Center, vertixesOfPreceedingArc[1], 1,
                !isPreceedingArcClockwise);
            var requiredCenter = CalculateArcCenter(endPointOfVirtualLine, vertixesOfPreceedingArc[1], arcRadius,
                isArcClockwise);
            contactPoint = vertixesOfPreceedingArc[1];
            return requiredCenter;
        }


        // startPoint refers to start point of the arc whose angle is being calculated
        // returns start angle and end angle at index 0 and 1 respectively
        public static double[] GetArcAngles(Vector3 startPoint, Vector3 center, double arcRadius, double inscribedAngle, bool isArcClockwise)
        {
            var angle1 = Math.Acos((startPoint.X - center.X) / arcRadius) * 180 / Math.PI;
            var retainedY = center.Y + (arcRadius*Math.Sin(angle1*Math.PI/180));
            if (Math.Abs(startPoint.Y - retainedY) > 0.1)
            {
                angle1 = 360-angle1;
            }
            var angle2 = isArcClockwise ? angle1 - inscribedAngle : angle1 + inscribedAngle;
            if (Math.Abs(angle1 - angle2 - inscribedAngle) < 0.1)
                return new[] { angle2, angle1 };
            return new[] { angle1, angle2 };
        }


        // returns both ends (co-ordinate) of arc.
        public static Vector3[] GetVertexesOfArc(this Arc obj)
        {
            var sX = obj.Center.X + obj.Radius * Math.Cos(obj.StartAngle * Math.PI / 180);
            var sY = obj.Center.Y + obj.Radius * Math.Sin(obj.StartAngle * Math.PI / 180);
            var eX = obj.Center.X + obj.Radius * Math.Cos(obj.EndAngle * Math.PI / 180);
            var eY = obj.Center.Y + obj.Radius * Math.Sin(obj.EndAngle * Math.PI / 180);
            var result = new[] { new Vector3 { X = sX, Y = sY }, new Vector3 { X = eX, Y = eY } };
            return result;
        }


        // returns both ends (co-ordinate) of arc.
        // returns start and end point at index 0 and 1 respectively
        public static Vector3[] GetVertexesOfArc(this Arc obj,bool isArcClockwise)
        {
            var sX = obj.Center.X + obj.Radius * Math.Cos(obj.StartAngle * Math.PI / 180);
            var sY = obj.Center.Y + obj.Radius * Math.Sin(obj.StartAngle * Math.PI / 180);
            var eX = obj.Center.X + obj.Radius * Math.Cos(obj.EndAngle * Math.PI / 180);
            var eY = obj.Center.Y + obj.Radius * Math.Sin(obj.EndAngle * Math.PI / 180);
            var result = isArcClockwise? new[] { new Vector3 { X = eX, Y = eY }, new Vector3 { X = sX, Y = sY } } : new[] { new Vector3 { X = sX, Y = sY }, new Vector3 { X = eX, Y = eY } };
            return result;
        }
        public static LwPolyline GetOffsetCurves(this LwPolyline obj, double offsetDist)
        {
            LwPolyline entityOffset = (LwPolyline)obj.Clone();

            entityOffset.Vertexes.Clear();

            for (int i = 0; i < obj.Vertexes.Count; i++)
            {
                if (i == (obj.Vertexes.Count - 1))
                {

                    entityOffset.Vertexes.Add(GetVertexOffset(obj.Vertexes[i - 1], obj.Vertexes[i], obj.Vertexes[i], offsetDist));
                    break;
                }
                entityOffset.Vertexes.Add(GetVertexOffset(obj.Vertexes[i], obj.Vertexes[i + 1], obj.Vertexes[i], offsetDist));
            }
            return entityOffset;
        }

        private static LwPolylineVertex GetVertexOffset(LwPolylineVertex startVertex, LwPolylineVertex endVertex, LwPolylineVertex middleVertex, double distance)
        {
            LwPolylineVertex middle = middleVertex;
            LwPolylineVertex vector = new LwPolylineVertex(startVertex.Position.X - endVertex.Position.X, startVertex.Position.Y - endVertex.Position.Y);
            LwPolylineVertex n = new LwPolylineVertex(-vector.Position.Y, vector.Position.X);

            double normLenght = Math.Sqrt((n.Position.X * n.Position.X) + (n.Position.Y * n.Position.Y));

            n.Position = new Vector2(n.Position.X / normLenght, n.Position.Y / normLenght);

            return new LwPolylineVertex(middle.Position.X + (distance * n.Position.X), middle.Position.Y + (distance * n.Position.Y));
        }
    }
}
