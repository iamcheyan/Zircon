using System.Drawing;

namespace Zircon.BotRunner;

/// <summary>
/// 同地图网格寻路(A*, 8 方向)。BotMap 提供可行走位, 服务端仍是移动权威,
/// 这里只负责让 MoveToward 的贪心直线步进升级为绕障路径。
/// 内置卡死检测: 沿路径走若干步位置不变 → 失效重算; 连续卡死 → 放弃并
/// 由行为层回退(服务端 AutoPath / 回城)。
/// </summary>
public sealed class BotPathfinder
{
    private const int MaxExpandNodes = 500000;
    private const int MaxPathLength = 400;

    /// <summary>运行时屏蔽格(服务端动态占位: 玩家/怪/NPC 站位不在
    /// .map 文件里, 被 server 拒收的格子加入此处强制绕行)。</summary>
    public readonly HashSet<Point> RuntimeBlocked;

    private readonly BotMap _map;

    public BotPathfinder(BotMap map, HashSet<Point> runtimeBlocked = null)
    {
        _map = map;
        RuntimeBlocked = runtimeBlocked ?? new HashSet<Point>();
    }

    private List<Point> _path = new();
    private int _pathIndex;
    private Point _goal;
    private int _stuckSteps;
    private int _repaths;

    public Point Goal => _goal;
    public bool HasPath => _path.Count > 0 && _pathIndex < _path.Count;
    public int RepathCount => _repaths;
    public int CurrentIndex => _pathIndex;
    public int PathLength => _path.Count;

    /// <summary>设定目标并计算路径。目标不可走时找附近最近可走点。</summary>
    public bool SetDestination(Point from, Point to)
    {
        _goal = to;
        _pathIndex = 0;
        _stuckSteps = 0;
        _path = FindPath(from, to);
        return _path.Count > 0;
    }

    /// <summary>
    /// 取下一步。偏离路径(被挤开/服务端纠正)时重算。返回 false 表示无路可走。
    /// </summary>
    public bool TryGetStep(Point current, out Point step)
    {
        step = Point.Empty;
        if (!HasPath) return false;

        // 已到路径终点附近
        if (Distance(current, _goal) <= 1) return false;

        // 偏离路径超过 2 格(服务器纠正/被怪推挤): 从最近路径点重算
        int nearest = NearestPathIndex(current);
        if (Distance(current, _path[nearest]) > 2)
        {
            _repaths++;
            _path = FindPath(current, _goal);
            _pathIndex = 0;
            if (_path.Count == 0) return false;
            nearest = 0;
        }
        _pathIndex = Math.Max(_pathIndex, nearest);

        if (_pathIndex >= _path.Count) return false;
        step = _path[_pathIndex];
        return true;
    }

    /// <summary>走了一步(或没走动)后推进游标。返回 true=仍可继续沿路径。</summary>
    public bool Advance(Point current, bool moved)
    {
        if (!moved)
        {
            if (++_stuckSteps >= 8) // ~2s 无位移(服务端步时 600ms)
            {
                // 卡死自愈: 重算路径; 连续重算仍卡 → 让行为层放弃本目的地
                _repaths++;
                bool recoverable = _repaths < 3;
                _stuckSteps = 0;
                _path = FindPath(current, _goal);
                _pathIndex = 0;
                return recoverable && _path.Count > 0;
            }
            return true;
        }
        _stuckSteps = 0;
        while (_pathIndex < _path.Count && Distance(current, _path[_pathIndex]) <= 1)
            _pathIndex++;
        return true;
    }

    public void Reset()
    {
        _path = new List<Point>();
        _pathIndex = 0;
        _stuckSteps = 0;
        _repaths = 0;
        _goal = Point.Empty;
    }

    private int NearestPathIndex(Point current)
    {
        int best = _pathIndex, bestDist = int.MaxValue;
        for (int i = _pathIndex; i < _path.Count; i++)
        {
            int d = Distance(current, _path[i]);
            if (d < bestDist) { bestDist = d; best = i; }
            if (bestDist <= 1) break;
        }
        return best;
    }

