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
				GetDownPath()
			};
			var betterPath = CheckPathes(pathes);

			BestDirection = betterPath.First().Direction;

			// Then find by square ~3x3


		}

		private static Path GetLeftPath()
		{
			var path = new Path();
			for (int x = PlayerPosition.X; x > 1; x--)
			{
				path.Add(new PathPart(Direction.LEFT, new Point(x, PlayerPosition.Y)));
			}
			return path;
		}
		private static Path GetRightPath()
		{
			var path = new Path();
			for (int x = PlayerPosition.X; x < FieldLength - 1; x++)
			{
				path.Add(new PathPart(Direction.RIGHT, new Point(x, PlayerPosition.Y)));
			}
			return path;
		}
		private static Path GetUpPath()
		{
			var path = new Path();
			for (int y = PlayerPosition.Y; y > 1; y--)
			{
				path.Add(new PathPart(Direction.UP, new Point(PlayerPosition.X, y)));
			}
			return path;
		}
		private static Path GetDownPath()
		{
			var path = new Path();
			for (int y = PlayerPosition.Y; y < FieldLength - 1; y++)
			{
				path.Add(new PathPart(Direction.DOWN, new Point(PlayerPosition.X, y)));
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
			}

			return int.MaxValue;
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
			return CheckPathValue(Enumerable.Range(1, points.Length - 1).Select(x => Field[points[x].Y, points[x].X]).ToArray());
		}

		private static int CheckPathValue(char[] chars)
		{
			int value = 0;

			for (int i = 0; i < chars.Length; i++)
			{
				if (chars[i] == WALL)
				{
					return int.MaxValue;
				}

				if (WallsDict.ContainsKey(chars[i]))
				{
					value += WallsDict[chars[i]];
				}
			}

			return value;
		}
	}

	enum Direction
	{
		LEFT, RIGHT, UP, DOWN
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
