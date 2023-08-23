using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.Creative;
using Terraria.Graphics.Renderers;
using Terraria.ID;
using Terraria.ModLoader;

namespace PixelSandbox
{
    public class PSVectorField
    {

        static protected Dictionary<(int, int, Func<Vector2, Vector2>, int), PSVectorField> _cache = new();

        public int width;
        public int height;
        public Vector2 Size => new Vector2(width, height);
        protected List<(RenderTarget2D, RenderTarget2D)> dataCollection;


        /// <summary>
        /// 把CPU向量场更新固化成纹理反查像素来源，具有一定限制但是可以提升性能。
        /// 对于多对一的移动，采用蒙特卡洛采样来获取其中一种情况。
        /// </summary>
        /// <param name="samples"> 蒙特卡洛采样轮数 </param>
        public static PSVectorField VectorField(int width, int height, Func<Vector2, Vector2> func, int samples = 1)
        {
            if (_cache.ContainsKey((width, height, func, samples)))
                return _cache[(width, height, func, samples)];
            var instance = new PSVectorField(width, height, func, samples);
            _cache.Add((width, height, func, samples), instance);
            return instance;
        }

        protected PSVectorField(int width, int height, Func<Vector2, Vector2> func, int samples = 1)
        {
            this.width = width;
            this.height = height;
            dataCollection = new();
            for (int s = 0; s < samples; s++)
            {
                Vector2[] from = new Vector2[width * height];
                Vector2[] togo = new Vector2[width * height];
                int[] dupCount = new int[width * height];
                Point[] dupLast = new Point[width * height];
                Vector2 offset = Vector2.One * 0.5f / Size;
                Vector2 pffset = Vector2.One * 0.5f / Size * 0;
                for (int j = 0; j < height; j++)
                    for (int i = 0; i < width; i++)
                    {
                        dupCount[i + j * width] = 0;
                        from[i + j * width].X = -1;
                    }
                for (int j = 0; j < height; j++)
                    for (int i = 0; i < width; i++)
                    {
                        Vector2 uv = new Vector2(i, j) / Size + offset;
                        Vector2 targetUV = func(uv);
                        Point targetPoint = (targetUV * Size + pffset).ToPoint();
                        targetUV = targetPoint.ToVector2() / Size + offset;
                        if (targetPoint.X < 0 || targetPoint.X >= width || targetPoint.Y < 0 || targetPoint.Y >= height)
                            continue;
                        int target = targetPoint.X + targetPoint.Y * width;
                        dupCount[target]++;
                        if (Main.rand.NextBool(dupCount[target]))
                        {
                            if (dupCount[target] > 1)
                            {
                                Point last = dupLast[target];
                                if (from[last.X + last.Y * width].X < 0)
                                    from[last.X + last.Y * width] = last.ToVector2() / Size + offset;
                                togo[last.X + last.Y * width] = last.ToVector2() / Size + offset;
                            }
                            from[target] = uv;
                            togo[i + j * width] = targetUV;
                            dupLast[target] = new Point(i, j);
                        }
                        else
                        {
                            if (from[i + j * width].X <= 0)
                                from[i + j * width] = uv;
                            togo[i + j * width] = uv;
                        }

                    }
                for (int j = 0; j < height; j++)
                    for (int i = 0; i < width; i++)
                    {
                        Vector2 uv = new Vector2(i, j) / Size + offset;
                        Point point = (uv * Size + pffset).ToPoint();
                        Vector2 targetUV = togo[point.X + point.Y * width];
                        Point targetPoint = (targetUV * Size + pffset).ToPoint();
                        if (uv != targetUV)
                            Debug.Assert(uv == from[targetPoint.X + targetPoint.Y * width]);
                    }
                for (int j = 0; j < height; j++)
                    for (int i = 0; i < width; i++)
                    {
                        Vector2 uv = new Vector2(i, j) / Size + offset;
                        from[i + j * width] -= uv;
                        togo[i + j * width] -= uv;
                    }
                RenderTarget2D fromTex = new RenderTarget2D(Main.graphics.GraphicsDevice, width, height, false,
                    SurfaceFormat.Vector2, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
                fromTex.SetData(from);
                RenderTarget2D togoTex = new RenderTarget2D(Main.graphics.GraphicsDevice, width, height, false,
                    SurfaceFormat.Vector2, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
                togoTex.SetData(togo);
                dataCollection.Add((fromTex, togoTex));
            }
        }

        public (Texture2D, Texture2D) GetTexture(int index = -1)
        {
            if (index < 0 || index >= dataCollection.Count)
                return dataCollection.Count == 0 ? (null, null) : dataCollection[Main.rand.Next(dataCollection.Count)];
            return dataCollection[index];
        }
    }
}