using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using WebSocket4Net;

namespace Battlecity
{
	class Program
	{
		static readonly char[] PlayerChars = new[] { '▲', '►', '▼', '◄' };
		static readonly char[] EnemyChars = new[] { '˄', '˃', '˅', '˂' };
		static readonly Dictionary<char, int> WallsDict = new Dictionary<char, int>
		{
			{ '╬', 3 },

			{ '╩', 2 },
			{ '╦', 2 },
			{ '╠', 2 },
			{ '╣', 2 },

			{ '╨', 1 },
			{ '╥', 1 },
			{ '╞', 1 },
			{ '╡', 1 },
			{ '│', 1 },
			{ '─', 1 },
			{ '┌', 1 },
			{ '┐', 1 },
			{ '└', 1 },
			{ '┘', 1 }
		};
		const char
			GROUND = ' ',
			BULLET = '•',
			WALL = '☼';

		static readonly Dictionary<Direction, Point> DirectionToPointDict = new Dictionary<Direction, Point>
		{
			{ Direction.LEFT, new Point(-1, 0) },
			{ Direction.UP, new Point(0, -1) },
			{ Direction.RIGHT, new Point(1, 0) },
			{ Direction.DOWN, new Point(0, 1) },
		};

		static char[,] Field;
		static int FieldLength;
		static Point PlayerPosition;

		static Direction BestDirection;

		static void Main(string[] args)
		{
			var regex = new Regex(@"=(.*)$");

			using (var webSocket = new WebSocket("ws://codenjoy.com:80/codenjoy-contest/ws?user=hsxsnhnir64osk1ku5ki&code=1208759298589485338"))
			{
				webSocket.MessageReceived += (sender, e) =>
				{
					var oneLineField = regex.Matches(e.Message)[0].Value.Substring(1);
					FieldLength = (int)Math.Sqrt(oneLineField.Length);

					InitializeField(oneLineField);

					// Chech lines for target
					FindClosestEnemy();

					webSocket.Send($"{BestDirection.ToString()}, ACT");
				};

				webSocket.Open();

				Console.ReadKey();
			}
		}

		private static void InitializeField(string oneLineField)
		{
			Field = new char[FieldLength, FieldLength];

			var index = 0;
			for (int y = 0; y < FieldLength; y++)
			{
				for (int x = 0; x < FieldLength; x++)
				{
					var currentObjectChar = oneLineField[index++];
					Field[y, x] = currentObjectChar;

					// Init objects
					if (PlayerChars.Contains(currentObjectChar))
					{
						PlayerPosition = new Point(x, y);
					}
				}
			}
		}

		private static bool CheckEnemyInCell(Point point)
		{
			return EnemyChars.Contains(Field[point.Y, point.X]);
		}

		private static void FindClosestEnemy()
		{
			// Find by directions
			var pathes = new List<Path>
			{
				GetLeftPath(),
				GetRightPath(),
				GetUpPath(),
				GetDownPath(),
			};

			var listOfPerm = new List<IEnumerable<IEnumerable<Direction>>>
			{
				GetPermutationsWithRept(new List<Direction> { Direction.LEFT, Direction.UP, Direction.RIGHT, Direction.DOWN }, 1),
				GetPermutationsWithRept(new List<Direction> { Direction.LEFT, Direction.UP, Direction.RIGHT, Direction.DOWN }, 2),
				GetPermutationsWithRept(new List<Direction> { Direction.LEFT, Direction.UP, Direction.RIGHT, Direction.DOWN }, 3),
				GetPermutationsWithRept(new List<Direction> { Direction.LEFT, Direction.UP, Direction.RIGHT, Direction.DOWN }, 4),
				GetPermutationsWithRept(new List<Direction> { Direction.LEFT, Direction.UP, Direction.RIGHT, Direction.DOWN }, 5),
				GetPermutationsWithRept(new List<Direction> { Direction.LEFT, Direction.UP, Direction.RIGHT, Direction.DOWN }, 6),
				GetPermutationsWithRept(new List<Direction> { Direction.LEFT, Direction.UP, Direction.RIGHT, Direction.DOWN }, 7),
				GetPermutationsWithRept(new List<Direction> { Direction.LEFT, Direction.UP, Direction.RIGHT, Direction.DOWN }, 8),
			};
			foreach (var item in listOfPerm.SelectMany(x => x))
			{
				pathes.Add(BuildPath(item));
			}

			var betterPath = CheckPathes(pathes.Where(x => x.Count > 0));

			BestDirection = betterPath.First().Direction;
		}

