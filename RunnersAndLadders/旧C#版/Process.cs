using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RunnersAndLadders
{
    partial class Game
    {
        //程序参数：长时间行动必须拆分，中途不断检测游戏条件
        private const int stepsForOneGrid = 5,      //角色前进一格分成的步数
            MaxInterval = 700,                      //两次检测之间最大停留时间
            WaitInterval = 200;                     //等待时重新检测游戏条件的间隔
        //游戏参数
        private const int BrickRecoverTime = 3000,  //被挖去的砖块恢复所需时间
            RedOneRecoverTime = 3000,               //红人爬出陷阱所需时间
            PlayerMoveTime = 400,                   //玩家前进一格所需时间
            RedOneMoveTime = 550,                   //红人前进一格所需时间
            RebornTime = 2000,                      //重生时间
            MaxCaughtTime = 2;                      //至多能被抓几次
                
        //资源初始化
        public static void InitializeResource()
        {
            Field.InitializePicturePieces();
            Character.InitializePictures();
        }
        //图形
        private Field LinkedField { get; }
        private readonly PictureBox baseBox;
        //主轴，返回是否胜利
        private readonly Task<bool> mainProcess;
        public async void WaitForMainProcess() => await mainProcess;
        //人物
        private readonly Player player;
        public Task PlayerTask { get; set; } = null;
        private readonly int redOneCount = 0;
        private readonly RedOne[] redOnes;
        private readonly Task[] redOneTasks;
        private Player GetPlayer() => player;
        private IEnumerable<RedOne> GetRedOnes() => redOnes;
        private IEnumerable<Character> GetEveryOne() => redOnes.Concat(new List<Character>{player});
        private IEnumerable<Character> AnyOne(bool redOnesonly, Func<Character, bool> matchFunc)
        {
            if (!redOnesonly)
            {
                if (matchFunc(player))
                    yield return player;
            }
            foreach (var redOne in redOnes.Where(matchFunc))
                yield return redOne;
        }
        private bool IsAnyOne(bool redOnesonly, Func<Character, bool> matchFunc)
        {
            if (!redOnesonly)
            {
                if (matchFunc(player))
                    return true;
            }
            return redOnes.Any(matchFunc);
        }
        //游戏状态
        public bool RoundEnd { get; set; } = false;
        public bool GameEnd { get; set; } = false;
        private int playerCaughtTime = 0;
        private int goldPicked = 0;
        private bool AllGoldPicked => goldPicked == LinkedField.GoldCount;
        private void PickGold() => ++goldPicked;
        public Game(string mapName,PictureBox box)
        {
            //图形
            baseBox = box;
            LinkedField = new Field($@"Maps\{mapName}.bmp", baseBox);
            //玩家
            player = new Player( this);
            //红人
            redOneCount = LinkedField.RedOneCount;
            redOnes = new RedOne[redOneCount];
            redOneTasks = new Task[redOneCount];
            for (int i = 0; i < redOneCount; ++i)
            {
                RedOne redOne = new RedOne(this);
                redOnes[i] = redOne;
            }
            //主轴相关
            void WaitOut()
            {
                if (PlayerTask != null)
                {
                    Task[] allTasks = new Task[redOneCount + 1];
                    for (int i = 0; i < redOneCount; ++i)
                        allTasks[i] = redOneTasks[i];
                    allTasks[redOneCount] = PlayerTask;
                    Task.WaitAll(allTasks);
                }
                else
                    Task.WaitAll(redOneTasks);
            }
            void GameStart()
            {
                //玩家设定为存活，红人设置为可见，重置坐标
                player.Dead = false;
                player.TeleportTo(LinkedField.PlayerCoordinate.x, LinkedField.PlayerCoordinate.y);
                for (int i = 0; i < redOneCount; ++i)
                {
                    RedOne redOne = redOnes[i];
                    var coordinate = LinkedField.GetRedOneCoordinate(i);
                    redOneTasks[i] = Task.Factory.StartNew(() =>
                    {
                        redOne.Visible = true;
                        redOne.TeleportTo(coordinate.x, coordinate.y);
                        redOne.TryCatchPlayer();
                    });
                }
                RoundEnd = false;
            }
            void RoundOver()
            {
                //人被抓
                ++playerCaughtTime;
                //等待大家走完路程
                WaitOut();
                //红人隐身，玩家变成死亡图标
                foreach (var character in redOnes)
                    character.Visible = false;
            }
            void GameOver()
            {
                player.Dispose();
                for (int i = 0; i < redOneCount; ++i)
                    redOnes[i].Dispose();
                WaitOut();
                if (AllGoldPicked)
                    MessageBox.Show("胜利！");
                else if (playerCaughtTime > MaxCaughtTime)
                    MessageBox.Show("失败！");
            }
            //主轴开始
            mainProcess = Task.Factory.StartNew(() =>
            {
                while(true)
                {
                    //游戏开始
                    GameStart();
                    while (!RoundEnd && !GameEnd)
                        Task.Delay(WaitInterval).Wait();
                    RoundOver();
                    //被抓太多则输
                    if (playerCaughtTime > MaxCaughtTime)
                        GameEnd = true;
                    //有可能游戏结束，包括但不限于如上原因
                    if (GameEnd)
                        break;
                    else
                    {
                        //等待一小会儿，准备下一轮行动
                        Task.Delay(RebornTime).Wait();
                    }
                }
                GameOver();
                return true;
            });
        }
        public void PlayerTryAct(Keys key)
        {
            if (GameEnd || RoundEnd)
                return;
            if (key == Keys.Space)
            {
                player.TryDig(Direction.Left);
            }
            else
            {
                Direction direction = Direction.Left;
                switch (key)
                {
                    case Keys.S:
                        direction = Direction.Down;
                        break;
                    case Keys.W:
                        direction = Direction.Up;
                        break;
                    case Keys.A:
                        direction = Direction.Left;
                        break;
                    case Keys.D:
                        direction = Direction.Right;
                        break;
                    default:
                        return;
                }
                player.TryMove(direction);
            }
        }
    }
}
