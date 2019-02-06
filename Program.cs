﻿﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;

namespace HyperSonic
{

    public static class ExtensionMethods
    {
        // Deep clone
        public static T DeepClone<T>(this T a)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, a);
                stream.Position = 0;
                return (T)formatter.Deserialize(stream);
            }
        }

        // public static List<int> Combinations(this List<int> list, int count) {

        // }
    }

    public class Entity
    {
        public int Type { get; }
        public int Owner { get; }
        public Position Position { get; }
        public int[] Params { get; }
        public Entity(int entityType, int owner, Position position, int param1, int param2)
        {
            Type = entityType;
            Owner = owner;
            Position = position;
            Params = new int[] { param1, param2 };
        }
    }
    public interface IReader
    {
        string ReadLine();
    }

    public class OnlineReader : IReader
    {
        public string ReadLine()
        {
            var nextLine = Console.ReadLine();
            Console.Error.WriteLine(nextLine);
            return nextLine;
        }
    }

    public class OfflineReader : IReader
    {
        private string[] lines;
        private int pointer = 0;
        public OfflineReader()
        {
            lines = File.ReadAllLines("input.txt");
        }
        public string ReadLine()
        {
            return lines[pointer++];
        }
    }
    public class Input
    {
        private int width;
        private int height;
        private int myId;
        private List<string> rows;
        private Dictionary<Position, List<Entity>> entities;
        private IReader reader;

        public Input(IReader reader)
        {
            this.reader = reader;
            entities = new Dictionary<Position, List<Entity>>();
            rows = new List<string>();
        }

        public void ReadRound()
        {
            rows.Clear();
            entities.Clear();

            for (int i = 0; i < height; i++)
            {
                string row = reader.ReadLine();
                AddRow(row);
            }
            int entitiesCount = int.Parse(reader.ReadLine());
            for (int i = 0; i < entitiesCount; i++)
            {
                var inputs = reader.ReadLine().Split(' ');
                int entityType = int.Parse(inputs[0]);
                int owner = int.Parse(inputs[1]);
                int x = int.Parse(inputs[2]);
                int y = int.Parse(inputs[3]);
                int param1 = int.Parse(inputs[4]);
                int param2 = int.Parse(inputs[5]);
                AddEntity(entityType, owner, x, y, param1, param2);
            }
        }

        public void AddRow(string row)
        {
            rows.Add(row);
        }

        public void AddEntity(int entityType, int owner, int x, int y, int param1, int param2)
        {
            var position = new Position(x, y);
            var entity = new Entity(entityType, owner, position, param1, param2);
            if (!entities.ContainsKey(position))
            {
                entities.Add(position, new List<Entity>());
            }
            entities[position].Add(entity);
        }

        public Map GetMap()
        {
            var map = new Map(myId);
            var y = 0;
            foreach (var row in rows)
            {
                var x = 0;
                foreach (var icon in row)
                {
                    var position = new Position(x, y);
                    var entities = GetEntities(position);
                    var field = FieldFactory.FromInput(icon, position, entities);
                    map.AddField(field);
                    x++;
                }
                y++;
            }
            map.ConnectFields();
            return map;
        }

        private List<Entity> GetEntities(Position position)
        {
            if (entities.ContainsKey(position))
            {
                return entities[position];
            }
            return null;
        }

        public void ReadInit()
        {
            var inputs = reader.ReadLine().Split(' ');
            width = int.Parse(inputs[0]);
            height = int.Parse(inputs[1]);
            myId = int.Parse(inputs[2]);
        }
    }

    [Serializable]
    public class Route
    {
        public string Id { get; }
        public int Steps { get; }

        public Route(string id, int steps)
        {
            Id = id;
            Steps = steps;
        }
    }

    [Serializable]
    public struct Position
    {
        public int X { get; }
        public int Y { get; }

        public Position(int x, int y)
        {
            X = x;
            Y = y;
        }

        public Position[] NeighborPositions()
        {
            return new Position[] {
                new Position(X - 1, Y),
                new Position(X, Y + 1),
                new Position(X + 1, Y),
                new Position(X, Y - 1)
            };
        }

        public override string ToString()
        {
            return $"[{X},{Y}]";
        }
    }

    [Serializable]
    public abstract class Field
    {
        public int FieldId { get; private set; }
        public Position Position { get; }
        public Field[] Neighbors { get; private set; }
        public List<Route> Routes { get; }
        public List<int> ExplodeAt { get; set; }

        // protected Func<int, Field> GetMyNeighbor;

        public Field(Position position)
        {
            FieldId = NextId;
            Position = position;
            Neighbors = new Field[4];
        }

        public abstract bool IsAccessible(int when);
        public abstract void SetExplodeAt(int direction, int range, int when);

        // public void AddNeighborFunc(Func<int, Field> func)
        // {
        //     GetMyNeighbor = func;
        // }

        public void AddNeighbor(int neighbor, Field field)
        {
            Neighbors[neighbor] = field;
        }

        public void ClearNeighbors()
        {
            Neighbors = new Field[4];
        }

        public virtual void SimulateRound(int round)
        {
            return;
        }

        public override string ToString()
        {
            return $"{GetType()}: {FieldId} {Position} [{string.Join(",", Neighbors.Select(n => n.FieldId))}]";
        }

        private static int nextId = 0;
        private static int NextId => nextId++;
    }

    [Serializable]
    public class Wall : Field
    {
        public Wall(Position position) : base(position) { }

        public override bool IsAccessible(int when) => false;
        public override void SetExplodeAt(int direction, int range, int when)
        {
            return;
        }
    }

    [Serializable]
    public class Floor : Field
    {
        public Box Box;
        public Player Player;
        public Item Item;
        public Bomb Bomb;

        public Floor(Position position) : base(position) { }

        public override bool IsAccessible(int when) => !HasBomb(when) && !HasBox(when);
        public bool HasPlayer(int time = 0) => Player?.IsPresent(time) ?? false;
        public bool HasBomb(int time = 0) => Bomb?.IsPresent(time) ?? false;
        public bool HasBox(int time = 0) => Box?.IsPresent(time) ?? false;
        public bool HasItem(int time = 0) => Item?.IsPresent(time) ?? false;

        public void AddEntity(Entity entity)
        {
            switch (entity.Type)
            {
                case 0: Player = new Player(entity); break;
                case 1: Bomb = new Bomb(entity); break;
                case 2: Item = new Item(entity); break;
                case 3: Box = new Box(entity.Position, entity.Params[0]); break;
                default: throw new ArgumentException(nameof(entity.Type));
            }
        }

        // public override void SimulateRound(int round)
        // {
        //     if (ExplodeAt != round)
        //     {
        //         return;
        //     }
        //     if (HasBomb)
        //     {
        //         Bomb = null;
        //     }
        //     if (HasItem)
        //     {
        //         Item = null;
        //     }
        //     if (HasPlayer)
        //     {
        //         Player = null;
        //     }
        //     if (HasBox)
        //     {
        //         var boxContent = Box.Content;
        //         Box = null;
        //         if (boxContent != 0)
        //         {
        //             Item = new Item(boxContent);
        //         }
        //     }
        // }

        public override void SetExplodeAt(int direction, int range, int when)
        {
            if (ExplodeAt == null)
            {
                ExplodeAt = new List<int>();
            }
            if (!ExplodeAt.Contains(when))
            {
                ExplodeAt.Add(when);
            }
            if (range == 0)
            {
                return;
            }
            if (HasBox(when))
            {
                Box.ExistsUntil = when;
                Item = new Item(Box.Content, when + 1);
                return;
            }
            if (HasItem(when))
            {
                Item.ExistsUntil = when;
                return;
            }
            if (HasBomb(when) && !Bomb.HasExploded)
            {
                Bomb.HasExploded = true;
                Bomb.ExistsUntil = when;
                for (int i = 0; i < 4; i++)
                {
                    if (Neighbors[i] == null)
                    {
                        continue;
                    }
                    Neighbors[i].SetExplodeAt(i, Bomb.ExplosionRange, when);
                }
            }
            else
            {
                if (Neighbors[direction] == null)
                {
                    return;
                }
                Neighbors[direction].SetExplodeAt(direction, range - 1, when);
            }
        }
    }

    // [Serializable]
    // public class Empty : Floor
    // {
    //     public Empty(Position position) : base(position) { }
    // }
    [Serializable]
    public class Obstacle
    {
        public int ExistsFrom { get; set; }
        public int? ExistsUntil { get; set; }

        public bool IsPresent(int when)
        {
            return ExistsFrom <= when && (ExistsUntil == null || ExistsUntil >= when);
        }
    }

    [Serializable]
    public class Box : Obstacle
    {
        public int Content { get; }
        public Box(Position position, int content)
        {
            Content = content;
            ExistsFrom = 0;
        }
    }

    [Serializable]
    public class Player : Obstacle
    {
        public int Id { get; }
        public int RemainingBombs { get; }
        public int ExplosionRange { get; }
        public Position Position { get; }
        public Player(Entity entity)
        {
            Id = entity.Owner;
            RemainingBombs = entity.Params[0];
            ExplosionRange = entity.Params[1];
            Position = entity.Position;
            ExistsFrom = 0;
        }
    }

    [Serializable]
    public class Bomb : Obstacle
    {
        public int Owner { get; }
        public int RoundsLeft { get; }
        public int ExplosionRange { get; }
        public bool HasExploded { get; set; }
        public Position Position { get; }
        public Bomb(Entity entity)
        {
            Owner = entity.Owner;
            RoundsLeft = entity.Params[0];
            ExplosionRange = entity.Params[1];
            Position = entity.Position;
            ExistsFrom = 0;
        }

        public Bomb(int owner, int roundsLeft, int range, Position position, int existsFrom)
        {
            Owner = owner;
            RoundsLeft = roundsLeft;
            ExplosionRange = range;
            Position = position;
            ExistsFrom = existsFrom;
        }
    }

    [Serializable]
    public class Item : Obstacle
    {
        public int Type { get; }
        public Item(Entity entity)
        {
            Type = entity.Params[0];
            ExistsFrom = 0;
        }

        public Item(int type, int existsFrom)
        {
            Type = type;
            ExistsFrom = existsFrom;
        }
    }

    public static class FieldFactory
    {

        public static Field FromInput(char icon, Position position, List<Entity> entities)
        {
            switch (icon)
            {
                case '.':
                    {
                        var floor = new Floor(position);
                        if (entities != null)
                        {
                            foreach (var entity in entities)
                            {
                                floor.AddEntity(entity);
                            }
                        }
                        return floor;
                    }
                case '0':
                case '1':
                case '2':
                    {
                        var floor = new Floor(position);
                        var boxEntity = new Entity(3, 0, position, int.Parse(icon.ToString()), 0);
                        floor.AddEntity(boxEntity);
                        return floor;
                    }
                case 'X': return new Wall(position);
                default: throw new ArgumentException(nameof(icon));
            }
            throw new ArgumentException(nameof(icon));
        }
    }

    [Serializable]
    public class Map
    {
        public Player Me { get; private set; }
        public List<Player> Others { get; }
        private Dictionary<Position, Field> fieldsByPosition;
        private Dictionary<int, Field> fieldsById;
        private List<Field> fields;
        private List<Bomb> bombs;
        private int myId;

        public Map(int myId)
        {
            fieldsByPosition = new Dictionary<Position, Field>();
            fieldsById = new Dictionary<int, Field>();
            fields = new List<Field>();
            Others = new List<Player>();
            bombs = new List<Bomb>();
            this.myId = myId;
        }

        public void AddField(Field field)
        {
            // field.AddNeighborFunc(id => fieldsById[id]);
            fields.Add(field);
            fieldsByPosition.Add(field.Position, field);
            fieldsById.Add(field.FieldId, field);

            if (field is Floor)
            {
                var floor = (Floor)field;
                if (floor.HasPlayer())
                {
                    if (floor.Player.Id == myId)
                    {
                        Me = floor.Player;
                    }
                    else
                    {
                        Others.Add(floor.Player);
                    }
                }
                if (floor.HasBomb())
                {
                    bombs.Add(floor.Bomb);
                    bombs = bombs.OrderBy(b => b.RoundsLeft).ToList();
                }
            }
        }

        public void AddBomb(Bomb bomb)
        {
            ((Floor)fieldsByPosition[bomb.Position]).Bomb = bomb;
            bombs.Add(bomb);
        }

        public Map Clone()
        {
            return this.DeepClone();
        }

        public void ConnectFields()
        {
            foreach (var field in fields)
            {
                var neighborPositions = field.Position.NeighborPositions();
                for (int i = 0; i < 4; i++)
                {
                    var neighborField = fieldsByPosition.GetValueOrDefault(neighborPositions[i]);
                    if (neighborField != null)
                    {
                        field.AddNeighbor(i, neighborField);
                    }
                }
            }
        }

        public void CalculateExplosionTimes()
        {
            fields.ForEach(field => field.ExplodeAt = null);
            foreach (var bomb in bombs)
            {
                if (bomb.HasExploded)
                {
                    continue;
                }
                bomb.HasExploded = true;
                for (int i = 0; i < 4; i++)
                {
                    fieldsByPosition[bomb.Position].SetExplodeAt(i, bomb.ExplosionRange, bomb.RoundsLeft);
                }
            }
        }

        public void SimulateRound(int round)
        {
            foreach (var field in fields)
            {
                field.SimulateRound(round);
            }
            ConnectFields();
        }

        public List<List<int>> WaysOutFrom(Position position, int routeLength, int distance, int bombsLeft, bool searchUntilFirst)
        {
            var currentField = fieldsByPosition[position];
            if (distance == routeLength)
            {
                var result = new List<List<int>>();
                result.Add(new List<int>() { fieldsByPosition[position].FieldId });
                return result;
            }

            var possibleNextFields =
                fieldsByPosition[position].Neighbors
                    .Where(n => n != null && n.IsAccessible(distance + 1))
                    .Append(currentField)
                    .ToList();

            var safeNextFields =
                possibleNextFields
                    .Where(nf => nf.ExplodeAt == null || !nf.ExplodeAt.Contains(distance + 1))
                    .ToList();

            List<List<int>> routes = 
            //     new List<List<int>>();
            // foreach(var nf in safeNextFields) {
            //     var listFromHere = WaysOutFrom(nf.Position, routeLength, distance + 1, bombsLeft, searchUntilFirst);
            //     if (listFromHere != null) {
            //         if (searchUntilFirst) {
            //             return listFromHere;
            //         }
            //         routes.Add(listFromHere.SelectMany(r => r).ToList());
            //     }
            // }
                
                safeNextFields
                    .Select(nf => WaysOutFrom(nf.Position, routeLength, distance + 1, bombsLeft, searchUntilFirst))
                    .Where(result => result != null)
                    .SelectMany(result => result)
                    .ToList();

            if (routes.Any())
            {
                if (distance != 0)
                {
                    routes.ForEach(r => r.Insert(0, currentField.FieldId));
                }
                return routes;
            }
            else
            {
                return null;
            }
        }

        public List<string> SafeRoutes()
        {

            return null;
        }

        public void PrintExplosionMap()
        {
            var explosionMap = string.Join(" ", fields.Select(f => f.ExplodeAt?.FirstOrDefault().ToString() ?? (f is Wall ? "x" : "-")));
            for (int i = 0; i < 11; i++)
            {
                Console.Error.WriteLine(explosionMap.Substring(i * 26, Math.Min(26, explosionMap.Length - i * 26)));
            }
            Console.Error.WriteLine();
        }

        public void PrintAccessibilityMap(int when)
        {
            var map = string.Join(" ", fields.Select(f => f.IsAccessible(when) ? '.' : 'x'));
            for (int i = 0; i < 11; i++)
            {
                Console.Error.WriteLine(map.Substring(i * 26, Math.Min(26, map.Length - i * 26)));
            }
            Console.Error.WriteLine();
        }
    }
    public class Strategy
    {
        public Map Map { private get; set; }
        public Strategy()
        {
        }

        public string GetNextAction()
        {
            var startingState = GetStartState();
            var ways = startingState.WaysOutFrom(Map.Me.Position, 8, 0, Map.Me.RemainingBombs, false);

            return "MOVE 6 5";
        }

        private Map GetStartState()
        {
            var newBombs = new List<Bomb>();
            // put bomb in others name if it doesn't kill them
            foreach (var other in Map.Others)
            {
                var mapWithNewBomb = Map.Clone();
                var newBomb = new Bomb(other.Id, 8, other.ExplosionRange, other.Position, 0);
                mapWithNewBomb.AddBomb(newBomb);
                mapWithNewBomb.CalculateExplosionTimes();
                var isOk = mapWithNewBomb.WaysOutFrom(other.Position, 8, 0, 0, true) != null;
                if (isOk)
                {
                    newBombs.Add(newBomb);
                }
            }
            var startState = Map.Clone();
            newBombs.ForEach(bomb => startState.AddBomb(bomb));
            startState.CalculateExplosionTimes();
            return startState;
        }
    }
    class Program
    {
        static void Main(string[] args)
        {

            IReader reader;
            if (File.Exists("input.txt"))
            {
                reader = new OfflineReader();
            }
            else
            {
                reader = new OnlineReader();
            }
            var input = new Input(reader);
            input.ReadInit();

            var strategy = new Strategy();

            // game loop
            while (true)
            {
                input.ReadRound();
                strategy.Map = input.GetMap();

                var nextAction = strategy.GetNextAction();

                Console.WriteLine(nextAction);
            }
        }
    }
}