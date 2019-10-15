using System;
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
		enum Direction
		{
			LEFT, RIGHT, UP, DOWN
		};

		static readonly	char[] PlayerChars = new[] { '▲', '►', '▼', '◄' };
		static readonly char[] EnemyChars = new[] { '˄', '˃', '˅', '˂' };
		const char BULLET = '•';

		static char[,] Field;

		static Point PlayerPosition;

		static int FieldLength;

		// Enemy states
		static int NearestDistanceToEnemy;
		static Point NearestEnemyPosition = new Point();
		static Direction DirectionToNearestEnemy;

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
					InitializeProps();

					// Chech lines for target
					FindClosestEnemy();

					webSocket.Send($"{DirectionToNearestEnemy.ToString()}, ACT");
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

		private static void InitializeProps()
		{
			NearestDistanceToEnemy = FieldLength;
		}

		private static bool CheckEnemyInCell(int x, int y)
		{
			return EnemyChars.Contains(Field[y, x]);
		}

		private static void FindClosestEnemy()
		{
			// Find by directions
			CheckLeft();
			CheckRight();
			CheckUp();
			CheckDown();

			// Then find by square ~3x3


		}

		private static void CheckLeft()
		{
			for (int x = PlayerPosition.X; x > 0; x--)
			{
				CheckPointAndUpdateState(Direction.LEFT, new Point(x, PlayerPosition.Y));
			}
		}
		private static void CheckRight()
		{
			for (int x = PlayerPosition.X; x < FieldLength; x++)
			{
				CheckPointAndUpdateState(Direction.RIGHT, new Point(x, PlayerPosition.Y));
			}
		}
		private static void CheckUp()
		{
			for (int y = PlayerPosition.Y; y > 0; y--)
			{
				CheckPointAndUpdateState(Direction.UP, new Point(PlayerPosition.X, y));
			}
		}
		private static void CheckDown()
		{
			for (int y = PlayerPosition.Y; y < FieldLength; y++)
			{
				CheckPointAndUpdateState(Direction.DOWN, new Point(PlayerPosition.X, y));
			}
		}

		private static void CheckPointAndUpdateState(Direction direction, Point position)
		{
			if (CheckEnemyInCell(position.X, position.Y))
			{
				var distanceToEnemy = GetDistanceToEnemy(position);
				if (NearestDistanceToEnemy > distanceToEnemy)
				{
					NearestDistanceToEnemy = distanceToEnemy;
					DirectionToNearestEnemy = direction;
				}
			}
		}

		private static int GetDistanceToEnemy(Point enemyPosition)
		{
			return (int)Math.Round(
				Math.Sqrt(
					Math.Pow((PlayerPosition.X - enemyPosition.X), 2) +
					Math.Pow((PlayerPosition.Y - enemyPosition.Y), 2)));
		}
	}
}