    private static int Distance(Point a, Point b) => Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));

    /// <summary>A* 主过程。地图为空(解析失败)时退化为直线两步路径。</summary>
    private List<Point> FindPath(Point start, Point goal)
    {
        if (_map == null) return new List<Point> { goal };
        if (Distance(start, goal) <= 1) return new List<Point>();

        var to = WalkableNearest(goal);
        if (!to.HasValue) return new List<Point>();
        goal = to.Value;
        if (Distance(start, goal) <= 1) return new List<Point> { goal };

        var from = WalkableNearest(start) ?? start;

        var open = new PriorityQueue();           // 二叉堆, key = f
        var gScore = new Dictionary<Point, int>();
        var cameFrom = new Dictionary<Point, Point>();
        var closed = new HashSet<Point>();

        gScore[from] = 0;
        open.Push(from, Heuristic(from, goal));
        int expanded = 0;

        while (open.Count > 0 && expanded < MaxExpandNodes)
        {
            var current = open.Pop();
            if (closed.Contains(current)) continue;
            closed.Add(current);
            expanded++;

            if (Distance(current, goal) <= 1)
                return Reconstruct(cameFrom, current);

            for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                var next = new Point(current.X + dx, current.Y + dy);
                if (closed.Contains(next) || !_map.CanWalk(next)) continue;
                // 注: 服务端 Walk 只校验目标格(MonsterObject.Walk →
                // cell.IsBlocking), 无墙角规则; 此前的禁斜穿切断了
                // 纯斜向阶梯走廊(如 MrKang→Lennard 一线), 已移除。

                // 动态占位(怪/人/NPC)拉黑是瞬态的: 软代价而非硬排除,
                // 否则补给街多 bot 互堵会把 A* 围死(茧房), 连 3 格目标
                // 都找不到路。有替代路线时必然绕开(+80 ≈ 6 格绕行)。
                int stepCost = (dx != 0 && dy != 0) ? 14 : 10;
                if (RuntimeBlocked.Contains(next)) stepCost += 80;
                int newG = gScore[current] + stepCost;
                if (gScore.TryGetValue(next, out int old) && old <= newG) continue;

                gScore[next] = newG;
                cameFrom[next] = current;
                open.Push(next, newG + Heuristic(next, goal));
            }
        }
        return new List<Point>();
    }

    private static int Heuristic(Point a, Point b)
    {
        int dx = Math.Abs(a.X - b.X), dy = Math.Abs(a.Y - b.Y);
        return 10 * Math.Max(dx, dy) + 4 * Math.Min(dx, dy);
    }

    private List<Point> Reconstruct(Dictionary<Point, Point> cameFrom, Point current)
    {
        var path = new List<Point>();
        while (cameFrom.TryGetValue(current, out var prev))
        {
            path.Add(current);
            current = prev;
        }
        path.Reverse();
        // 长路径保留"头部"(离当前位置近的一段): 尾部截断会把起点掐掉,
        // 游标对不上导致对 30 格外的中间点贪心撞墙。走完一段后由上层
        // 重算下一段(分块逼近)。
        if (path.Count > MaxPathLength) path = path.GetRange(0, MaxPathLength);
        return path;
    }

    private Point? WalkableNearest(Point p)
    {
        if (_map.CanWalk(p)) return p;
        for (int r = 1; r <= 6; r++)
        {
            for (int dx = -r; dx <= r; dx++)
            for (int dy = -r; dy <= r; dy++)
            {
                if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != r) continue;
                var q = new Point(p.X + dx, p.Y + dy);
                if (_map.CanWalk(q)) return q;
            }
        }
        return null;
    }

    /// <summary>极简二叉最小堆。</summary>
    private sealed class PriorityQueue
    {
        private readonly List<(Point P, int F)> _heap = new();

        public int Count => _heap.Count;

        public void Push(Point p, int f)
        {
            _heap.Add((p, f));
            int i = _heap.Count - 1;
            while (i > 0)
            {
                int parent = (i - 1) / 2;
                if (_heap[parent].F <= _heap[i].F) break;
                (_heap[parent], _heap[i]) = (_heap[i], _heap[parent]);
                i = parent;
            }
        }

        public Point Pop()
        {
            var top = _heap[0];
            _heap[0] = _heap[^1];
            _heap.RemoveAt(_heap.Count - 1);
            int i = 0;
            while (true)
            {
                int l = i * 2 + 1, r = l + 1, small = i;
                if (l < _heap.Count && _heap[l].F < _heap[small].F) small = l;
                if (r < _heap.Count && _heap[r].F < _heap[small].F) small = r;
                if (small == i) break;
                (_heap[small], _heap[i]) = (_heap[i], _heap[small]);
                i = small;
            }
            return top.P;
        }
    }
}
