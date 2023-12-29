using ChessChallenge.API;
using System;

public class MyBot : IChessBot
{
    // Piece values -> None, Pawn, Knight, Bishop, Rook, Queen, King
    private static readonly int[] PieceVals = { 0, 10, 30, 35, 50, 90, 900 };
    
    // Piece positions -> additional values
    private static readonly int[] PiecePositionalVals =
    {
        -4, -4, -4, -3, -3, -4, -4, -4,
        -4, -4, -4, -3, -3, -4, -4, -4,
        -4, -2, -1, -2, -2, -1, -2, -4,
        -4, -2, -1,  0,  0, -1, -2, -4,
        -4, -2, -1,  0,  0, -1, -2, -4,
        -4, -2, -1, -2, -2, -1, -2, -4,
        -4, -4, -4, -3, -3, -4, -4, -4,
        -4, -4, -4, -3, -3, -4, -4, -4
    };
    
    // The structure where necessary information for each move is stored
    private class MoveNode
    {
        public readonly Move Move;
        public readonly MoveNode ParentMoveNode;
        public MoveNode[] ChildMoves;
        public int HeuristicVal;
        public readonly int Depth;

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

    private Board _localBoard;
    private bool _localIsWhite;

    private const int MAX_VAL = 9999999;
    private const int MIN_VAL = -9999999;

    public Move Think(Board board, Timer timer)
    {
        _localBoard = board;
        _localIsWhite = _localBoard.IsWhiteToMove;

        return CalculateNDepthMoves(5);
    }

    private Move CalculateNDepthMoves(int depth)
    {
        // Initialize Root MoveNode
        MoveNode rootMove = new MoveNode(null, new MoveNode(), new Move(), 0, 0);
        
        // Run MiniMax algorithm for calculating Resulted MoveNode
        MoveNode resMoveNode = MiniMax(rootMove, depth, MIN_VAL, MAX_VAL, true);
        
        // Get the next move from resulted MiniMax algorithm's MoveNode
        while (resMoveNode.Depth != 1)
        {
            resMoveNode = resMoveNode.ParentMoveNode;
        }
        
        // Return resulted MoveNode
        return resMoveNode.Move;
    }
    
    
    /// <summary>
    /// Generates and assigns all possible moves from the given node to its "ChildMoves" array,
    /// optimizing it for faster execution of the Minimax algorithm. The ordering is designed to facilitate
    /// the algorithm in finding the correct result more efficiently. This is achieved by prioritizing capture
    /// moves at the beginning of the array.
    /// </summary>
    /// <param name="parentMoveNode">The parent node from which child nodes are generated.</param>
    private void GenerateChildrenMoveNodes(MoveNode parentMoveNode)
    {
        // Capture all legal moves and separate them into captureMoves and legalMoves arrays
        Move[] captureMoves = _localBoard.GetLegalMoves(true);
        Move[] legalMoves = _localBoard.GetLegalMoves();

        parentMoveNode.ChildMoves = new MoveNode[legalMoves.Length];

        int lastIndex = 0;
        
        // Generate child nodes for capture moves
        for (int i = 0; i < captureMoves.Length; i++)
        {
            MoveNode childNode = new MoveNode(null, parentMoveNode, captureMoves[i],
                parentMoveNode.ParentMoveNode.HeuristicVal, parentMoveNode.Depth + 1);
            
            // Calculate and update the PreHeuristic value for the child node
            childNode.HeuristicVal += CalculatePreHeuristicValue(childNode);

            parentMoveNode.ChildMoves[i] = childNode;
            lastIndex = i + 1;
        }
        
        // Generate child nodes for non-capture moves
        for (int i = 0; i < legalMoves.Length; i++)
        {
            bool sameMoveIncluded = false;
            
            // Check if the legal move is already included in captureMoves
            for (int j = 0; j < captureMoves.Length; j++)
            {
                if (captureMoves[j].Equals(legalMoves[i]))
                {
                    sameMoveIncluded = true;
                    break;
                }
            }
            
            // If the legal move is not a capture move, create a new child node
            if (!sameMoveIncluded)
            {
                MoveNode childNode = new MoveNode(null, parentMoveNode, legalMoves[i],
                    parentMoveNode.ParentMoveNode.HeuristicVal, parentMoveNode.Depth + 1);
                
                // Calculate and update the PreHeuristic value for the child node
                childNode.HeuristicVal += CalculatePreHeuristicValue(childNode);

                parentMoveNode.ChildMoves[lastIndex++] = childNode;
            }
        }
    }
    
