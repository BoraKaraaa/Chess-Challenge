using System;
using ChessChallenge.API;

public class MyBot : IChessBot
{
    private int[] pieceVals = { 0, 10, 30, 30, 50, 90, 900 };
    
    #region PiecePositionHeuristicValues

    private int[] piecePositionalVals = 
    {
        // None
        - 4, -3, -3, -3, -3, -3, -3, -4,
        - 4, -4, -4, -3, -3, -4, -4, -4, 
        - 4, -2, -1, -1, -1, -1, -2, -4,
        - 4, -2, -1, -0, -0, -1, -2, -4,
        - 4, -2, -1, -0, -0, -1, -2, -4,
        - 4, -2, -1, -1, -1, -1, -2, -4,
        - 4, -4, -4, -3, -3, -4, -4, -4,
        - 4, -3, -3, -3, -3, -3, -3, -4
    };

    #endregion

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

    private const int MAX_VAL = 9999999;
    private const int MIN_VAL = -9999999;

    private const int ABSOLUTE_WIN = 100;

    public Move Think(Board board, Timer timer)
    {
        localBoard = board;
        localIsWhite = localBoard.IsWhiteToMove;

        return CalculateNdepthMoves(5);
    }

    private Move CalculateNdepthMoves(int depth)
    {
        MoveNode rootMove = new MoveNode(null, new MoveNode(), new Move(),
            MIN_VAL, 0);
        
        MoveNode resMoveNode = MiniMax(rootMove, depth, MIN_VAL, MAX_VAL, true);

        while (resMoveNode.Depth != 1)
        {
            resMoveNode = resMoveNode.ParentMoveNode;
        }

        return resMoveNode.Move;
    }
    
    private void GenerateChildrenMoveNodes(MoveNode parentMoveNode)
    {
        Move[] captureMoves = localBoard.GetLegalMoves(true);
        Move[] legalMoves = localBoard.GetLegalMoves();

        parentMoveNode.ChildMoves = new MoveNode[legalMoves.Length];

        // Generate ChildMoves 

        int lastIndex = 0;
        
        for (int i = 0; i < captureMoves.Length; i++)
        {
            MoveNode childNode = new MoveNode(null, parentMoveNode, captureMoves[i], 
                MIN_VAL, parentMoveNode.Depth+1);
            
            parentMoveNode.ChildMoves[i] = childNode;
            lastIndex = i+1;
        }
        
        for (int i = 0; i < legalMoves.Length; i++)
        {
            bool sameMoveIncluded = false;
            
            for (int j = 0; j < captureMoves.Length; j++)
            {
                if (captureMoves[j].Equals(legalMoves[i]))
                {
                    sameMoveIncluded = true;
                    break;
                }
            }

            if (!sameMoveIncluded)
            {
                MoveNode childNode = new MoveNode(null, parentMoveNode, legalMoves[i], 
                    MIN_VAL, parentMoveNode.Depth+1);
            
                parentMoveNode.ChildMoves[lastIndex++] = childNode;
            }
        }
    }

    private bool IsWhitePlayingByDepth(int depth)
    {
        return localIsWhite ? depth % 2 == 1 : depth % 2 == 0;
    }
    
    private int CalculatePositionValue(MoveNode moveNode, out bool isCheck)
    {
        int totalHeuristicVal = 0;
        
        isCheck = false;

        if (localBoard.IsInCheckmate())
        {
            if (localBoard.IsWhiteToMove != localIsWhite)
            {
                return MAX_VAL-10;
            }

            return MIN_VAL+10;
        }

        if (localBoard.IsInCheck())
        {
            isCheck = true;
            totalHeuristicVal += 3;
        }

        PieceList[] piecesList = localBoard.GetAllPieceLists();

        foreach (var pieceList in piecesList)
        {
            Piece firstPiece = pieceList.GetPiece(0);

            if (firstPiece.PieceType == PieceType.Pawn)
            {
                foreach (var pawn in pieceList)
                {
                    if (IsWhitePlayingByDepth(moveNode.Depth) == localIsWhite)
                    {
                        if (pawn.IsWhite)
                        {
                            totalHeuristicVal += pawn.Square.Index / 8;
                        }
                        else
                        {
                            totalHeuristicVal += 8 - pawn.Square.Index / 8;
                        }
                    }
                    else
                    {
                        if (pawn.IsWhite)
                        {
                            totalHeuristicVal -= pawn.Square.Index / 8;
                        }
                        else
                        {
                            totalHeuristicVal -= 8 - pawn.Square.Index / 8;
                        }
                    }
                }
            }
            else if (firstPiece.PieceType == PieceType.Queen)
            {
                if (localBoard.PlyCount < 8)
                {
                    totalHeuristicVal -= 10;
                }
            }
            
            if (firstPiece.IsWhite == localIsWhite)
            {
                totalHeuristicVal += pieceVals[(int)pieceList.GetPiece(0).PieceType] * pieceList.Count;
            }
            else
            {
                totalHeuristicVal -= pieceVals[(int)pieceList.GetPiece(0).PieceType] * pieceList.Count;
            }
        }

        ulong whitePiecesBitboard = 0;
        ulong blackPiecesBitboard = 0;
        
        if (localIsWhite)
        {
            whitePiecesBitboard = localBoard.WhitePiecesBitboard;
        }
        else
        {
            blackPiecesBitboard = localBoard.BlackPiecesBitboard;
        }

        for (int i = 63; i >= 0; i--)
        {
            if (IsWhitePlayingByDepth(moveNode.Depth))
            {
                if (((whitePiecesBitboard >> i) & 1) == 1)
                {
                    totalHeuristicVal += piecePositionalVals[i];
                }
            }
            else
            {
                if (((blackPiecesBitboard >> i) & 1) == 1)
                {
                    totalHeuristicVal += piecePositionalVals[i];
                }   
            }
        }
        
        if (moveNode.Move.IsPromotion)
        {
            totalHeuristicVal += 50;
        }

        if (moveNode.Move.IsCastles)
        {
            totalHeuristicVal += 25;
        }

        if (localBoard.IsDraw())
        {
            if (IsWhitePlayingByDepth(moveNode.Depth) == localIsWhite)
            {
                if (totalHeuristicVal > ABSOLUTE_WIN)
                {
                    return -100;
                }
                else
                {
                    return 100;
                }
            }
            else
            {
                if (totalHeuristicVal < ABSOLUTE_WIN)
                {
                    return -100;
                }
                else
                {
                    return 100;
                }
            }
        }

        return totalHeuristicVal;
    }

    private MoveNode MiniMax(MoveNode moveNode, int depth, int alpha, int beta, bool maximizingPlayer)
    {
        bool isCheckMate = localBoard.IsInCheckmate();
        
        if (depth == 0 || isCheckMate || localBoard.IsDraw())
        {
            moveNode.HeuristicVal = CalculatePositionValue(moveNode, out bool isCheck);

            if (isCheckMate)
            {
                moveNode.HeuristicVal += depth;
                return moveNode;
            }
            
            if (!isCheck)
            {
                return moveNode; 
            }

            depth = 1;
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