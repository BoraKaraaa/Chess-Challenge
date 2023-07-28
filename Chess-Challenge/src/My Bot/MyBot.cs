using System;
using ChessChallenge.API;

public class MyBot : IChessBot
{
    private int[] pieceVals = { 0, 10, 30, 30, 50, 90, 900 };

    private int[] piecePositionalVals = 
    {
        - 3, -3, -3, -3, -3, -3, -3, -3,
        - 3, -2, -2, -2, -2, -2, -2, -3,
        - 3, -2, -1, -1, -1, -1, -2, -3,
        - 3, -2, -1, -0, -0, -1, -2, -3,
        - 3, -2, -1, -0, -0, -1, -2, -3,
        - 3, -2, -1, -1, -1, -1, -2, -3,
        - 3, -2, -2, -2, -2, -2, -2, -3,
        - 3, -3, -3, -3, -3, -3, -3, -3,
    };
    
    private class MoveNode
    {
        public Move Move;
        public MoveNode ParentMoveNode;
        public MoveNode[] ChildMoves; 
        public int HeuristicVal;
        public int Depth;
        
        public MoveNode() { }
        public MoveNode(MoveNode[] childMoves, MoveNode parentMoveNode, Move move, int heuristicVal, int depth)
        {
            HeuristicVal = heuristicVal;
            Move = move;
            ChildMoves = childMoves;
            ParentMoveNode = parentMoveNode;
            Depth = depth;
        }
    }

    private Board localBoard;
    private bool localIsWhite;

    private const int MAX_VAL = 999999;
    private const int MIN_VAL = -999999;

    public Move Think(Board board, Timer timer)
    {
        localBoard = board;
        localIsWhite = localBoard.IsWhiteToMove;
        
        return CalculateNdepthMoves(4);
    }

    private Move CalculateNdepthMoves(int depth)
    {
        
        //Move[] legalMoves = localBoard.GetLegalMoves();
        
        MoveNode rootMove = new MoveNode(null, new MoveNode(), new Move(),
            MIN_VAL, 0);

        /*
        for (int i = 0; i < legalMoves.Length; i++)
        {
            MoveNode childNode = new MoveNode(null, rootMove, legalMoves[i], 
                MIN_VAL, 1);
            rootMove.ChildMoves[i] = childNode;

            GenerateChildren(ref childNode, depth - 1);
        }
        */
        
        MoveNode resMoveNode = MiniMax(rootMove, depth, MIN_VAL, MAX_VAL, true);

        while (resMoveNode.Depth != 1)
        {
            resMoveNode = resMoveNode.ParentMoveNode;
        }

        return resMoveNode.Move;
    }
    
    private void GenerateChildren(ref MoveNode parentNode, int depth)
    {
        if (depth == 0)
        {
            return;
        }
        
        localBoard.MakeMove(parentNode.Move);

        Move[] legalMoves = localBoard.GetLegalMoves();

        parentNode.ChildMoves = new MoveNode[legalMoves.Length];
        
        for (int i = 0; i < legalMoves.Length; i++)
        {
            MoveNode childNode = new MoveNode(null, parentNode, legalMoves[i],
                MIN_VAL, depth+1);
            
            parentNode.ChildMoves[i] = childNode;
            GenerateChildren(ref childNode, depth - 1);
        }
        
        localBoard.UndoMove(parentNode.Move);
    }

    private void GenerateChildrenMoveNodes(MoveNode parentMoveNode)
    {
        Move[] legalMoves = localBoard.GetLegalMoves();

        parentMoveNode.ChildMoves = new MoveNode[legalMoves.Length];
        
        // Generate ChildMoves 
        for (int i = 0; i < legalMoves.Length; i++)
        {
            MoveNode childNode = new MoveNode(null, parentMoveNode, legalMoves[i], 
                MIN_VAL, parentMoveNode.Depth+1);
            
            // Order Moves
            parentMoveNode.ChildMoves[i] = childNode;
        }
    }

    private bool IsWhitePlayingByDepth(int depth)
    {
        return localIsWhite ? depth % 2 == 1 : depth % 2 == 0;
    }
    
    private int CalculatePositionValue(Move move)
    {
        int totalHeuristicVal = 0;

        if (localBoard.IsInCheckmate())
        {
            if (localBoard.IsWhiteToMove != localIsWhite)
            {
                totalHeuristicVal += 1000;
            }

            totalHeuristicVal += -1000;
        }
        
        /*
        if (localBoard.IsDraw())
        {
            totalHeuristicVal = 0;
        }
        */
        
        if (localBoard.IsInCheck())
        {
            totalHeuristicVal += 3;
        }

        PieceList[] piecesList = localBoard.GetAllPieceLists();

        foreach (var pieceList in piecesList)
        {
            Piece piece = pieceList.GetPiece(0);

            if (piece.IsWhite == localIsWhite)
            {
                totalHeuristicVal += pieceVals[(int)pieceList.GetPiece(0).PieceType] * pieceList.Count;
            }
            else
            {
                totalHeuristicVal -= pieceVals[(int)pieceList.GetPiece(0).PieceType] * pieceList.Count;
            }
        }

        ulong whitePiecesBitboard = localBoard.WhitePiecesBitboard;
        ulong blackPiecesBitboard = localBoard.BlackPiecesBitboard;

        for (int i = 63; i >= 0; i--)
        {
            if (((whitePiecesBitboard >> i) & 1) == 1)
            {
                if (localIsWhite)
                {
                    totalHeuristicVal += piecePositionalVals[i];
                }
            }
            
            if (((blackPiecesBitboard >> i) & 1) == 1)
            {
                if (!localIsWhite)
                {
                    totalHeuristicVal += piecePositionalVals[i];
                }
            }
        }
        
        return totalHeuristicVal;
    }

    private MoveNode MiniMax(MoveNode moveNode, int depth, int alpha, int beta, bool maximizingPlayer)
    {
        if (depth == 0 || localBoard.IsInCheckmate() || localBoard.IsDraw())
        {
            moveNode.HeuristicVal = CalculatePositionValue(moveNode.Move);
            return moveNode;
        }

        GenerateChildrenMoveNodes(moveNode);
        
        MoveNode localMoveNode = new MoveNode();
        
        if (maximizingPlayer)
        {
            localMoveNode.HeuristicVal = MIN_VAL;
            
            foreach (var childMove in moveNode.ChildMoves)
            {
                localBoard.MakeMove(childMove.Move);
                
                MoveNode resMoveNode = MiniMax(childMove, depth - 1, alpha, beta, false);
                
                localBoard.UndoMove(childMove.Move);
                
                if (localMoveNode.HeuristicVal < resMoveNode.HeuristicVal)
                {
                    localMoveNode = resMoveNode;
                }
                
                alpha = Math.Max(alpha, localMoveNode.HeuristicVal);
                
                if (localMoveNode.HeuristicVal >= beta)
                {
                    break;
                }
            }
        }
        else
        {
            localMoveNode.HeuristicVal = MAX_VAL;
            
            foreach (var childMove in moveNode.ChildMoves)
            {
                localBoard.MakeMove(childMove.Move);
                
                MoveNode resMoveNode = MiniMax(childMove, depth - 1, alpha, beta, true);
                
                localBoard.UndoMove(childMove.Move);
                
                if (localMoveNode.HeuristicVal > resMoveNode.HeuristicVal)
                {
                    localMoveNode = resMoveNode;
                }
                
                beta = Math.Min(beta, localMoveNode.HeuristicVal);
                
                if (localMoveNode.HeuristicVal <= alpha)
                {
                    break;
                }
            }
        }
        
        return localMoveNode;
    }


}