    /// <summary>
    /// Determines if the side playing at the given depth is the white side in a chess game.
    /// The function takes into account the initial side (_localIsWhite) and calculates the current side
    /// based on the depth of the search.
    /// </summary>
    /// <param name="depth">The depth of the search, indicating the current move number or ply in the game tree.</param>
    /// <returns>
    /// "true" if the white side is playing at the given depth; otherwise, "false". 
    /// </returns>
    private bool IsWhitePlayingByDepth(int depth)
    {
        return _localIsWhite ? depth % 2 == 1 : depth % 2 == 0;
    }
    
    
    /// <summary>
    /// Calculates and returns the pre-heuristic value for a given move node in the Minimax algorithm.
    /// The pre-heuristic value is determined by summing up values received from its parent nodes, representing
    /// the move's significance in the context of the game tree. Additionally, the heuristic value of the final move is
    /// calculated in the <see cref="CalculatePositionValue"/> function.
    /// </summary>
    /// <param name="moveNode">The move node for which the pre-heuristic value is calculated.</param>
    /// <returns>
    /// Returned the pre-heuristic value, representing the aggregated importance of the move derived from its parent nodes. 
    /// </returns>
    private int CalculatePreHeuristicValue(MoveNode moveNode)
    {
        int preHeuristicVal = 0;
        
        // If the move involves pawn promotion, adjust the pre-heuristic value accordingly
        if (moveNode.Move.IsPromotion)
        {
            preHeuristicVal += IsWhitePlayingByDepth(moveNode.Depth) == _localIsWhite ? 60 : -60;
        }
        
        // If the move involves castling, adjust the pre-heuristic value accordingly
        if (moveNode.Move.IsCastles)
        {
            preHeuristicVal += IsWhitePlayingByDepth(moveNode.Depth) == _localIsWhite ? 15 : -15;
        }
        
        // Return calculated "preHeuristicVal"
        return preHeuristicVal;
    }
    
    /// <summary>
    /// Calculates the heuristic value for a given move node in the Minimax algorithm based on the current board state.
    /// Additionally, it checks for checkmate, check, and evaluates the positional values of pieces.
    /// </summary>
    /// <param name="moveNode">The move node for which the heuristic value is calculated.</param>
    /// <param name="isCheck">An out parameter indicating whether the current move results in a check.</param>
    /// <returns>
    /// The heuristic value representing the overall evaluation of the current board state.
    /// This value incorporates factors such as material values, positional advantages, and checks.
    /// If the game is in checkmate, a special value is returned (-10,000 for checkmate against the maximizing player,
    /// and 10,000 for checkmate against the minimizing player).
    /// </returns>
    private int CalculatePositionValue(MoveNode moveNode, out bool isCheck)
    {
        int totalHeuristicVal = moveNode.HeuristicVal;

        isCheck = false;
        
        // Check for checkmate
        if (_localBoard.IsInCheckmate())
        {
            if (IsWhitePlayingByDepth(moveNode.Depth) == _localIsWhite)
            {
                return MAX_VAL - 10;
            }

            return MIN_VAL + 10;
        }
        
        // Check for check and update the heuristic value accordingly
        if (_localBoard.IsInCheck() && (IsWhitePlayingByDepth(moveNode.Depth) == _localIsWhite))
        {
            isCheck = true;
            totalHeuristicVal += 3;
        }
        
        // Evaluate piece values and positional advantages
        PieceList[] piecesList = _localBoard.GetAllPieceLists();

        foreach (var pieceList in piecesList)
        {
            Piece firstPiece = pieceList.GetPiece(0);

            if (firstPiece.PieceType == PieceType.Pawn)
            {
                foreach (var pawn in pieceList)
                {
                    if (IsWhitePlayingByDepth(moveNode.Depth) == _localIsWhite)
                    {
                        // Adjust the heuristic value based on pawn positions
                        totalHeuristicVal += pawn.IsWhite ? pawn.Square.Index / 8 : 8 - pawn.Square.Index / 8;
                    }
                }
            }
            
            // Adjust the heuristic value based on material values and piece count
            totalHeuristicVal += firstPiece.IsWhite == _localIsWhite ? PieceVals[(int)firstPiece.PieceType] * pieceList.Count :
                -PieceVals[(int)firstPiece.PieceType] * pieceList.Count;
        }
        
        // Evaluate positional advantages based on bitboards
        ulong pieceBitboard = _localIsWhite ? _localBoard.WhitePiecesBitboard : _localBoard.BlackPiecesBitboard;

        for (int i = 63; i >= 0; i--)
        {
            if (((pieceBitboard >> i) & 1) == 1)
            {
                totalHeuristicVal += IsWhitePlayingByDepth(moveNode.Depth) == _localIsWhite ? PiecePositionalVals[i] : -PiecePositionalVals[i];
            }
        }

        return totalHeuristicVal;
    }
    
