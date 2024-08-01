//#define DEBUG_BRESENHAM
using System.Drawing;
using static System.Math;
#if DEBUG_BRESENHAM
using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
#endif

namespace Canyon.Shared.Mathematics
{
    public class Bresenham
    {
#if DEBUG_BRESENHAM
        private static readonly ILogger logger = LogFactory.CreateLogger<Bresenham>();
#endif

        private Bresenham() { }

        public static List<Point> Calculate(int x0, int y0, int x1, int y1)
        //(int x, int y, int targetX, int targetY, int length)
        {
            //List<Point> points;
            //int dx = Math.Abs(targetX - x);
            //int dy = Math.Abs(targetY - y);
            //// If slope is less than one
            //if (dx > dy)
            //{
            //    // passing argument as 0 to plot(x,y)
            //    points = GetPoints(x, y, targetX, targetY, dx, dy, 0);
            //}
            //// if slope is greater than or equal to 1
            //else
            //{
            //    // passing argument as 1 to plot (y,x)
            //    points = GetPoints(y, x, targetY, targetX, dy, dx, 1);
            //    points = points.Select(x => new Point(x.Y, x.X)).ToList();
            //}
            List<Point> points = new();

            int dx = Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
            int dy = -Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
            int err = dx + dy, e2; /* error value e_xy */

            for (; ; )
            {  /* loop */
                //setPixel(x0, y0);
                points.Add(new Point(x0, y0));
                if (x0 == x1 && y0 == y1)
                {
                    break;
                }

                e2 = 2 * err;
                if (e2 >= dy) { err += dy; x0 += sx; } /* e_xy+e_x > 0 */
                if (e2 <= dx) { err += dx; y0 += sy; } /* e_xy+e_y < 0 */
            }

#if DEBUG_BRESENHAM
            //StringBuilder stringBuilder = new();
            //for (int i = 0; i < points.Count; i++)
            //{
            //    Point point = points[i];
            //    stringBuilder.Append($"{i} >>> X:{point.X},Y:{point.Y}\n");
            //}
            //logger.LogDebug("From[{x1},{y1}] To[{x2},{y2}]: \n{}", x0, y0, x1, y1, stringBuilder.ToString());
#endif

            return points;
        }

        //private static List<Point> GetPoints(int x1, int y1, int x2,
        //                         int y2, int dx, int dy,
        //                         int decide)
        //{
        //    List<Point> points = new();
        //    // pk is initial decision making parameter
        //    // Note:x1&y1,x2&y2, dx&dy values are interchanged
        //    // and passed in plotPixel function so
        //    // it can handle both cases when m>1 & m<1
        //    int pk = 2 * dy - dx;
        //    for (int i = 0; i <= dx; i++)
        //    {
        //        points.Add(new Point(x1, y1));
        //        // checking either to decrement or increment the
        //        // value if we have to plot from (0,100) to
        //        // (100,0)
        //        if (x1 < x2)
        //        {
        //            x1++;
        //        }
        //        else
        //        {
        //            x1--;
        //        }

        //        if (pk < 0)
        //        {
        //            // decision value will decide to plot
        //            // either  x1 or y1 in x's position
        //            if (decide == 0)
        //            {
        //                pk += 2 * dy;
        //            }
        //            else
        //            {
        //                pk += 2 * dy;
        //            }
        //        }
        //        else
        //        {
        //            if (y1 < y2)
        //            {
        //                y1++;
        //            }
        //            else
        //            {
        //                y1--;
        //            }

        //            pk = pk + 2 * dy - 2 * dx;
        //        }
        //    }
        //    return points;
        //}

        public static List<Point> CalculateThick(int x0, int y0, int x1, int y1, double wd)
        {
#if DEBUG_BRESENHAM
            Stopwatch sw = Stopwatch.StartNew();
            int bx = x0;
            int by = y0;
#endif

            int dx = Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
            int dy = Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
            int err = dx - dy, e2, x2, y2;                          /* error value e_xy */
            double ed = dx + dy == 0 ? 1 : Sqrt((float)dx * dx + (float)dy * dy);
            List<Point> points = new();
            for (wd = (wd + 1) / 2; ;)
            {                                   /* pixel loop */
                points.Add(new Point(x0, y0));
                e2 = err; x2 = x0;
                if (2 * e2 >= -dx)
                {                                           /* x step */
                    for (e2 += dy, y2 = y0; e2 < ed * wd && (y1 != y2 || dx > dy); e2 += dx)
                    {
                        points.Add(new Point(x0, y2 += sy));
                    }
                    if (x0 == x1)
                    {
                        break;
                    }

                    e2 = err; err -= dy; x0 += sx;
                }
                if (2 * e2 <= dy)
                {                                            /* y step */
                    for (e2 = dx - e2; e2 < ed * wd && (x1 != x2 || dx < dy); e2 += dy)
                    {
                        points.Add(new Point(x2 += sx, y0));
                    }
                    if (y0 == y1)
                    {
                        break;
                    }

                    err += dx; y0 += sy;
                }
            }

#if DEBUG_BRESENHAM
            sw.Stop();
            StringBuilder stringBuilder = new();
            for (int i = 0; i < points.Count; i++)
            {
                Point point = points[i];
                stringBuilder.Append($"{i:00} >>> X:{point.X},Y:{point.Y}\n");
            }
            logger.LogDebug("Bresenham Tick({wd}) From[{x1},{y1}] To[{x2},{y2}](Distance: {dist}): \n{}\n{ticks} ticks", wd, bx, by, x1, y1, Calculations.GetDistance(bx, by, x1, y1), stringBuilder.ToString(), sw.ElapsedTicks);
#endif
            return points;
        }
    }
}
