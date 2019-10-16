﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using WebSocket4Net;

namespace Battlecity
{
	class Program
	{
		static readonly char[] PlayerChars = new[] { '▲', '►', '▼', '◄' };
		static readonly char[] EnemyChars = new[] { '˄', '˃', '˅', '˂' };
		static readonly Dictionary<char, int> ConstructionDict = new Dictionary<char, int>
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


		static readonly Direction[] Directions = new Direction[]
		{
			Direction.LEFT,
			Direction.UP,
			Direction.RIGHT,
			Direction.DOWN,
		};
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

		static bool
			ShouldMove,
			ShouldAct;

		static string MoveDirection { get; set; }
		static string Action { get; set; } = "ACTION";
		static string Comma => !string.IsNullOrEmpty(MoveDirection) && !string.IsNullOrEmpty(Action) ? ", " : "";
		static bool MoveFirst { get; set; } = true;

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
					//FindClosestEnemy();

					//if (ShouldAct)
					//{
					//	webSocket.Send($"{BestDirection.ToString()}, ACT");
					//}
					//else
					//{
					//	webSocket.Send($"{BestDirection.ToString()}");
					//}

					MoveToPossibleSafePlace();

					if (MoveFirst)
					{
						webSocket.Send($"{MoveDirection}{Comma}{Action}");
					}
					else
					{
						webSocket.Send($"{Action}{Comma}{MoveDirection}");
					}
				};

				webSocket.Open();

				Console.ReadLine();
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

			//pathes.AddRange(new[]
			//{
			//	BuildPath(new[] { Direction.UP, Direction.LEFT }),
			//	BuildPath(new[] { Direction.UP, Direction.RIGHT }),
			//	BuildPath(new[] { Direction.RIGHT, Direction.UP }),
			//	BuildPath(new[] { Direction.RIGHT, Direction.DOWN }),
			//	BuildPath(new[] { Direction.DOWN, Direction.LEFT }),
			//	BuildPath(new[] { Direction.DOWN, Direction.RIGHT }),
			//	BuildPath(new[] { Direction.LEFT, Direction.UP }),
			//	BuildPath(new[] { Direction.LEFT, Direction.DOWN }),
			//});

			var betterPath = CheckPathes(pathes.Where(x => x.Count > 0));

			BestDirection = betterPath.First().Direction;

