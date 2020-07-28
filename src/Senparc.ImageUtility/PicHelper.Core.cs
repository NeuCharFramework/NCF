namespace Senparc.ImageUtility
{
    public partial class PicHelper
    {
        //public static void Watermark(string picPath, Stream stream, string markText, int floatX, int floatY, Color color, float fontsize = 14.0f)
        //{
        //    string filepath = System.Web.HttpContext.Current.Server.MapPath(picPath);

        //    System.Drawing.Image myimage = System.Drawing.Image.FromFile(filepath);
        //    //创建Image对象
        //    Bitmap mybitmap = new Bitmap(myimage,/* 200, 200);*/myimage.Width, myimage.Height);
        //    //创建Bitmap对象

        //    Graphics g = Graphics.FromImage(mybitmap);
        //    //创建了Graphics对象

        //    //float fontsize = 8.0f;//字体大小
        //    float fongwidth = fontsize * (fontsize * 4);//23;//字体一共的宽度
        //    float fontheight = fontsize + 100;//字体的高度
        //    Font myfont = new Font("宋体", fontsize, FontStyle.Bold);
        //    //定义一个Font 文字的式样，可以再下面写上去

        //    RectangleF Rf = new RectangleF(floatX, floatY, fongwidth, fontheight);

        //    //创建了一组个矩行框,注意这里的RectangleF，这里是的F 表示存储一组Float的数据，如果是整数就直接用Rectangel就可以了
        //    Brush thisbrush = new SolidBrush(color);
        //    //定义一个Brush笔刷，和笔刷的式样为SolidBrush,还有刷出来的颜色为 #ff00cc

        //    g.DrawString(markText, myfont, thisbrush, Rf);
        //    //画上了
        //    g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;

        //    //MemoryStream ms = new MemoryStream();
        //    //mybitmap.Save(Server.MapPath("index2.jpg"), ImageFormat.Jpeg);
        //    mybitmap.Save(stream, ImageFormat.Jpeg);
        //    stream.Position = 0;
        //    //HttpContext.Current.Response.Clear();
        //    //HttpContext.Current.Response.ContentType = "image/jpeg";
        //    //HttpContext.Current.Response.BinaryWrite(ms.ToArray());
        //}
    }
}