		private static Path GetLeftPath()
		{
			var path = new Path();
			for (int x = PlayerPosition.X - 1; x > 0; x--)
			{
				path.Add(new PathPart(Direction.LEFT, new Point(x, PlayerPosition.Y)));
			}
			return path;
		}
		private static Path GetRightPath()
		{
			var path = new Path();
			for (int x = PlayerPosition.X + 1; x < FieldLength; x++)
			{
				path.Add(new PathPart(Direction.RIGHT, new Point(x, PlayerPosition.Y)));
			}
			return path;
		}
		private static Path GetUpPath()
		{
			var path = new Path();
			for (int y = PlayerPosition.Y - 1; y > 0; y--)
			{
				path.Add(new PathPart(Direction.UP, new Point(PlayerPosition.X, y)));
			}
			return path;
		}
		private static Path GetDownPath()
		{
			var path = new Path();
			for (int y = PlayerPosition.Y + 1; y < FieldLength; y++)
			{
				path.Add(new PathPart(Direction.DOWN, new Point(PlayerPosition.X, y)));
			}
			return path;
		}

		private static Path BuildPath(IEnumerable<Direction> directions)
		{
			var path = new Path();

			foreach (var dir in directions)
			{
				var additionalPoint = DirectionToPointDict[dir];
				path.Add(new PathPart
				{
					Direction = dir,
					Point = new Point(PlayerPosition.X + additionalPoint.X, PlayerPosition.Y + additionalPoint.Y)
				});
			}

			return path;
		}

		private static Path CheckPathes(IEnumerable<Path> paths)
		{
			Path mostProfitablePath = paths.First();
			var lowerPathValue = int.MaxValue;

			foreach (var path in paths)
			{
				var pathValue = CheckPath(path);
				if (lowerPathValue > pathValue)
				{
					lowerPathValue = pathValue;
					mostProfitablePath = path;
				}
			}

			return mostProfitablePath;
		}

		private static int CheckPath(Path path)
		{
			// TO DO: Move this to CheckPathValue
			var points = path.GetPoints();
			foreach (var point in points)
			{
				if (CheckEnemyInCell(point))
				{
					return CheckPathValue(points);
				}
				if (CheckWallInCell(point))
				{
					return int.MaxValue;
				}
			}

			return int.MaxValue;
		}

		private static bool CheckWallInCell(Point point)
		{
			return Field[point.Y, point.X] == WALL;
		}

		private static int GetDistanceToEnemy(Point enemyPosition)
		{
			return (int)Math.Round(
				Math.Sqrt(
					Math.Pow((PlayerPosition.X - enemyPosition.X), 2) +
					Math.Pow((PlayerPosition.Y - enemyPosition.Y), 2)));
		}

		private static int CheckPathValue(Point[] points)
		{
			return CheckPathValue(Enumerable.Range(0, points.Length).Select(x => Field[points[x].Y, points[x].X]).ToArray());
		}

		private static int CheckPathValue(char[] chars)
		{
			int value = 0;

			for (int i = 0; i < chars.Length; i++)
			{
				if (i != chars.Length - 1 && chars[i] == WALL)
				{
					return int.MaxValue;
				}

				if (WallsDict.ContainsKey(chars[i]))
				{
					value += WallsDict[chars[i]] * 3;
				}

				if (chars[i] == GROUND)
				{
					value++;
				}
			}

			return value;
		}

		static IEnumerable<IEnumerable<T>> GetPermutations<T>(IEnumerable<T> list, int length)
		{
			if (length == 1) return list.Select(t => new T[] { t });
			return GetPermutations(list, length - 1)
				.SelectMany(t => list.Where(o => !t.Contains(o)),
					(t1, t2) => t1.Concat(new T[] { t2 }));
		}

		static IEnumerable<IEnumerable<T>> GetPermutationsWithRept<T>(IEnumerable<T> list, int length)
		{
			if (length == 1) return list.Select(t => new T[] { t });
			return GetPermutationsWithRept(list, length - 1)
				.SelectMany(t => list,
					(t1, t2) => t1.Concat(new T[] { t2 }));
		}
	}

	enum Direction
	{
		LEFT, UP, RIGHT, DOWN
	};

	class Path : List<PathPart>
	{
		public Point[] GetPoints()
		{
			return this.Select(x => x.Point).ToArray();
		}
	}

	struct PathPart
	{
		public Direction Direction { get; set; }
		public Point Point { get; set; }

		public PathPart(Direction direction, Point point)
		{
			Direction = direction;
			Point = point;
		}
	}
}
