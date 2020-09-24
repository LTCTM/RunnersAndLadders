using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace RunnersAndLadders
{
    partial class Game
    {
        public struct Coordinate
        {
            public int x, y;
        }
        enum GridType { Air, Gold, Ladder, Stick, Brick, Stone, Trap }
        class Field
        {
            //图元
            private static Dictionary<GridType, Bitmap> gridToElement =
                new Dictionary<GridType, Bitmap>();
            public static void InitializePicturePieces()
            {
                Bitmap PathToBitMap(string mapPath) =>
                    new Bitmap(new Bitmap($@"Elements\{mapPath}.bmp"),
                    UnitSize, UnitSize);
                foreach (var item in Enum.GetValues(typeof(GridType)))
                    gridToElement.Add((GridType)item, PathToBitMap(item.ToString()));
            }
            //颜色
            private static readonly Color playerInintialColor = Color.FromArgb(0, 0, 0);
            private static readonly Color redOneInintialColor = Color.FromArgb(255, 0, 64);
            private static readonly Dictionary<Color, GridType> colorToGrid =
                new Dictionary<Color, GridType>() {
                { Color.FromArgb(255,255,255), GridType.Air},   //白
                { Color.FromArgb(255,201,14), GridType.Stick},  //橘黄
                { Color.FromArgb(127,127,127), GridType.Stone}, //灰
                { Color.FromArgb(0,162,232), GridType.Ladder},  //青蓝
                { Color.FromArgb(34,177,76), GridType.Gold},    //绿
                { Color.FromArgb(163,61,39), GridType.Brick}    //砖色
                };

            //红人数量及初始坐标
            public Coordinate PlayerCoordinate { get; }
            public int RedOneCount { get; } = 0;
            private readonly Coordinate[] redOneCoordinates = new Coordinate[3];
            public Coordinate GetRedOneCoordinate(int index) => redOneCoordinates[index];
            //绘图工具
            public PictureBox Box { get; }
            public Bitmap Picture { get; }
            //绘图及数据
            private readonly Bitmap file;
            private GridType[,] grids;
            public int Width { get; }
            public int Height { get; }
            public const int UnitSize = 50;
            public int GoldCount { get; } = 0;
            //
            public Field(string path, PictureBox newBox)
            {
                //基本信息
                file = new Bitmap(path);
                Box = newBox;
                Width = file.Width;
                Height = file.Height;
                grids = new GridType[Width, Height];
                //绘画大小
                Picture = new Bitmap(UnitSize * Width, UnitSize * Height);
                //绘制底板
                for (int i = 0; i < Width; ++i)
                    for (int j = 0; j < Height; ++j)
                    {
                        Color fileGridData = file.GetPixel(i, j);
                        if (fileGridData == playerInintialColor)
                        {
                            PlayerCoordinate = new Coordinate()
                            {
                                x = i,
                                y = j
                            };
                            grids[i, j] = GridType.Air;
                        }
                        else if (fileGridData == redOneInintialColor)
                        {
                            redOneCoordinates[RedOneCount++] = new Coordinate()
                            {
                                x = i,
                                y = j
                            };
                            grids[i, j] = GridType.Air;
                        }
                        else
                        {
                            grids[i, j] = colorToGrid[fileGridData];
                            if (grids[i, j] == GridType.Gold)
                                ++GoldCount;
                        }
                        GridToPictureArea(i, j);
                    }
                Box.Image = Picture;
            }
            private void GridToPictureArea(int x, int y)
            {
                Bitmap element = gridToElement[grids[x, y]];
                for (int i = 0; i < UnitSize; ++i)
                    for (int j = 0; j < UnitSize; ++j)
                        Picture.SetPixel(x * UnitSize + i, y * UnitSize + j, element.GetPixel(i, j));
            }
            public GridType GetGrid(int x, int y) => grids[x, y];
            public void SetGrid(GridType into, int x, int y)
            {
                grids[x, y] = into;
                GridToPictureArea(x, y);
                Box.Image = Picture;
            }
        }
    }
}