# Tilt Five Tools Demo

This folder contains a demo Tilt Five application. It consists of a main menu
and a couple of demo mini-games.


## Top Scenes

The "main.tscn" scene is the starting scene of the game. It extends from
Tilt Five Tools "Staging" to get scene switching and manage the players.
It is configured to:
* Load the main menu scene at start
* Use the custom "player" scene for players
* Adds a spectator camera

The "player.tscn" scene extends from the Tilt Five Tools "Player" to get basic
player behavior, and additionally:
* Adds a Pointer to the wand
* Adds Board Scale button support to the wand
* Adds glasses and wand models for the spectator camera


## Main Menu

The "main_menu.tscn" scene extends from Tilt Five Tools "Scene" and adds a
Viewport2Din3D to host the main menu UI. This UI (defined in "main_menu_2d.tscn")
provides a basic menu allowing the player to run the two demo mini-games or
to quit.


## Demo 1

The first demo mini-game shows how to handle a scene with a character avatar.
Every player has a sphere character spawned for them which they can move
around using the wand joystick and buttons.

The "demo1_scene.tscn" scene extends from Tilt Five Tools "Scene" and:

* Sets "character.tscn" to be loaded to make characters for each player
* Contains a basic map for the players to move around on
* Contains Scene Switch areas to reload the main menu on death or finish

The "character.tscn" is instantiated for each player. On load it gets the
players wand and subscribes to button and joystick events to move the
players sphere body around the world.


## Demo 2

The second demo mini-game shows how to handle a scene with no character
avatars. The players have to defend a base by shooting their pointer at
the ground and firing missiles at it to destroy the enemies.

The "demo2_scene.tscn" scene extends from Tilt Five Tools "Scene" and:

* Contains a ground object which reacts to wand clicks by firing missiles
* Contains enemy spawn points which spawn enemies that charge the players base
* The missiles flies to the ground and explodes killing enemies in range
* The enemies move towards the player base
* If an enemy reaches the player base it triggers a reload of the main menu
