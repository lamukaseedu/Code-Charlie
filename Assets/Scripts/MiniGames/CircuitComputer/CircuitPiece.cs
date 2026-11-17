using UnityEngine;

public enum CircuitPieceType
{
    UpRight, RightDown, DownLeft, LeftUp, Horizontal, Vertical
}

public enum CircuitDirection { Up, Right, Down, Left }

public enum CircuitCellKind { Empty, Obstacle, Start, End, FixedPiece }

public static class CircuitPiece
{
    /*
     * Author: Andres Rondon-Villarmosa
     * Created: 11/7/2026
     */

    public static CircuitDirection Opposite(CircuitDirection direction) =>
        (CircuitDirection)(((int)direction + 2) % 4);

    public static bool HasOpening(CircuitPieceType piece, CircuitDirection side)
    {
        switch (piece)
        {
            case CircuitPieceType.UpRight:
                return side == CircuitDirection.Up || side == CircuitDirection.Right;
            case CircuitPieceType.RightDown:
                return side == CircuitDirection.Right || side == CircuitDirection.Down;
            case CircuitPieceType.DownLeft:
                return side == CircuitDirection.Down || side == CircuitDirection.Left;
            case CircuitPieceType.LeftUp:
                return side == CircuitDirection.Left || side == CircuitDirection.Up;
            case CircuitPieceType.Horizontal:
                return side == CircuitDirection.Left || side == CircuitDirection.Right;
            case CircuitPieceType.Vertical:
                return side == CircuitDirection.Up || side == CircuitDirection.Down;
            default: return false;
        }
    }

    // incomingSide is the side of this cell the beam ENTERS from.
    public static bool TryGetExit(CircuitPieceType piece, CircuitDirection incomingSide,
        out CircuitDirection exit)
    {
        exit = incomingSide;
        if (!HasOpening(piece, incomingSide)) return false;
        for (int i = 0; i < 4; i++)
        {
            CircuitDirection direction = (CircuitDirection)i;
            if (direction != incomingSide && HasOpening(piece, direction))
            {
                exit = direction;
                return true;
            }
        }
        return false;
    }
}
