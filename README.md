# HydroFlash-Primary-Scripts
The main scripts in HydroFlash for Topps to check out
 Scripts included: 
   * **AIFinder**: The component that handles finding players for the AI
   * **aiPlayerController**: The AI Player, makes decisions on what to do
   * **GameController**: controls questions and gameplay
   * **GameSparksHandler**: controls gamesparks connectivity. This component was formerly what is now "MenuManager" so some methods/control still needs to be moved there.
   * **MenuManager**: Controls everything connected to the menu
   * **playerController**: Controls the player and Gameplay
   * **GameNetworkController**: Controls the networked game. If Master Client, controls game as a whole,
   * **ServerConnector**: Handles Photon functionality
   * **SmoothMouseLook**: Moves the camera and handles camera position related things like shooting
