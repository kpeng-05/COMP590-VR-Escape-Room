This project was tested on a Quest 3, so it is the recommended hardware to use.

[Video Link](https://youtu.be/KEge8taVWDY)

# Controls
The hand controls are finicky at best. It is recommended to use the Quest 3 controllers.

## Movement & Camera
Both joysticks can be used for movement.
 - Push forward on this joystick to teleport to a location in front of you.
 - Hold forward on this joystick to aim the teleport.

The right joystick can control the camera.
 - Push the joystick to the left or right to shift the camera to the left or right.
 - Once the camera has shifted, it will stay there. 
  - If you are in an area of limited space or do not want to turn around very much, it is recommended to use this functionality.

## Interaction
Grab objects using the inner triggers on either controller. 
Interact with said object using the front trigger on that same controller.
 - Not all grabbable objects have interactions.

# Basic Escape Room Puzzle Flow
<details>
    <summary>Spoilers for the puzzles and how to solve them</summary>

1. Dumbledore the lights to get glow in the dark code -> opens safe with chess piece
    Point the wand at all of the lights to turn them off
    Fire a ray (user triggered?) and see if it collides with the lights
2. Use chess piece to mate in 1 (place white knight on c7)
3. Point conductor baton for statues to play music in correct order (right - left - middle)
    Fire a ray (user triggered) and see if it collides with the robots
4. To escape, user picks up lightsaber to cut hole in wall
</details>

## Daniel Wang Reflections:
<details>
    <summary>Click to Expand </summary>

### What worked: 
The basic mechanics (teleportation movement, "distance" grabbing\*, interacting with the various objects)
are all in a working state.

\*This project uses regular grabbing, although I found the range to be too small, so I increased the grab 
radius by a large enough amount that it might as well be a distance grab.

### What didn't work:
It is possible to softlock yourself by grabbing the interactable objects and clipping them through 
the floor while you are holding onto them and letting go.
The player can teleport into all of the boxes in the room.

### How close is it to the final vision:
For what I envisioned, this is fairly close. The one thing that doesn't match up for me is 
||the lightsaber's interactions with the walls. Every wall is currently just a bunch of cubes that get ||
||deleted upon contact with the lightsaber. My vision was that the player would actually be able to cut ||
||apart the wall, although I have since realized that this is not very feasible for the hardware.||

### Is this game fun
No, I do not think that this game would be very fun to do a blind playthrough for.
There are no instructions for the player on what to do or hints on how to progress.
Real-life escape rooms usually have some kind of indicator on what the players should do next.
Admittedly this is very similar to how some older video games are (e.g., LoZ on the NES), 
where the games themselves have little to no instruction on how to proceed and the player
needs to rely on external sources (like this README).

### Given more time, what would change
I can't really say anything since I started working on the assignment very late in it's lifespan.
What I would do is add more sound effects and other stupid things to make it more funny.
This is because I enjoy making "shitpost" content. One sound effect I would have liked 
to include is the Foxy Jumpscare from FNAF 2. 
I personally would like to add some kind of sound effect cue to help the player progress.
</details>

Assets:
To be filled in later
