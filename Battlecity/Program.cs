using System;
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
		static readonly Dictionary<char, Direction> PlayerDirectionDict = new Dictionary<char, Direction>
		{
			{ '▲', Direction.UP },
			{ '►', Direction.RIGHT },
			{ '▼', Direction.DOWN },
			{ '◄', Direction.LEFT },
		};

		static readonly char[] EnemyChars = new[] { '˄', '˃', '˅', '˂' };
		static readonly Dictionary<char, Direction> EnemyDirectionDict = new Dictionary<char, Direction>
		{
			{ '˄', Direction.UP },
			{ '˃', Direction.RIGHT },
			{ '˅', Direction.DOWN },
			{ '˂', Direction.LEFT },
		};

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
		static Direction PlayerDirection;

		static Direction BestDirection;

		static bool
			ShouldMove,
			ShouldAct;

		static string MoveDirection { get; set; }
		static string Action { get; set; } = "ACTION";
		static string Comma => !string.IsNullOrEmpty(MoveDirection) && !string.IsNullOrEmpty(Action) ? ", " : "";
		static bool MoveFirst { get; set; } = true;

		static int _shotCd;
		static int ShotCD
		{
			get => _shotCd;
			set
			{
				_shotCd = value < 0 ? 0 : value;
			}
		}
		static bool CanShoot { get; set; }

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

					//Chech lines for target
					//FindClosestEnemy();

					//if (ShouldAct)
					//{
					//	webSocket.Send($"{BestDirection.ToString()}, ACT");
					//}
					//else
					//{
					//	webSocket.Send($"{BestDirection.ToString()}");
					//}


					Log("-----");

					ShotCD--;

					MoveToPossibleSafePlace();


					Log($"Move {MoveDirection.ToString()}");
					if (MoveFirst)
					{
						webSocket.Send($"{MoveDirection}{Comma}{Action}");
					}
					else
					{
						webSocket.Send($"{Action}{Comma}{MoveDirection}");
					}

					// Reset.
					Action = "";
					MoveFirst = true;

					Log("-----");
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
						PlayerDirection = GetPlayerDirection();
					}
				}
			}
		}

		private static Direction GetPlayerDirection()
		{
			return PlayerDirectionDict[GetFieldChar(PlayerPosition)];
		}

		private static bool CheckEnemyInCell(Point point)
		{
			if (!CheckPointCorrectness(point))
			{
				return false;
			}

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

		private static void Log(string message)
		{
			Console.WriteLine(message);
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
					return GetPathValue(points.Take(i + 1).ToArray());
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
			if (!CheckPointCorrectness(point))
			{
				return false;
			}

			return Field[point.Y, point.X] == BULLET;
		}

		private static bool CheckPointCorrectness(Point point)
		{
			if (point.X < 0 || point.Y < 0 ||
				point.X > FieldLength - 1 || point.Y > FieldLength - 1)
			{
				return false;
			}

			return true;
		}

		private static bool CheckWallInCell(Point point)
		{
			if (!CheckPointCorrectness(point))
			{
				return false;
			}

			return Field[point.Y, point.X] == WALL;
		}

		private static int GetDistanceToEnemy(Point enemyPosition)
		{
			return (int)Math.Round(
				Math.Sqrt(
					Math.Pow((PlayerPosition.X - enemyPosition.X), 2) +
					Math.Pow((PlayerPosition.Y - enemyPosition.Y), 2)));
		}

		private static int GetPathValue(Point[] points)
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

		static readonly Random Random = new Random();
		static bool PlayerChangedDirection;
		static int _movesToChangeDirection;
		static int MovesToChangeDirection
		{
			get => _movesToChangeDirection;
			set
			{
				if (_movesToChangeDirection < 0)
				{
					_movesToChangeDirection = 3;
					PlayerChangedDirection = true;
				}
				else
				{
					_movesToChangeDirection = value;
				}
			}
		}

		private static void MoveToPossibleSafePlace()
		{
			// If there is a threat to the Player
			if (CheckIfPlayerShouldMove())
			{
				if (IsHorizontalDirectionMoveSafe(PlayerPosition.Add(DirectionToPointDict[Direction.LEFT])) && !CheckBulletInCell(PlayerPosition.Add(new Point(-3, 0))))
				{
					Move(Direction.LEFT);
					Log("Evaded to the LEFT");
				}
				else if (IsHorizontalDirectionMoveSafe(PlayerPosition.Add(DirectionToPointDict[Direction.RIGHT])) && !CheckBulletInCell(PlayerPosition.Add(new Point(3, 0))))
				{
					Move(Direction.RIGHT);
					Log("Evaded to the RIGHT");
				}
				else if (IsVerticallDirectionMoveSafe(PlayerPosition.Add(DirectionToPointDict[Direction.UP])) && !CheckBulletInCell(PlayerPosition.Add(new Point(0, -3))))
				{
					Move(Direction.UP);
					Log("Evaded to the UP");
				}
				else if (IsVerticallDirectionMoveSafe(PlayerPosition.Add(DirectionToPointDict[Direction.DOWN])) && !CheckBulletInCell(PlayerPosition.Add(new Point(0, 3))))
				{
					Move(Direction.DOWN);
					Log("Evaded to the DOWN");
				}
				else
				{
					// TO DO: If there is no way to escape - try to turn to the bullet and make a shot.

					Stay();
				}

				PlayerChangedDirection = true;

				return;
			}
			else
			{
				//var newPlayerDirection = GetRandomSafeDirection();
				////newPlayerDirection = GetDirectionForPossibleKill();

				//Move(newPlayerDirection);
			}


			// If player has bullet and there is a potential Kill - shoot.
			// If not - try to move to most safe place (that are without enemies faced to the Player).
			// If not - try to move to an approximately safe direction.


			if (CheckPossibleShootThenMoveOutcomes())
			{
				MoveFirst = false;
				TryToShoot();

				var newPlayerDirection = GetRandomSafeDirection(PlayerDirection);
				Move(newPlayerDirection);

				PlayerChangedDirection = true;
			}
			else
			{
				if (PlayerChangedDirection)
				{
					// TO DO: Replace random with optimal but need to fix optimal
					//var newPlayerDirection = GetMostOptimalDirection();
					var newPlayerDirection = GetRandomSafeDirection();
					Move(newPlayerDirection);
					PlayerChangedDirection = false;
				}
				else
				{
					Move(PlayerDirection);
					MovesToChangeDirection--;
				}
			}

			TryToShoot();
		}

		private static Direction GetMostOptimalDirection()
		{
			// TO DO: Replace Path methods with something more suitable
			var pathes = new List<Path>
			{
				GetLeftPath(),
				GetRightPath(),
				GetUpPath(),
				GetDownPath(),
			};

			var betterPath = GetMoreEfficientPath(pathes);
			//var betterPath = CheckPathes(pathes.Where(x => x.Count > 0));

			return betterPath.Count() == 0
				? GetRandomDirection()
				: betterPath.FirstOrDefault().Direction;
		}

		private static Path GetMoreEfficientPath(List<Path> pathes)
		{
			var mostProfitablePath = pathes.First();
			var lowerPathValue = int.MaxValue;

			foreach (var path in pathes)
			{
				var pathValue = GetPathEfficiency(path);
				if (lowerPathValue > pathValue)
				{
					lowerPathValue = pathValue;
					mostProfitablePath = path;
				}
			}

			return mostProfitablePath;
		}

		private static int GetPathEfficiency(Path path)
		{
			int value = 0;
			var points = path.GetPoints();

			for (int i = 0; i < points.Length; i++)
			{
				if (CheckWallInCell(points[i]))
				{
					return int.MaxValue;
				}
				if (CheckConstructionInCell(points[i]))
				{
					value += ConstructionDict[GetFieldChar(points[i])] * 4;
				}
			}

			return value;

			//ShouldAct = true;
			//return 100;




			//var points = path.GetPoints();
			//var chars = Enumerable.Range(0, points.Length).Select(x => Field[points[x].Y, points[x].X]).ToArray();

			//int value = 0;

			//for (int i = 0; i < chars.Length; i++)
			//{
			//	if (i != chars.Length - 1 && chars[i] == WALL)
			//	{
			//		return int.MaxValue;
			//	}
			//	else if (ConstructionDict.ContainsKey(chars[i]))
			//	{
			//		value += ConstructionDict[chars[i]] * 5;
			//	}
			//	else if (chars[i] == GROUND)
			//	{
			//		value++;
			//	}
			//	else
			//	{
			//		value++;
			//	}
			//}

			//return value;
		}

		private static Direction GetRandomDirection()
		{
			return Directions[Random.Next(Directions.Length)];
		}

		/// <summary>
		/// If Player directed towards a potential Enemy.
		/// </summary>
		/// <returns></returns>
		private static bool CheckPossibleShootThenMoveOutcomes()
		{
			var isAble = false;
			var direction = Direction.NONE;

			switch (PlayerDirection)
			{
				case Direction.LEFT:
					isAble = CheckStarQuarterLeft();
					direction = Direction.LEFT;
					break;
				case Direction.UP:
					isAble = CheckStarQuarterUp();
					direction = Direction.UP;
					break;
				case Direction.RIGHT:
					isAble = CheckStarQuarterRight();
					direction = Direction.RIGHT;
					break;
				case Direction.DOWN:
					isAble = CheckStarQuarterDown();
					direction = Direction.DOWN;
					break;
				default:
					break;
			}

			if (isAble)
			{
				Log($"Potential Enemy to the {direction}");
			}

			return isAble;
		}

		private static bool CheckStarQuarterRight()
		{
			return
				!CheckBarrierInCell(PlayerPosition.Add(new Point(2, 0))) &&
				(
					CheckEnemyInCell(PlayerPosition.Add(new Point(3, 0))) ||
					CheckEnemyInCell(PlayerPosition.Add(new Point(2, -1))) ||
					CheckEnemyInCell(PlayerPosition.Add(new Point(2, 1)))
				) ||
				!CheckBarrierInCell(PlayerPosition.Add(new Point(1, 0))) &&
				(
					CheckEnemyInCell(PlayerPosition.Add(new Point(1, 1))) ||
					CheckEnemyInCell(PlayerPosition.Add(new Point(1, -1)))
				);
		}

		private static bool CheckStarQuarterLeft()
		{
			return
				!CheckBarrierInCell(PlayerPosition.Add(new Point(-2, 0))) &&
				(
					CheckEnemyInCell(PlayerPosition.Add(new Point(-3, 0))) ||
					CheckEnemyInCell(PlayerPosition.Add(new Point(-2, -1))) ||
					CheckEnemyInCell(PlayerPosition.Add(new Point(-2, 1)))
				) ||
				!CheckBarrierInCell(PlayerPosition.Add(new Point(-1, 0))) &&
				(
					CheckEnemyInCell(PlayerPosition.Add(new Point(-1, 1))) ||
					CheckEnemyInCell(PlayerPosition.Add(new Point(-1, -1)))
				);
		}

		private static bool CheckStarQuarterUp()
		{
			/* E - Enemy; P - Player; _ - Ground
			 # E #
			 E _ E
			 E _ E
			 _ P _
			*/
			return
				!CheckBarrierInCell(PlayerPosition.Add(new Point(0, -2))) &&
				(
					CheckEnemyInCell(PlayerPosition.Add(new Point(0, -3))) ||
					CheckEnemyInCell(PlayerPosition.Add(new Point(1, -2))) ||
					CheckEnemyInCell(PlayerPosition.Add(new Point(-1, -2)))
				) ||
				!CheckBarrierInCell(PlayerPosition.Add(new Point(0, -1))) &&
				(
					CheckEnemyInCell(PlayerPosition.Add(new Point(1, -1))) ||
					CheckEnemyInCell(PlayerPosition.Add(new Point(-1, -1)))
				);
		}

		private static bool CheckStarQuarterDown()
		{
			return
				!CheckBarrierInCell(PlayerPosition.Add(new Point(0, 2))) &&
				(
					CheckEnemyInCell(PlayerPosition.Add(new Point(0, 3))) ||
					CheckEnemyInCell(PlayerPosition.Add(new Point(1, 2))) ||
					CheckEnemyInCell(PlayerPosition.Add(new Point(-1, 2)))
				) ||
				!CheckBarrierInCell(PlayerPosition.Add(new Point(0, 1))) &&
				(
					CheckEnemyInCell(PlayerPosition.Add(new Point(1, 1))) ||
					CheckEnemyInCell(PlayerPosition.Add(new Point(-1, 1)))
				);
		}

		private static Direction DirectionGetEnemyDirection(Point enemyPos)
		{
			return EnemyDirectionDict[GetFieldChar(enemyPos)];
		}

		private static Direction GetRandomSafeDirection(params Direction[] except)
		{
			var list = new List<Direction>();

			foreach (var direction in DirectionToPointDict.Keys.Where(x => except != null && !except.Contains(x)))
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

			if (list.Contains(PlayerDirection))
			{
				return PlayerDirection;
			}

			if (list.Count == 0)
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
			if (direction == Direction.NONE)
			{
				Stay();
				return;
			}

			MoveDirection = direction.ToString();
		}

		private static void TryToShoot()
		{
			Action = "ACTION";
			ShotCD = 3;
		}

		private static bool IsHorizontalDirectionMoveSafe(Point point)
		{
			return
				!CheckUpAndBottomThreat(point) &&
				!CheckWallInCell(point) &&
				!CheckConstructionInCell(point);
		}

		private static bool IsVerticallDirectionMoveSafe(Point point)
		{
			return
				!CheckLeftAndRightThreat(point) &&
				!CheckWallInCell(point) &&
				!CheckConstructionInCell(point);
		}

		private static bool CheckConstructionInCell(Point point)
		{
			if (!CheckPointCorrectness(point))
			{
				return false;
			}

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

		//private static Direction CheckThreatDirection()
		//{
		//	

		//	return Direction.NONE;
		//}

		private static bool IsBulletOnCrossOnPlayer()
		{
			return
				CheckUpAndBottomThreat(PlayerPosition) ||
				CheckLeftAndRightThreat(PlayerPosition);
		}

		private static bool CheckUpAndBottomThreat(Point point)
		{
			return
				!CheckBarrierInCell(new Point(point.X, point.Y + 1)) &&
				(CheckThreatInCell(new Point(point.X, point.Y + 1)) ||
				CheckThreatInCell(new Point(point.X, point.Y + 2))) ||

				!CheckBarrierInCell(new Point(point.X, point.Y - 1)) &&
				(CheckThreatInCell(new Point(point.X, point.Y - 1)) ||
				CheckThreatInCell(new Point(point.X, point.Y - 1)));

		}

		private static bool CheckLeftAndRightThreat(Point point)
		{
			return
				!CheckBarrierInCell(new Point(point.X + 1, point.Y)) &&
				(CheckThreatInCell(new Point(point.X + 1, point.Y)) ||
				CheckThreatInCell(new Point(point.X + 2, point.Y))) ||

				!CheckBarrierInCell(new Point(point.X - 1, point.Y)) &&
				(CheckThreatInCell(new Point(point.X - 1, point.Y)) ||
				CheckThreatInCell(new Point(point.X - 2, point.Y)));
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
