using System;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "Footprints", menuName = "Dungeon/CA/Rules/Footprints")]
public sealed class FootprintRuleCA : RuleCA
{
    public override Type CellType => typeof(CellDataCA);

    public override void Apply<TCell>(RuleContextCA<TCell> context)
    {
        DungeonSettingsCA settings = context.Settings;
        if (settings == null || context.States == null) throw new ArgumentException("Footprints require settings and state definitions.");
        var states = new List<CellStateCA>(context.States);
        int empty = states.IndexOf(settings.EmptyState);
        var cells = new CellDataCA[context.Width, context.Height];
        var reserved = new bool[context.Width, context.Height];
        var candidates = new List<Vector2Int>();
        for (int x = 0; x < context.Width; x++)
        {
            for (int y = 0; y < context.Height; y++)
            {
                int index = context.GetCell(x, y).StateIndex;
                if (context.GetCell(x, y) is CellDataCA previous && previous.IsFollower) index = empty;
                cells[x, y] = new CellDataCA(index, x, y);
                CellStateCA state = states[index];
                if (state.Footprint != Vector2Int.one || state.WallPadding > 0) candidates.Add(new Vector2Int(x, y));
            }
        }
        var random = new System.Random(unchecked(context.Seed ^ 0x46a38b17));
        Shuffle(candidates, random);
        foreach (Vector2Int anchor in candidates)
        {
            if (reserved[anchor.x, anchor.y]) continue;
            int index = context.GetCell(anchor.x, anchor.y).StateIndex;
            CellStateCA state = states[index];
            Vector2Int size = state.Footprint;
            int padding = state.WallPadding;
            bool fits = anchor.x >= padding && anchor.y >= padding
                && (long)anchor.x + size.x <= context.Width - padding && (long)anchor.y + size.y <= context.Height - padding;
            if (fits)
                for (int dx = 0; dx < size.x; dx++)
                    for (int dy = 0; dy < size.y; dy++) fits &= !reserved[anchor.x + dx, anchor.y + dy];
            if (!fits)
            {
                cells[anchor.x, anchor.y] = new CellDataCA(empty, anchor.x, anchor.y);
                continue;
            }
            int follow = states.IndexOf(state.FollowState);
            for (int dx = 0; dx < size.x; dx++)
            {
                for (int dy = 0; dy < size.y; dy++)
                {
                    bool follower = dx != 0 || dy != 0;
                    cells[anchor.x + dx, anchor.y + dy] = new CellDataCA(follower ? follow : index, anchor.x, anchor.y, follower);
                    reserved[anchor.x + dx, anchor.y + dy] = true;
                }
            }
        }
        Schedule(cells, states, settings, random);
        for (int x = 0; x < context.Width; x++) for (int y = 0; y < context.Height; y++) context.SetCell(x, y, cells[x, y]);
    }

    static void Schedule(CellDataCA[,] cells, List<CellStateCA> states, DungeonSettingsCA settings, System.Random random)
    {
        int width = cells.GetLength(0), height = cells.GetLength(1);
        int regionSize = settings.ProgressionRegionSize;
        int columns = (width - 1) / regionSize + 1;
        var regions = new SortedDictionary<int, List<Vector2Int>>();
        var waves = new int[width, height];
        int remaining = 0;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                waves[x, y] = -1;
                CellDataCA cell = cells[x, y];
                if (cell.IsFollower || !(states[cell.StateIndex].PreviewAge || states[cell.StateIndex].ActivateAtMaturity)) continue;
                int region = y / regionSize * columns + x / regionSize;
                if (!regions.TryGetValue(region, out List<Vector2Int> anchors)) regions.Add(region, anchors = new List<Vector2Int>());
                anchors.Add(new Vector2Int(x, y));
                remaining++;
            }
        }
        var order = new List<List<Vector2Int>>(regions.Values);
        foreach (List<Vector2Int> region in order)
        {
            Shuffle(region, random);
            Vector2 center = new Vector2((region[0].x / regionSize + .5f) * regionSize, (region[0].y / regionSize + .5f) * regionSize);
            region.Sort((a, b) => ((Vector2)b - center).sqrMagnitude.CompareTo(((Vector2)a - center).sqrMagnitude));
        }
        Shuffle(order, random);
        int radius = Math.Min(settings.ProximityRadius, Math.Max(width, height));
        for (int wave = 0; remaining > 0; wave++)
        {
            foreach (List<Vector2Int> region in order)
            {
                for (int i = region.Count - 1; i >= 0; i--)
                {
                    Vector2Int anchor = region[i];
                    bool blocked = false;
                    int x0 = Math.Max(0, anchor.x - radius), x1 = (int)Math.Min(width - 1L, (long)anchor.x + radius);
                    int y0 = Math.Max(0, anchor.y - radius), y1 = (int)Math.Min(height - 1L, (long)anchor.y + radius);
                    for (int x = x0; x <= x1 && !blocked; x++)
                        for (int y = y0; y <= y1; y++) if (waves[x, y] == wave) { blocked = true; break; }
                    if (blocked) continue;
                    waves[anchor.x, anchor.y] = wave;
                    CellDataCA cell = cells[anchor.x, anchor.y];
                    float delay = settings.FirstSpawnDelay + wave * settings.SecondsBetweenWaves;
                    Vector2Int size = states[cell.StateIndex].Footprint;
                    for (int dx = 0; dx < size.x; dx++)
                        for (int dy = 0; dy < size.y; dy++) cells[anchor.x + dx, anchor.y + dy] = cells[anchor.x + dx, anchor.y + dy].WithTiming(delay, 0f);
                    region.RemoveAt(i);
                    remaining--;
                    break;
                }
            }
        }
    }

    static void Shuffle<T>(List<T> values, System.Random random)
    {
        for (int i = values.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            T value = values[i];
            values[i] = values[j];
            values[j] = value;
        }
    }
}