    /// <summary>
    /// Executes the Minimax algorithm to determine the optimal move for the current board state up to a certain depth
    /// in the game tree. The algorithm considers maximizing and minimizing players, alpha-beta pruning for optimization,
    /// and evaluates heuristic values using the <see cref="CalculatePositionValue"/> function.
    /// </summary>
    /// <param name="moveNode">The current move node being evaluated in the Minimax algorithm.</param>
    /// <param name="depth">The remaining depth of the search in the game tree.</param>
    /// <param name="alpha">The alpha value for alpha-beta pruning, representing the best value that the maximizing player currently guarantees.</param>
    /// <param name="beta">The beta value for alpha-beta pruning, representing the best value that the minimizing player currently guarantees.</param>
    /// <param name="maximizingPlayer">A boolean indicating whether the current player is maximizing (true) or minimizing (false).</param>
    /// <returns>
    /// The optimal move node determined by the Minimax algorithm, containing the calculated heuristic value and the associated move.
    /// The algorithm explores the game tree recursively, considering maximizing and minimizing players,
    /// and utilizes alpha-beta pruning to improve efficiency.
    /// </returns>
    private MoveNode MiniMax(MoveNode moveNode, int depth, int alpha, int beta, bool maximizingPlayer)
    {
        // Check for terminal conditions: depth limit, checkmate, or draw
        bool isCheckMate = _localBoard.IsInCheckmate();
        bool isDraw = _localBoard.IsDraw();

        if (depth == 0 || isCheckMate || isDraw)
        {
            moveNode.HeuristicVal = CalculatePositionValue(moveNode, out bool isCheck);

            if (isCheckMate)
            {
                moveNode.HeuristicVal += depth;
                return moveNode;
            }

            if (isDraw)
            {
                return moveNode;
            }

            if (!isCheck)
            {
                return moveNode;
            }

            depth = 1;
        }
        
        // Generate child nodes for the current move node
        GenerateChildrenMoveNodes(moveNode);
        
        // Initialize a local move node
        MoveNode localMoveNode = new MoveNode();

        if (maximizingPlayer)
        {
            localMoveNode.HeuristicVal = MIN_VAL;
            
            // Iterate over child moves for the maximizing player
            foreach (var childMove in moveNode.ChildMoves)
            {
                _localBoard.MakeMove(childMove.Move);
                
                // Recursively call the MiniMax function for the next depth with the minimizing player
                MoveNode resMoveNode = MiniMax(childMove, depth - 1, alpha, beta, false);

                _localBoard.UndoMove(childMove.Move);
                
                // Update the local move node with the maximum heuristic value
                if (localMoveNode.HeuristicVal < resMoveNode.HeuristicVal)
                {
                    localMoveNode = resMoveNode;
                }
                
                // Update alpha value for alpha-beta pruning
                alpha = Math.Max(alpha, localMoveNode.HeuristicVal);
                
                // Break the loop if beta cutoff condition is met
                if (localMoveNode.HeuristicVal >= beta)
                {
                    break;
                }
            }
        }
        else
        {
            localMoveNode.HeuristicVal = MAX_VAL;
            
            // Iterate over child moves for the minimizing player
            foreach (var childMove in moveNode.ChildMoves)
            {
                _localBoard.MakeMove(childMove.Move);
                
                // Recursively call the MiniMax function for the next depth with the maximizing player
                MoveNode resMoveNode = MiniMax(childMove, depth - 1, alpha, beta, true);

                _localBoard.UndoMove(childMove.Move);
                
                // Update the local move node with the minimum heuristic value
                if (localMoveNode.HeuristicVal > resMoveNode.HeuristicVal)
                {
                    localMoveNode = resMoveNode;
                }
                
                // Update beta value for alpha-beta pruning
                beta = Math.Min(beta, localMoveNode.HeuristicVal);
                
                // Break the loop if alpha cutoff condition is met
                if (localMoveNode.HeuristicVal <= alpha)
                {
                    break;
                }
            }
        }
        
        // Return the move node with the optimal heuristic value
        return localMoveNode;
    }
}
