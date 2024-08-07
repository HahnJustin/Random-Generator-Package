using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using Dalichrome.RandomGenerator.Random;

namespace Dalichrome.RandomGenerator {
    public static class ExtensionMethods
    {
        public static T DeepClone<T>(this T obj)
        {
            using var ms = new MemoryStream();
            var formatter = new BinaryFormatter();
            formatter.Serialize(ms, obj);
            ms.Position = 0;
            return (T)formatter.Deserialize(ms);
        }

        public static List<T> DeepClone<T>(this List<T> list)
        {
            if (list == null)
            {
                throw new ArgumentNullException(nameof(list));
            }

            List<T> clonedList = new(list.Count);
            foreach (T item in list)
            {
                clonedList.Add(item.DeepClone());
            }

            return clonedList;
        }

        public static void Shuffle<T>(this IList<T> list, AbstractRandom random)
        {
            for (int i = list.Count - 1; i > 1; i--)
            {
                int rnd = random.NextInt(i + 1);

                T value = list[rnd];
                list[rnd] = list[i];
                list[i] = value;
            }
        }

        public static float DistanceSquared(this Vector2 a, Vector2 b)
        {
            var dx = b.x - a.x;
            var dy = b.y - a.y;
            return (dx * dx) + (dy * dy);
        }

        public static Texture2D GetTexture(this Sprite sprite) // method to get the cropped sprite as we put it
        {
            // Define the width and height based on the sprite's texture rect
            int width = (int)sprite.textureRect.width;
            int height = (int)sprite.textureRect.height;

            // Get the pixels from the sprite's texture
            var pixels = sprite.texture.GetPixels32();

            // Create a new texture with the same dimensions
            Texture2D texture = new(width, height);

            // Get the pixel colors from the specified section of the original texture
            var extractedPixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int originalIndex = ((int)sprite.textureRect.y + y) * sprite.texture.width + (int)sprite.textureRect.x + x;
                    int extractedIndex = y * width + x;
                    extractedPixels[extractedIndex] = pixels[originalIndex];
                }
            }

            // Set the pixel colors to the new texture
            texture.SetPixels32(extractedPixels);
            texture.Apply();
            return texture;
        }

        public static bool InBounds(this int[,] grid, Vector2Int pos)
        {
            return grid.InBounds(pos.x, pos.y);
        }

        public static bool InBounds(this int[,] grid, int x, int y)
        {
            return grid.GetLength(0) > x && x >= 0 && grid.GetLength(1) > y && y >= 0;
        }

        public static bool HasNeighbor(this int[,] grid, int x, int y, int neighborValue, bool eightDirection = true)
        {
            for (int x1 = x - 1; x1 <= x + 1; x1++)
            {
                for (int y1 = y - 1; y1 <= y + 1; y1++)
                {
                    if (!grid.InBounds(x1, y1) || (x1 == x && y1 == y) || (!eightDirection && x1 != 0 && y1 != 0)) continue;
                    if (grid[x1, y1] == neighborValue) return true;
                }
            }
            return false;
        }

        public static bool HasNoOtherButNeighbor(this int[,] grid, int x, int y, int neighborValue, bool eightDirection = true)
        {
            for (int x1 = x - 1; x1 <= x + 1; x1++)
            {
                for (int y1 = y - 1; y1 <= y + 1; y1++)
                {
                    if (!grid.InBounds(x1, y1) || (x1 == x && y1 == y) || (!eightDirection && x1 != 0 && y1 != 0)) continue;
                    if (grid[x1, y1] != neighborValue) return false;
                }
            }
            return true;
        }

        public static bool IfNeighborDoes(this int[,] grid, int x, int y, Func<int, bool> func, bool eightDirection = true)
        {
            for (int x1 = x - 1; x1 <= x + 1; x1++)
            {
                for (int y1 = y - 1; y1 <= y + 1; y1++)
                {
                    if (!grid.InBounds(x1, y1) || (x1 == x && y1 == y) || (!eightDirection && x1 != 0 && y1 != 0)) continue;
                    if (func(grid[x1, y1])) return true;
                }
            }
            return false;
        }

        public static int HowManyNeighborDoes(this int[,] grid, int x, int y, Func<int, bool> func, bool eightDirection = true)
        {
            int value = 0;
            for (int x1 = x - 1; x1 <= x + 1; x1++)
            {
                for (int y1 = y - 1; y1 <= y + 1; y1++)
                {
                    if (!grid.InBounds(x1, y1) || (x1 == x && y1 == y) || (!eightDirection && x1 != 0 && y1 != 0)) continue;
                    if (func(grid[x1, y1])) value += 1;
                }
            }
            return value;
        }

        public static void SetNeighbors(this int[,] grid, int x, int y, Func<int, int> func, bool eightDirection = true)
        {
            for (int x1 = x - 1; x1 <= x + 1; x1++)
            {
                for (int y1 = y - 1; y1 <= y + 1; y1++)
                {
                    if (!grid.InBounds(x1, y1) || (x1 == x && y1 == y) || (!eightDirection && x1 != 0 && y1 != 0)) continue;
                    grid[x1, y1] = func(grid[x1, y1]);
                }
            }
        }

        public static Vector2Int GetNearestPosition(this int[,] grid, Vector2Int position, int value)
        {
            int x = position.x;
            int y = position.y;

            // Check point (xs, ys)
            for (int d = 1; d < Mathf.Max(grid.GetLength(0), grid.GetLength(1)); d++)
            {
                for (int i = 0; i < d + 1; i++)
                {
                    int x1 = x - d + i;
                    int y1 = y - i;

                    if (x1 < grid.GetLength(0) && x1 > -1 &&
                        y1 < grid.GetLength(1) && y1 > -1 &&
                        grid[x1, y1] == value)
                    {
                        return new Vector2Int(x1, y1);
                    }

                    int x2 = x + d - i;
                    int y2 = y + i;

                    if (x2 < grid.GetLength(0) && x2 > -1 &&
                        y2 < grid.GetLength(1) && y2 > -1 &&
                        grid[x2, y2] == value)
                    {
                        return new Vector2Int(x2, y2);
                    }
                }

                for (int i = 1; i < d; i++)
                {
                    int x1 = x - i;
                    int y1 = y + d - i;

                    if (x1 < grid.GetLength(0) && x1 > -1 &&
                        y1 < grid.GetLength(1) && y1 > -1 &&
                        grid[x1, y1] == value)
                    {
                        return new Vector2Int(x1, y1);
                    }

                    int x2 = x + i;
                    int y2 = y - d + i;

                    if (x2 < grid.GetLength(0) && x2 > -1 &&
                        y2 < grid.GetLength(1) && y2 > -1 &&
                        grid[x2, y2] == value)
                    {
                        return new Vector2Int(x2, y2);
                    }
                }
            }
            return Constants.OutsideGridVectorInt;
        }
    }
}