			LogBestPath(betterPath);
		}

		private static void LogBestPath(Path betterPath)
		{
			StringBuilder sb = new StringBuilder($"{betterPath.Value}: ");
			foreach (var item in betterPath)
			{
				sb.Append($"{item.Direction} ");
			}
			Console.WriteLine(sb.ToString());
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

			var pos = PlayerPosition;

			foreach (var dir in directions)
			{
				var additionalPoint = DirectionToPointDict[dir];

				pos.X += additionalPoint.X;
				pos.Y += additionalPoint.Y;

				path.Add(new PathPart
				{
					Direction = dir,
					Point = new Point(pos.X, pos.Y)
				});
			}

			return path;
		}

		private static Path CheckPathes(IEnumerable<Path> paths)
		{
			var mostProfitablePath = paths.First();
			var lowerPathValue = int.MaxValue;

			foreach (var path in paths)
			{
				path.Value = GetPathValue(path);
				if (lowerPathValue > path.Value)
				{
					lowerPathValue = path.Value;
					mostProfitablePath = path;
				}
			}

			return mostProfitablePath;
		}

		private static int GetPathValue(Path path)
		{
			// TO DO: Move this to CheckPathValue
			var points = path.GetPoints();

			for (int i = 0; i < points.Length; i++)
			{
				if (CheckEnemyInCell(points[i]))
				{
					ShouldAct = true;
					return CheckPathValue(points.Take(i + 1).ToArray());
				}
				if (CheckWallInCell(points[i]))
				{
					return int.MaxValue;
				}
				if ((i == 0 || i == 1 || i == 2) && CheckBulletInCell(points[i]))
				{
					return int.MaxValue; ;
				}
				if (!CheckPathPartSafeness(path[i]))
				{
					return int.MaxValue;
				}
			}

			ShouldAct = true;
			return 100;
		}

		private static bool CheckBulletInCell(Point point)
		{
			if (point.X < 0 || point.Y < 0 || point.X > FieldLength - 1 || point.Y > FieldLength - 1)
			{
				return false;
			}

			return Field[point.Y, point.X] == BULLET;
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
				else if (ConstructionDict.ContainsKey(chars[i]))
				{
					value += ConstructionDict[chars[i]] * 5;
				}
				else if (chars[i] == GROUND)
				{
					value++;
				}
				else
				{
					value++;
				}
			}

			return value;
		}

		private static bool CheckPathPartSafeness(PathPart pathPart)
		{
			if (pathPart.Direction == Direction.DOWN || pathPart.Direction == Direction.UP)
			{
				if (CheckBulletInCell(new Point(pathPart.Point.X + 1, pathPart.Point.Y)) ||
					CheckBulletInCell(new Point(pathPart.Point.X + 2, pathPart.Point.Y)) ||
					CheckBulletInCell(new Point(pathPart.Point.X - 1, pathPart.Point.Y)) ||
					CheckBulletInCell(new Point(pathPart.Point.X - 2, pathPart.Point.Y)))
				{
					return false;
				}
			}
			else
			{
				if (CheckBulletInCell(new Point(pathPart.Point.X, pathPart.Point.Y + 1)) ||
					CheckBulletInCell(new Point(pathPart.Point.X, pathPart.Point.Y + 2)) ||
					CheckBulletInCell(new Point(pathPart.Point.X, pathPart.Point.Y - 1)) ||
					CheckBulletInCell(new Point(pathPart.Point.X, pathPart.Point.Y - 2)))
				{
					return false;
				}
			}

			return true;
		}


		#region New

		static Direction PlayerCurrentDirection;
		static bool PlayerChangedDirection;
		static readonly Random Random;

		private static void MoveToPossibleSafePlace()
		{
			if (CheckIfPlayerShouldMove())
			{
				if (IsHorizontalDirectionMoveSafe(PlayerPosition.Add(DirectionToPointDict[Direction.LEFT])) && CheckBulletInCell(PlayerPosition.Add(new Point(-3, 0))))
				{
					Move(Direction.LEFT);
				}
				else if (IsHorizontalDirectionMoveSafe(PlayerPosition.Add(DirectionToPointDict[Direction.RIGHT])) && CheckBulletInCell(PlayerPosition.Add(new Point(3, 0))))
				{
					Move(Direction.RIGHT);
				}
				else if (IsVerticallDirectionMoveSafe(PlayerPosition.Add(DirectionToPointDict[Direction.UP])) && CheckBulletInCell(PlayerPosition.Add(new Point(0, -3))))
				{
					Move(Direction.UP);
				}
				else if (IsVerticallDirectionMoveSafe(PlayerPosition.Add(DirectionToPointDict[Direction.DOWN])) && CheckBulletInCell(PlayerPosition.Add(new Point(0, 3))))
				{
					Move(Direction.DOWN);
				}
				else
				{
					Stay();
				}

				PlayerChangedDirection = true;
			}
			else
			{
				if (PlayerChangedDirection || CheckBarrierInCell(PlayerPosition.Add(DirectionToPointDict[PlayerCurrentDirection])))
				{
					PlayerChangedDirection = false;
					PlayerCurrentDirection = GetRandomSafeDirection();
				}

				if (PlayerCurrentDirection == Direction.NONE)
				{
					Stay();
				}

				Move(PlayerCurrentDirection);
			}
		}

		private static Direction GetRandomSafeDirection()
		{
			var list = new List<Direction>();

			foreach (var direction in DirectionToPointDict.Keys)
			{
				if (direction == Direction.LEFT || direction == Direction.RIGHT)
				{
					if (IsHorizontalDirectionMoveSafe(PlayerPosition.Add(DirectionToPointDict[direction])))
					{
						list.Add(direction);
					}
				}
				else
				{
					if (IsVerticallDirectionMoveSafe(PlayerPosition.Add(DirectionToPointDict[direction])))
					{
						list.Add(direction);
					}
				}
			}

			if(list.Count == 0)
			{
				return Direction.NONE;
			}

			return list[Random.Next(list.Count)];
		}

		private static void Stay()
		{
			MoveDirection = "";
		}

		private static void Move(Direction direction)
		{
			MoveDirection = direction.ToString();
		}

		private static bool IsHorizontalDirectionMoveSafe(Point point)
		{
			return
				CheckVerticalThreat(point) &&
				!CheckWallInCell(point) &&
				!CheckConstructionInCell(point);
		}

		private static bool IsVerticallDirectionMoveSafe(Point point)
		{
			return
				CheckHorizontalThreat(point) &&
				!CheckWallInCell(point) &&
				!CheckConstructionInCell(point);
		}

		private static bool CheckConstructionInCell(Point point)
		{
			return ConstructionDict.Keys.Contains(GetFieldChar(point));
		}

		private static char GetFieldChar(Point point)
		{
			return Field[point.Y, point.X];
		}

		private static bool CheckIfPlayerShouldMove()
		{
			if (IsBulletOnCrossOnPlayer())
			{
				return true;
			}

			return false;
		}

		private static bool IsBulletOnCrossOnPlayer()
		{
			return
				CheckHorizontalThreat(PlayerPosition) ||
				CheckVerticalThreat(PlayerPosition);
		}

		private static bool CheckHorizontalThreat(Point point)
		{
			return
				!CheckBarrierInCell(new Point(point.Y, point.X + 1)) &&
				(CheckThreatInCell(new Point(point.Y, point.X + 1)) ||
				CheckThreatInCell(new Point(point.Y, point.X + 2))) ||

				!CheckBarrierInCell(new Point(point.Y, point.X - 1)) &&
				(CheckThreatInCell(new Point(point.Y, point.X - 1)) ||
				CheckThreatInCell(new Point(point.Y, point.X - 2)));
		}

		private static bool CheckVerticalThreat(Point point)
		{
			return
				!CheckBarrierInCell(new Point(point.Y + 1, point.X)) &&
				(CheckThreatInCell(new Point(point.Y + 1, point.X)) ||
				CheckThreatInCell(new Point(point.Y + 2, point.X))) ||

				!CheckBarrierInCell(new Point(point.Y - 1, point.X)) &&
				(CheckThreatInCell(new Point(point.Y - 1, point.X)) ||
				CheckThreatInCell(new Point(point.Y - 2, point.X)));
		}

		private static bool CheckThreatInCell(Point point)
		{
			return CheckBulletInCell(point);
		}

		private static bool CheckBarrierInCell(Point point)
		{
			return CheckWallInCell(point) || CheckConstructionInCell(point);
		}


		#endregion
	}

	enum Direction
	{
		LEFT, UP, RIGHT, DOWN, NONE
	};

	class Path : List<PathPart>
	{
		public int Value { get; set; }

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

	static class PointExtension
	{
		public static Point Add(this Point point, Point newPoint)
		{
			return new Point(point.X + newPoint.X, point.Y + newPoint.Y);
		}
	}
}
