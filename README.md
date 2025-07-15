# Chessbot 2.0

Hi!
In this project I aim to write a fully functional chess game, and then a chess engine that will hopefully become a better chess player than I am.
As of 2025-07-15, after about 3 days work, a fully functional chess game was completed! 

# FEN Notation
As per standard of chess programs, FEN notation is used for piece placement. FEN notation is simply a single string that represents the entire state of the game; not just piece placement but also castling rights, move counter, en passant square and more. The starting position looks like this in FEN:
> rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1

Let's break down what it means! The first part of the notation shows the locations of the pieces, starting on row 8 column 1. Small characters denote black pieces and upper-case means white pieces. A forward slash / means that the end of the row has been reached, and a number x indicates a gap of x squares with no pieces at all. 
After a bit of coding, entering this into my chess program looked like this:
![Starting position](https://github.com/Edvard-vP/Chess-Bot-2.0/blob/main/Assets/starting_position.PNG)
Nice!
The remaining parts of the FEN notation are, in order:

 - Active color
 - Castling rights
 - En passant square
 - Half move clock
 - Full move counter

 At the beginning I just read these into variables, before implementing them as I implemented the movements of the different pieces.
# Structure of the code
The basic chess game has 3 classes:

 - Board Handler
 - Piece Handler
 - Rules Handler

Lets go through and see what they do!
## Board Handler
This class keeps track of the visual board. It keeps track of the pieces, places out the FEN notation and handles updating the board after a new move has been made. It also handles highlighting possible moves. More pictures on that later!
## Piece Handler
This simple class attached to all pieces handles both the properties of each piece and their movement. A piece stores both its FEN name as a char, as well as its own x and y coordinates on the board. Other than that it also has logic for mouse movement, allowing a player to click on and drag around a selected piece.
## Rules Handler
This is the big chess rule book that is the heart the chess program. It solves one simple problem: given a selected piece, return all legal moves that piece can make. It also automatically makes the game a draw in case of 3-fold repetition of the 50 move rules (more on this later on). 
# Piece movement
Here is a quick rundown on how the code finds all legal moves for a rook:

 - Start one step to one side of the rook. Iterate in that direction and mark the square as a legal move if:
    - The square is within the confines of the board
    - The square is empty
 - If the square contains an enemy, mark it as a legal move but stop iterating in that direction.
 - If the square contains a friendly piece, stop iterating in that direction.

Repeating this in the four different directions will yield all valid square a rook can move to.
![Rook movement](https://github.com/Edvard-vP/Chess-Bot-2.0/blob/main/Assets/rook.PNG)

There is however one major rule that needs to be followed: a move can not put the player into check.
## Preventing self-check
This was quite tricky to implement but I eventually found a decent way to implement it. For every piece, when a move that is legal by the basic movements is found, that move is not yet added to the list of legal moves. Instead, that move is made on a virtual board. On this virtual board, all squares the enemy is attacking are found. If the king of the color that made the move is on one of these squares, the move is rejected. 
However! There is a risk here of creating an infinite loop. It is very important to not "check for check" on any of the enemy pieces. If that is done, we get an infinite loop looking like this:

 - White wants to move a piece. Will this create self-check?
 - Make the move on a virtual board to check enemy attacks
 - Check the first black piece. Where can this move? Will this create self-check?
 - Make the move on a virtual board
 - .. ad infinitum

The way I solved this was to simply tell the movement-finder function to *not* look for check when finding all squares the enemy is attacking. This is okay since the enemy is not actually moving in this case, we are the ones making a move.

Here is a quick sanity check to make sure a check can not be ignored:
![The white bishop can not move only one step](https://github.com/Edvard-vP/Chess-Bot-2.0/blob/main/Assets/movement4.PNG)

![The king must move out of the way](https://github.com/Edvard-vP/Chess-Bot-2.0/blob/main/Assets/movement5.PNG)

![The knight only has one valid move](https://github.com/Edvard-vP/Chess-Bot-2.0/blob/main/Assets/movement6.PNG)

![Capturing the attacker also works](https://github.com/Edvard-vP/Chess-Bot-2.0/blob/main/Assets/movement7.PNG)

## En passant
After a pawn is moved to steps forward, the square behind is marked as a possible en passant square. When checking for pawn movements, if the pawn is in the right position, it can capture en passant. After any move has been made, the chance to en passant is lost.
![Black just moved g pawn two steps forward](https://github.com/Edvard-vP/Chess-Bot-2.0/blob/main/Assets/enpassant.PNG)

## Castling
Castling is handled when checking for valid king moves. There are a few caveats to castling, namely some edges cases where castling is *not* allowed:

 - If the king has been moved
 - If the corresponding rook has been moved
 - If *any* of the squares along the castling line is in check.

Another small detail that needed to be added was to make another "shadow move" which also moves the corresponding rook. Initially I made a small blunder which made the move count increase twice here (once for the king, and once for the rook).



## 3-fold repetition
In standard chess, if the same position has been reached three times, the game is automatically declared a draw. 
I thought for a while what the best way to store all previous positions in a chess game would be, and finally landed at a dictionary. 
What to store in the dictionary? The answer is as obvious as it is clean: the FEN notation! So after every move, the current FEN notation is calculated. After this the part of the FEN-string that details the pieces are stored in the dict as a key. If the same key is added 3 times, the same position has been seen thrice and the game is a draw. 
This is a fairly efficient way of solving it, as it only requires the rapid lookup of a dictionary, and the FEN notation was updated anyways.
