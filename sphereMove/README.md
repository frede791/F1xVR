1. Add a 3D sphere to the map scene, set whatever size for the sphere.
2. Attach MoveSphere.cs script to the 3D sphere.
3. Add camera to the scene.
4. Play it and view the result.

For the z-axis in the map (the gravity axis), I did not set car.z, which can be obtained from the retrieved data. Instead I added mesh collider component (in the Inspector) for the map and rigidbody component for the sphere so then it will have gravitational force. I haven’t tried setting the car.z, can be tried.

The Monza_traj.py will generate data and plot for this race in Monza. Might be helpful for coordinate transformation.


## (Accurate) tracking on Monza
- Follow the steps above to create a sphere at the origin. For better visibility increase the size to 10x10x10
- Remove the 90 degree x rotation from the monza map
- Set the transform position of the map to x = 150, y = 2398, z = 150
- Run play, and if necessary swap from the "play" mode to "scene" mode to be able to watch the whole map.

### Notes on the SphereMove.cs script.
- Only transformations are setting the scale to 0.1 and flipping x inputs which seem to be mirrored against the orientation of the map.
- All other modifications are turned off.
