# Unity-Voxel-Cave-Generator
3D voxel cave generator.

To use the cave generator simply create an empty game object in a unity scene and drag the script onto it. The script executes on awake and depending on what settings are being used it can sometimes take awhile to load (especially if you don't have good hardware). The cave's custom mesh doesn't have UV's generated so using realtime and baked lighting is a bit iffy, however, using unity's built in fog and ambient lighting can make the cave outline very clear. The script also gives the option to apply a custom material to the cave, so dragging a material into the inspector will apply it to the cave.
