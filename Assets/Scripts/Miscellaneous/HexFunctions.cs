using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Miscellaneous
{
    /// <summary>
    /// Hex Help Functions.
    /// It must be understood that unless specified the parameters for all functions will be expecting      NON AXIAL COORDINATES!!
    /// </summary>
    public static class HexFunctions
    {
        public struct HexTile : IEquatable<HexTile>
        {
            // Distance travelled is the distance from the start to the current Tile 
            // Distance left is the distance left from the current to the target tile 

            public int distanceTravelled;
            public int distanceLeft;
            public int totalDistance;

            public Vector3Int AxialPosition;

            /// <summary>
            /// Before calling this, make sure you have called the SetAdjacentTiles method
            /// </summary>
            public List<HexTile> adjacentTiles;

            public HexTile(Vector3Int axialPosition, bool setAdjacent = false)
            {
                this.AxialPosition = axialPosition;
                distanceLeft = 0;
                distanceTravelled = 0;
                totalDistance = 0;
                adjacentTiles = new List<HexTile>();

                if (setAdjacent)
                {
                    SetAdjacentTiles();
                }
            }

            // Calling this in the constructor will cause a stack overflow
            // since for each adjacent tile you will have to get its adjecent tiles
            // then get the adjacent tiles of the adjacent tiles... etc
            public void SetAdjacentTiles()
            {
                foreach (Vector3Int pos in GetNeighbors(Axial.NonAxialOffset(AxialPosition), MapHexSize))
                {
                    Vector3Int newPos = Axial.AxialOffsetPosition(pos.x, pos.y);

                    adjacentTiles.Add(new HexTile(newPos));
                }
            }

            // We need this because although 2 different types might have thesame position
            // other instance variables might be different, thus the types wont be equal
            bool IEquatable<HexTile>.Equals(HexTile other)
            {
                return AxialPosition.Equals(other.AxialPosition);
            }
        }
        public struct Axial
        {
            public int x;
            public int y;
            public int z;

            /// <summary>
            /// Converts a position (X, Y) to axial coordinates. Returns Axial Struct
            /// </summary>
            /// <param name="x"></param>
            /// <param name="y"></param>
            /// <returns>A new Axial Class</returns>
            public static Axial AxialFromOffset(int x, int y)
            {
                Axial a = new Axial();
                a.x = x - (y - (y & 1)) / 2;
                a.y = y;
                a.z = -a.x - a.y;
                return a;
            }
            /// <summary>
            /// Converts a position to axial coordinates. Returns Axial Struct
            /// </summary>
            /// <param name="pos">Non Axial Coordinate to convert</param>
            /// <returns>returns a new Axial Class</returns>
            public static Axial AxialFromOffset(Vector3Int pos)
            {
                Axial a = new Axial();
                a.x = pos.x - (pos.y - (pos.y & 1)) / 2;
                a.y = pos.y;
                a.z = -a.x - a.y;
                return a;
            }
            /// <summary>
            /// Converts a position (X, Y) to axial coordinates
            /// </summary>
            /// <param name="x"></param>
            /// <param name="y"></param>
            /// <returns>The Vector3Int containing the new Axial position</returns>
            public static Vector3Int AxialOffsetPosition(int x, int y)
            {
                Axial a = new Axial();
                a.x = x - (y - (y & 1)) / 2;
                a.y = y;
                a.z = -a.x - a.y;

                return a.Coordinates;
            }
            /// <summary>
            /// Converts a position (X, Y) to axial coordinates
            /// </summary>
            /// <param name="x"></param>
            /// <param name="y"></param>
            /// <returns>The Vector3Int containing the new Axial position</returns>
            public static Vector3Int AxialOffsetPosition(Vector3Int pos)
            {
                return (AxialOffsetPosition(pos.x, pos.y));
            }

            /// <summary>
            /// Converts an Axial position to a Non Axial position
            /// </summary>
            /// <param name="axialOffset"></param>
            /// <returns></returns>
            public static Vector3Int NonAxialOffset(Vector3Int axialOffset)
            {
                int x = axialOffset.x + ((axialOffset.y - (axialOffset.y & 1)) / 2);

                return new Vector3Int(x, axialOffset.y, 0);
            }

            public Vector3Int Coordinates { get { return new Vector3Int(x, y, z); } }
        }

        public static Vector2Int MapHexSize { get; set; }

        /// <summary>
        /// Finds path from given start point to end point. Returns an empty list if the path couldn't be found.
        /// </summary>
        /// <param name="startPoint">Start tile.Non Axial Position</param>
        /// <param name="endPoint">Destination tile. Non Axial Position</param>
        /// Credits: https://blog.theknightsofunity.com/pathfinding-on-a-hexagonal-grid-a-algorithm/
        public static List<HexTile> FindPath(Vector3Int start, Vector3Int stop, Vector3Int maxSize)
        {
            HexTile startPoint = new HexTile(Axial.AxialOffsetPosition(start.x, start.y));
            HexTile stopPoint = new HexTile(Axial.AxialOffsetPosition(stop.x, stop.y));

            List<HexTile> openPathTiles = new List<HexTile>();
            List<HexTile> closedPathTiles = new List<HexTile>();

            List<HexTile> closestTiles = new List<HexTile>();

            // Prepare the start tile.
            HexTile currentTile = startPoint;

            currentTile.distanceTravelled = 0;
            currentTile.distanceLeft = GetEstimatedPathCost(startPoint.AxialPosition, stopPoint.AxialPosition, maxSize);

            // Add the start tile to the open list.
            openPathTiles.Add(currentTile);

            HexTile tempTile;

            while (openPathTiles.Count != 0)
            {
                // Sorting the open list to get the tile with the lowest F.
                openPathTiles = openPathTiles.OrderBy(x => x.totalDistance)
                    .ThenBy(x => x.distanceLeft).ToList();

                tempTile = openPathTiles[0];

                closestTiles = openPathTiles.Where(x => (x.distanceLeft == tempTile.distanceLeft)
                                            && (x.totalDistance == tempTile.totalDistance))
                                .OrderBy(x => XDistance(stopPoint.AxialPosition, x.AxialPosition)).ToList();

                currentTile = closestTiles[0];

                // Removing the current tile from the open list and adding it to the closed list.
                openPathTiles.Remove(currentTile);
                closedPathTiles.Add(currentTile);

                int distanceTravelled = currentTile.distanceTravelled + 1;

                // If there is a target tile in the closed list, we have found a path.
                if (closedPathTiles.Contains(stopPoint))
                {
                    break;
                }

                currentTile.SetAdjacentTiles();

                // Investigating each adjacent tile of the current tile.
                for (int i = 0; i < currentTile.adjacentTiles.Count; i++)
                {
                    HexTile adjacentTile = currentTile.adjacentTiles[i];

                    // Ignore not walkable adjacent tiles.
                    //if (adjacentTile.isObstacle)
                    //{
                    //    continue;
                    //}

                    // Ignore the tile if it's already in the closed list.
                    if (closedPathTiles.Contains(adjacentTile))
                    {
                        continue;
                    }

                    // If it's not in the open list - add it and compute G and H.
                    if (!(openPathTiles.Contains(adjacentTile)))
                    {
                        HexTile adj = adjacentTile;

                        adj.distanceTravelled = distanceTravelled;
                        adj.distanceLeft = GetEstimatedPathCost(adjacentTile.AxialPosition, stopPoint.AxialPosition, maxSize);
                        openPathTiles.Add(adj);
                    }
                    // Otherwise check if using current G we can get a lower value of F, if so update it's value.
                    else if (adjacentTile.totalDistance > distanceTravelled + adjacentTile.distanceLeft)
                    {
                        adjacentTile.distanceTravelled = distanceTravelled;
                    }
                }
            }

            List<HexTile> finalPathTiles = new List<HexTile>();

            // Backtracking - setting the final path.
            if (closedPathTiles.Contains(stopPoint))
            {
                // the last point should be the stop point
                currentTile = closedPathTiles.Last();
                currentTile.SetAdjacentTiles();

                finalPathTiles.Add(currentTile);

                for (int i = currentTile.distanceTravelled - 1; i >= 0; i--)
                {
                    currentTile = closedPathTiles.Find(x => x.distanceTravelled == i && currentTile.adjacentTiles.Contains(x));
                    finalPathTiles.Add(currentTile);
                }

                finalPathTiles.Reverse();
            }

            return finalPathTiles;

            // Measures the X distance between a target and a position
            // This is used to prevent the pathfinding from only going diagonally
            // Instead the pathfinding takes more of a straight-ish path
            // it feels more human...           
            int XDistance(Vector3Int target, Vector3Int currentPosition)
            {
                // this means the positions are on thesame row
                if (target.y == currentPosition.y)
                {
                    Debug.Log("Same Row");
                    Debug.Log(currentPosition.ToString());
                    return 0;
                }

                target = Axial.NonAxialOffset(target);
                currentPosition = Axial.NonAxialOffset(currentPosition);

                int temp1;

                temp1 = Mathf.Abs(target.x - currentPosition.x);

                return temp1;
            }
        }

        /// <summary>
        /// Returns estimated path cost from given start position to target position of hex tile using Manhattan distance.
        /// </summary>
        /// <param name="startPosition">Start position.</param>
        /// <param name="targetPosition">Destination position.</param>
        /// <param name="isAxial">Default is true, set to false if the parameters are non axial coordinates.</param>
        /// 
        private static int GetEstimatedPathCost(Vector3Int startPosition, Vector3Int targetPosition, Vector3Int maxSize, bool isAxial = true)
        {
            if (isAxial)
            {
                startPosition = Axial.NonAxialOffset(startPosition);
                targetPosition = Axial.NonAxialOffset(targetPosition);

                return CalculateDistance(startPosition, targetPosition, maxSize, Edges.Horizontal);
            }
            else
            {
                return CalculateDistance(startPosition, targetPosition, maxSize, Edges.Horizontal);
            }
            // this method can wrap and not wrap


            // the below code works if you are not wrapping...
            //return Mathf.Max(Mathf.Abs(startPosition.Z - targetPosition.Z), Mathf.Max(Mathf.Abs(startPosition.X - targetPosition.X), Mathf.Abs(startPosition.Y - targetPosition.Y)));
        }

        public enum Edges { None, Horizontal, Vertical, Both }

        public static int CalculateDistanceAsPercent(Vector3Int start, Vector3Int stop, Vector3Int maxSize, Edges edges = Edges.Horizontal, bool circle = true)
        {
            float distanceX = 0;
            float distanceY = 0;

            // When wrapping values across the map for any axis, do not account for the Z values
            // since wrapping only takes place across a 2d axis, we only need to account for X and Y axis
            switch (edges)
            {
                case Edges.None:
                    return (int)Vector3Int.Distance(start, stop);
                case Edges.Horizontal:

                    distanceX = GetWrappedShortestDistance(maxSize.x, start.x, stop.x);
                    distanceY = GetShortestDistance(maxSize.y, start.y, stop.y);
                    
                    break;
                case Edges.Vertical:
                    distanceX = GetShortestDistance(maxSize.x, start.x, stop.x);
                    distanceY = GetWrappedShortestDistance(maxSize.y, start.y, stop.y);
                    break;
                case Edges.Both:
                    distanceX = GetWrappedShortestDistance(maxSize.x, start.x, stop.x);
                    distanceY = GetWrappedShortestDistance(maxSize.y, start.y, stop.y);
                    break;
                default:
                    break;
            }

            float wrapedDistance;


            if (circle)
            {
                // do pythagoreom
                wrapedDistance = Mathf.Sqrt(Mathf.Pow(distanceX, 2) + Mathf.Pow(distanceY, 2));
            }
            else
            {
                // just add both distances
                wrapedDistance =  Mathf.Max(distanceX, distanceY);
            }

            return Mathf.RoundToInt(wrapedDistance * 100);

            #region
            // Credits
            // https://blog.demofox.org/2017/10/01/calculating-the-distance-between-points-in-wrap-around-toroidal-space/
            #endregion
            
            static float GetWrappedShortestDistance(int length, int start, int stop)
            {
                // get the new distance here
                float distance = Mathf.Abs(start - stop);

                float halfLength = Mathf.CeilToInt(length / 2);

                if (distance > halfLength)
                {
                    distance = length - distance;
                }

                return distance / length;
            }

            static float GetShortestDistance(int length, int start, int stop)
            {
                return Mathf.Abs(start - stop) / length;
            }

        }

        public static int CalculateDistance(Vector3Int start, Vector3Int stop, Vector3Int maxSize, Edges edges = Edges.Horizontal)
        {

            Vector3Int start1 = Axial.NonAxialOffset(start);
            Vector3Int stop1 = Axial.NonAxialOffset(stop);

            start = Axial.AxialOffsetPosition(start.x, start.y);
            stop = Axial.AxialOffsetPosition(stop.x, stop.y);

            int halfWidth = Mathf.CeilToInt(MapHexSize.y / 2);

            int distanceX = 0;
            int distanceY = 0;
            int distanceZ = 0;

            // When wrapping values across the map for any axis, do not account for the Z values
            // since wrapping only takes place across a 2d axis, we only need to account for X and Y axis
            switch (edges)
            {
                case Edges.None:
                    return (int)Vector3Int.Distance(start, stop);
                case Edges.Horizontal:

                    distanceX = GetWrappedShortestDistance(maxSize.x, start.x, stop.x);
                    distanceY = GetShortestDistance(start.y, stop.y);
                    distanceZ = 0;

                    break;
                case Edges.Vertical:
                    distanceX = GetShortestDistance(start.x, stop.x);
                    distanceY = GetWrappedShortestDistance(maxSize.y, start.y, stop.y);
                    distanceZ = 0;
                    break;
                case Edges.Both:
                    distanceX = GetWrappedShortestDistance(maxSize.x, start.x, stop.x);
                    distanceY = GetWrappedShortestDistance(maxSize.y, start.y, stop.y);
                    distanceZ = 0;
                    break;
                default:
                    break;
            }

            // do pythagoreom
            float wrapedDistance = Mathf.Sqrt(Mathf.Pow(distanceX, 2) + Mathf.Pow(distanceY, 2));

            return Mathf.RoundToInt(wrapedDistance);

            #region
            // Credits
            // https://blog.demofox.org/2017/10/01/calculating-the-distance-between-points-in-wrap-around-toroidal-space/
            #endregion
            static int GetWrappedShortestDistance(int length, int start, int stop)
            {
                // get the new distance here
                int distance = Mathf.Abs(start - stop);

                int halfLength = Mathf.CeilToInt(length / 2);

                if (distance > halfLength)
                {
                    distance = length - distance;
                }

                return distance;
            }

            static int GetShortestDistance(int start, int stop)
            {
                return Mathf.Abs(start - stop);
            }

        }
        public static List<Vector3Int> GetSurroundingTiles(Vector3Int initialPosition, Vector2Int mapSize, int distance = 1)
        {
            // the loop order...do not MODIFY!!
            int[] loopOrder = new int[] { 1, 3, 4, 5, 6, 1, 2 };

            List<Vector3Int> surroundingTiles = new List<Vector3Int>();

            if (distance < 1)
            {
                distance = 1;
            }

            Vector3Int currentPos = initialPosition;
            Vector3Int startPos = initialPosition;

            int counter = 1;

            while (counter <= distance)
            {
                for (int s = 0; s < loopOrder.Length; s++)
                {
                    for (int i = 1; i <= counter; i++)
                    {
                        currentPos = GetNeighborHex(currentPos, loopOrder[s], mapSize);

                        surroundingTiles.Add(currentPos);

                        if (s == 0)
                        {
                            // the first value of the loopOrder array is merely meant to set the start position
                            // it should not be repeated
                            startPos = currentPos;
                            break;
                        }
                    }
                }

                // we start back from position one
                currentPos = startPos;

                counter++;

            }

            return surroundingTiles;
        }

        public static int GetOppositeNeighbor(int neighbor)
        {
            // will return numbers from 1 - 6

            if (neighbor <= 3)
            {
                return neighbor + 3;
            }
            else
            {
                return neighbor - 3;
            }
        }

        private static Vector3Int GetNeighborHex(Vector3Int curPos, int neighborSide, Vector2Int mapSize)
        {
            /// PLEASE BE ADVICED, THE Y POSITION OF THE HEX MATTERS
            /// WE HAVE TO ACCOUNT FOR CHANGES IF THE Y POSITION IS ODD OR EVEN
            Vector3Int tempPos = curPos;

            switch (neighborSide)
            {
                case 1:
                    // increase both X and Y by 1

                    if (tempPos.y % 2 == 1)
                    {
                        tempPos.x += 1;
                    }

                    tempPos.y += 1;

                    break;
                case 2:

                    tempPos.x += 1;

                    break;
                case 3:

                    if (tempPos.y % 2 == 1)
                    {
                        tempPos.x += 1;
                    }

                    tempPos.y -= 1;

                    break;
                case 4:

                    if (tempPos.y % 2 == 0)
                    {
                        tempPos.x -= 1;
                    }

                    tempPos.y -= 1;

                    break;
                case 5:

                    tempPos.x -= 1;
                    break;

                default:
                    // case 6

                    if (tempPos.y % 2 == 0)
                    {
                        tempPos.x -= 1;
                    }

                    tempPos.y += 1;
                    break;
            }


            if (tempPos.x >= mapSize.x)
            {
                tempPos.x -= mapSize.x;
            }

            if (tempPos.x < 0)
            {
                tempPos.x += mapSize.x;
            }

            if (tempPos.y >= mapSize.y)
            {
                tempPos.y -= mapSize.y;
            }

            if (tempPos.y < 0)
            {
                tempPos.y += mapSize.y;
            }

            return tempPos;
        }

        /// <summary>
        /// Gets the neighbor hexes in order, from position 0 - 5
        /// </summary>
        /// <returns></returns>
        public static Vector3Int[] GetNeighbors(Vector3Int curPos, Vector2Int mapSize)
        {
            Vector3Int[] neighbors = new Vector3Int[6];

            // we start at index 1 because out getNeighborHex is also index from the number 1
            for (int i = 1; i <= 6; i++)
            {
                neighbors[i - 1] = GetNeighborHex(curPos, i, mapSize);
            }

            return neighbors;
        }

        public static List<Vector3Int> DrawHexShape(int maxWidth, int minWidth, Vector3Int startPos)
        {
            return DrawFlatHeadHex(maxWidth, minWidth, startPos);
        }

        private static List<Vector3Int> DrawFlatHeadHex(int maxWidth, int minWidth, Vector3Int startPos)
        {
            List<Vector3Int> axialCoor = new List<Vector3Int>();

            // will use axial system

            int height = maxWidth - minWidth; // height of each side of the hex

            // top side

            int endXPos = startPos.x + minWidth; // represents the X position to stop at

            int startX = startPos.x; //start at top
            int startY = startPos.y + height;       // start at top

            // draw top to middle (including middle)
            for (int y = startY; y >= startPos.y; y--)
            {
                for (int x = startX; x < endXPos; x++)
                {
                    axialCoor.Add(new Vector3Int(x, y, 0));
                }
                // every row increase count by 1
                endXPos++;
            }

            // draw middle to bottom (excluding middle)

            startX = startPos.x + height; // start at bottom
            startY = startPos.y - height; // start at bottom

            endXPos = startX + minWidth; // represents the X position to stop at

            for (int y = startY; y < startPos.y; y++)
            {
                for (int x = startX; x < endXPos; x++)
                {
                    axialCoor.Add(new Vector3Int(x, y, 0));
                }

                startX--;
            }

            List<Vector3Int> returnHexes = new();

            foreach (Vector3Int pos in axialCoor)
            {
                returnHexes.Add(Axial.NonAxialOffset(pos));
            }

            return returnHexes;
        }

        private static double HaversineDistance(double lat1, double lon1,
                        double lat2, double lon2, Vector2Int mapsize)
        {
            // distance between latitudes and longitudes
            double dLat = (Math.PI / 180) * (lat2 - lat1);
            double dLon = (Math.PI / 180) * (lon2 - lon1);

            // convert to radians
            lat1 = (Math.PI / 180) * (lat1);
            lat2 = (Math.PI / 180) * (lat2);

            // apply formulae
            double a = Math.Pow(Math.Sin(dLat / 2), 2) +
                       Math.Pow(Math.Sin(dLon / 2), 2) *
                       Math.Cos(lat1) * Math.Cos(lat2);
            
            double rad = Mathf.Sqrt((mapsize.x * mapsize.y) / Mathf.PI);
            
            double c = 2 * Math.Asin(Math.Sqrt(a));
            return rad * c;
        }

        internal static float HaversineDistance(Vector3Int position, Vector3Int currentSunRayFocus, Vector2Int mapSize)
        {
            Vector3Int pos1 = position;
            Vector3Int pos2 = currentSunRayFocus;

            return (float)HaversineDistance(pos1.x, pos1.y, pos2.x, pos2.y, mapSize);
        }
    }
}
