using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RunnersAndLadders
{
    partial class Game
    {
        enum Direction { Left, Right, Up, Down }
        abstract class Character : IDisposable
        {
            private static readonly Color transColor = Color.FromArgb(255, 255, 255);
            //图元
            private static Dictionary<Direction, Bitmap> playerPictures =
                new Dictionary<Direction, Bitmap>(),
                redOnePictures =
                new Dictionary<Direction, Bitmap>();
            private static Bitmap deadPlayerPicture;
            public static void InitializePictures()
            {
                Bitmap PathToBitMap(string characterType, string direction)
                {
                    Bitmap pic = new Bitmap(new Bitmap($@"Characters\{characterType}_{direction}.bmp"),
                    Field.UnitSize, Field.UnitSize);

                    pic.MakeTransparent();
                    for (int i = 0; i < pic.Width; ++i)
                        for (int j = 0; j < pic.Height; ++j)
                            if (pic.GetPixel(i, j) == transColor)
                                pic.SetPixel(i, j, Color.Transparent);
                    return pic;
                }
                foreach (Direction item in Enum.GetValues(typeof(Direction)))
                {
                    playerPictures.Add(item, PathToBitMap("Player", item.ToString()));
                    redOnePictures.Add(item, PathToBitMap("RedOne", item.ToString()));
                    deadPlayerPicture = PathToBitMap("Player", "Dead");
                }
            }
            //图片与控件
            protected PictureBox Container { get; }
            protected Game LinkedGame { get; }
            protected Field LinkedField => LinkedGame.LinkedField;
            public bool Visible
            {
                get => Container.Visible;
                set => Container.Visible = value;
            }
            //基本属性
            public abstract bool IsPlayer { get; }
            public abstract int MoveTime { get; } //走过一格所需时间
            private const int StepDistance =
                Field.UnitSize / Game.stepsForOneGrid; //分步移动的步长
            private readonly int delay; //分布移动的间隔
                                        //临时属性
            private Direction face = Direction.Right;
            public Direction Face
            {
                get => face;
                protected set
                {
                    face = value;
                    Container.Image = Picture;
                }
            }
            protected Bitmap Picture => IsPlayer ? (dead ? deadPlayerPicture : playerPictures[Face])
                : redOnePictures[Face];
            private bool dead = false;
            public bool Dead
            {
                protected get => dead;
                set
                {
                    dead = value;
                    Container.Image = Picture;
                }
            }
            public int X { get; protected set; } = 0;
            public int Y { get; protected set; } = 0;
            public Character(Game newGame)
            {
                LinkedGame = newGame;
                delay = MoveTime / Game.stepsForOneGrid;
                Container = new PictureBox()
                {
                    Parent = LinkedField.Box,
                    BackColor = Color.Transparent,
                    Top = 0,
                    Left = 0,
                    Width = Picture.Width,
                    Height = Picture.Height,
                    Image = Picture,
                    Visible = true
                };
            }
            public void Dispose() => LinkedField.Box.Controls.Remove(Container);
            protected bool AtBottom => Y == LinkedField.Height - 1;
            protected bool TryFall()
            {
                if (AtBottom)
                    return false;
                GridType onCharacter = LinkedField.GetGrid(X, Y);
                GridType BelowCharacter = LinkedField.GetGrid(X, Y + 1);
                //在梯子和棍子上
                bool canGrab = onCharacter == GridType.Ladder || onCharacter == GridType.Stick;
                //脚下有支持
                bool OnGround = BelowCharacter == GridType.Ladder || BelowCharacter == GridType.Stone
                    || BelowCharacter == GridType.Brick;
                //在陷阱中的红人
                bool redOneInTrap = !IsPlayer && onCharacter == GridType.Trap;
                //陷阱中的红人做支持
                bool stepOnRedOne = LinkedGame.IsAnyOne(true, match => match.X == X && match.Y == Y + 1)
                    && BelowCharacter == GridType.Trap;
                if (!canGrab && !OnGround && !redOneInTrap && !stepOnRedOne)
                {
                    Face = Direction.Down;
                    TryMove(Direction.Down);
                    return true;
                }
                else
                    return false;
            }
            public void TeleportTo(int x, int y)
            {
                X = x;
                Y = y;
                Container.Top = Y * Field.UnitSize;
                Container.Left = X * Field.UnitSize;
                TryFall();
            }
            //返回是否成功移动
            public bool TryMove(Direction direction)
            {
                int playerX = X, playerY = Y;
                GridType onCharacter = LinkedField.GetGrid(playerX, playerY);
                //不在梯子上那么不能往上走
                if (direction == Direction.Up && onCharacter != GridType.Ladder)
                    return false;
                //如果脸与原方向不一致，则先转脸
                if (Face != direction)
                {
                    Face = direction;
                    return true;
                }
                //脸与原方向一致，需要移动
                //得到理论将要到达的位置
                switch (direction)
                {
                    case Direction.Left:
                        --playerX;
                        break;
                    case Direction.Right:
                        ++playerX;
                        break;
                    case Direction.Up:
                        --playerY;
                        break;
                    case Direction.Down:
                        ++playerY;
                        break;
                }
                //移出必需限制在边界内
                if (!(playerX >= 0 && playerX <= LinkedField.Width - 1 &&
                    playerY >= 0 && playerY <= LinkedField.Height - 1))
                    return false;
                GridType target = LinkedField.GetGrid(playerX, playerY);
                //不能被阻挡
                if (target == GridType.Brick || target == GridType.Stone)
                    return false;
                //成功移动
                Move(direction);
                //进入新格子发生事件
                bool causeDead = ArriveInNewGrid(playerX, playerY);
                //可能下坠
                if (!causeDead)
                    TryFall();
                return true;
            }
            protected void Move(Direction direction)
            {
                switch (direction)
                {
                    case Direction.Down:
                        {
                            int top = Y * Field.UnitSize;
                            for (int i = 0; i < Game.stepsForOneGrid; ++i)
                            {
                                Container.Top = i * StepDistance + top;
                                Task.Delay(delay).Wait();
                            }
                            Container.Top = (++Y) * Field.UnitSize;
                        }
                        break;
                    case Direction.Up:
                        {
                            int top = Y * Field.UnitSize;
                            for (int i = 0; i < Game.stepsForOneGrid; ++i)
                            {
                                Container.Top = -i * StepDistance + top;
                                Task.Delay(delay).Wait();
                            }
                            Container.Top = (--Y) * Field.UnitSize;
                        }
                        break;
                    case Direction.Right:
                        {
                            int left = X * Field.UnitSize;
                            for (int i = 0; i < Game.stepsForOneGrid; ++i)
                            {
                                Container.Left = i * StepDistance + left;
                                Task.Delay(delay).Wait();
                            }
                            Container.Left = (++X) * Field.UnitSize;
                        }
                        break;
                    case Direction.Left:
                        {
                            int left = X * Field.UnitSize;
                            for (int i = 0; i < Game.stepsForOneGrid; ++i)
                            {
                                Container.Left = -i * StepDistance + left;
                                Task.Delay(delay).Wait();
                            }
                            Container.Left = (--X) * Field.UnitSize;
                        }
                        break;
                }
            }
            //返回进入新格子是否中断计划
            protected abstract bool ArriveInNewGrid(int x, int y);
        }
        class Player : Character
        {
            public override bool IsPlayer => true;
            public override int MoveTime => Game.PlayerMoveTime;
            public Player(Game newGame) : base(newGame) { }
            public bool TryDig(Direction direction)
            {
                if (AtBottom || direction == Direction.Up || direction == Direction.Down)
                    return false;
                int x, y = Y + 1;
                GridType side, nearFeet;
                if (Face == Direction.Left)
                {
                    if (X == 0)
                        return false;
                    side = LinkedField.GetGrid(x = X - 1, Y);
                    nearFeet = LinkedField.GetGrid(X - 1, Y + 1);
                }
                else
                {
                    if (X == LinkedField.Width - 1)
                        return false;
                    side = LinkedField.GetGrid(x = X + 1, Y);
                    nearFeet = LinkedField.GetGrid(X + 1, Y + 1);
                }
                bool block = side == GridType.Brick || side == GridType.Stone || side == GridType.Ladder;
                if (nearFeet == GridType.Brick && !block)
                {
                    LinkedField.SetGrid(GridType.Trap, x, y);
                    TryFall();
                    Task.Factory.StartNew(() =>
                    {
                    //被挖去的砖块恢复的步数 
                    const int Step = Game.BrickRecoverTime / Game.MaxInterval + 1,
                            ActuelInterval = Game.BrickRecoverTime / Step;
                        for (int i = 0; i < Step; ++i)
                        {
                            Task.Delay(ActuelInterval).Wait();
                            if (LinkedGame.GameEnd)
                                return;
                        }
                        LinkedField.SetGrid(GridType.Brick, x, y);
                    //洞填上会杀死角色
                    foreach (var character in LinkedGame.AnyOne(false,
                            match => match.X == x && match.Y == y))
                            character.Dead = true;
                    });
                    return true;
                }
                else
                    return false;
            }
            protected override bool ArriveInNewGrid(int x, int y)
            {
                //同在一格则被红人抓到
                if (LinkedGame.IsAnyOne(true, match => match.X == x && match.Y == y))
                {
                    Dead = true;
                    LinkedGame.RoundEnd = true;
                }
                else
                {
                    GridType target = LinkedField.GetGrid(x, y);
                    //吃掉金子
                    if (target == GridType.Gold)
                    {
                        LinkedField.SetGrid(GridType.Air, x, y);
                        LinkedGame.PickGold();
                        if (LinkedGame.AllGoldPicked)
                            LinkedGame.GameEnd = true;
                    }
                }
                if (LinkedGame.GameEnd || LinkedGame.RoundEnd)
                    return true;
                else
                    return false;
            }
        }
        class RedOne : Character
        {
            public override bool IsPlayer => false;
            public override int MoveTime => Game.RedOneMoveTime;
            public RedOne(Game newGame) : base(newGame) { }
            public void TryCatchPlayer()
            {
                Task.Delay(500).Wait();
                while (!LinkedGame.GameEnd && !LinkedGame.RoundEnd)
                {
                    if (!TryMove(Direction.Left))
                        return;
                    //TryMove(Direction.Left);
                }
            }
            protected override bool ArriveInNewGrid(int x, int y)
            {
                //同在一格则抓到玩家
                Player player = LinkedGame.GetPlayer();
                if (player.X == x && player.Y == y)
                {
                    player.Dead = true;
                    LinkedGame.RoundEnd = true;
                }
                if (LinkedGame.GameEnd || LinkedGame.RoundEnd)
                    return true;
                else
                    return false;
            }
        }
    }
}