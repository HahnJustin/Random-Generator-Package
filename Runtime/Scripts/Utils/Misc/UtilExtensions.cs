using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Utils
{
    public static class UtilExtensions
    {

        public static Vector2Int GetPointInDirection(this Vector2Int point, Direction direction, int movement = 1)
        {
            int searchX = point.x;
            int searchY = point.y;
            switch (direction)
            {
                case Direction.Up:
                    searchY += movement;
                    break;
                case Direction.Up_Right:
                    searchX += movement;
                    searchY += movement;
                    break;
                case Direction.Right:
                    searchX += movement;
                    break;
                case Direction.Down_Right:
                    searchX += movement;
                    searchY -= movement;
                    break;
                case Direction.Down:
                    searchY -= movement;
                    break;
                case Direction.Down_Left:
                    searchX -= movement;
                    searchY -= movement;
                    break;
                case Direction.Left:
                    searchX -= movement;
                    break;
                case Direction.Up_Left:
                    searchX -= movement;
                    searchY += movement;
                    break;
            }
            return new Vector2Int(searchX, searchY);
        }

        private static readonly List<Direction> _cardinalDirections = new()
        {
            Direction.Right,
            Direction.Down,
            Direction.Left,
            Direction.Up
        };

            private static readonly List<Direction> _eightDirections = new()
        {
            Direction.Right,
            Direction.Down_Right,
            Direction.Down,
            Direction.Down_Left,
            Direction.Left,
            Direction.Up_Left,
            Direction.Up,
            Direction.Up_Right
        };

        /// <summary>
        /// Returns all four cardinal directions in clockwise order starting from Right.
        /// </summary>
        public static List<Direction> GetCardinalDirections(this Direction _) => _cardinalDirections;

        /// <summary>
        /// Returns all eight directions in clockwise order starting from Right.
        /// </summary>
        public static List<Direction> GetEightDirections(this Direction _) => _eightDirections;

        /// <summary>
        /// Returns the opposite direction.
        /// </summary>
        public static Direction GetOpposite(this Direction dir)
        {
            return dir switch
            {
                Direction.Up => Direction.Down,
                Direction.Up_Right => Direction.Down_Left,
                Direction.Right => Direction.Left,
                Direction.Down_Right => Direction.Up_Left,
                Direction.Down => Direction.Up,
                Direction.Down_Left => Direction.Up_Right,
                Direction.Left => Direction.Right,
                Direction.Up_Left => Direction.Down_Right,
                _ => Direction.NA
            };
        }
    }
}