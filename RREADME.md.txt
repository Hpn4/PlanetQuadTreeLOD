# Planet QuadTree LOD

This project is a QuadTree LOD for planet like object.
The script is configurable in the editor. You can add as many level as you want and you can specify when to switch level.
There is also a custom culling that will discard chunk that does not need to be rendered.

I also added a small FBM generator to create a rough shape.

## Demo

Here an animation of the QuadTree in action. I also added a small FBM to show that cracks does not appear inside a face.
A SampleScene is given if you would like to test.

## How to use

Simply add the Planet script to a gameobject. You can change the radius, the resolution of a single chunk, the number of level...



## Limitations

All the calculation are done on CPU side.
Also, some cracks may appear at the border of faces when they are different level resolution.