1. Add a 3D sphere to the SampleScene, parallel to the monza_res3.
2. Attach MoveSphere.cs script to the 3D sphere.
3. Add camera to the scene if wanted.
4. Play it and view the result.

For the z-axis in the map (the gravity axis), I did not set car.z, which can be obtained from the retrieved data. Instead I added mesh collider component (in the Inspector) for the map and rigidbody component for the sphere so then it will have gravitational force. I haven’t tried setting the car.z, can be tried.

The Monza_traj.py will generate data and plot for this race in Monza. Might be helpful for coordinate transformation.

## Sphere Collision and Gravity
- We will need a cleaned and if possible flattened track. Otherwise I found the sphere rolling on the unflattened ground  because of its gravity and also colliding with obstacles. So currently I turned off sphere collider and its gravity.

## (Accurate) tracking on Monza
- Follow the steps above to create a sphere at the origin (0, 0, 10). For better visibility increase the size to 10x10x10
- Remove the 90 degree x rotation from the monza map
- Set the transform position of the map to x = 150, y = 2398, z = 150
- Go Edit->Project Settings->Physics, make sure the gravity axis is in z, not default y.
- Run play, and if necessary swap from the "play" mode to "scene" mode to be able to watch the whole map.

### Notes on the SphereMove.cs script.
- Only transformations are setting the scale to 0.1 and flipping x inputs which seem to be mirrored against the orientation of the map.
- Logic to implement is commented with "TODO". The most important two are to identify the end of the race and to see whether it's possible to further smooth it when the car is fast so consecutive trajectory points are still a bit far from each other.
- I would personally doubt on its achievability in real time races because of the update rate reqirement? Not sure.