Animation Engine
================

.. Animation:
.. include:: /Refs.rst


| Vrtist uses a custom animation engine.
| 
| The creation and edition of animations are done by selecting an object or a group of objects and using the dopesheet panel.
|

.. image:: /img/Dopesheet.png
    :align: center
    :width: 300
    :class: no-scaled-link
 
| 
| Properties that can be animated are: the position, rotation and scale of objects. The intensity and color of lights. The focal, focus and aperture of cameras. 
| To open the dopesheet panel go to the camera tool on the palette, and check the animation editor
| 
| The dopesheet offers a variety of controls. 
|
| Interpolation types for animation curves: |ConstantIcon| constant, |LinearIcon| linear, |BezierIcon| Bezier
| Timeline controls:
| |StartIcon| Go to start.
| |prevKeyIcon| Go to the previous keyframe.
| |prevIcon| Go to the previous frame.
| |PlayIcon| Play animation.
| |NextIcon| Go to the next frame.
| |NextKeyIcon| Go to the next keyframe.
| |EndIcon| Go to end.
| |RecordIcon| Record. Clicking on this icon will start a timer. At the end of this timer, move the selection as wanted. This will automatically create keyframes for each frame. 
| Click the |StopIcon| icon that replaced the record icon to stop recording.

| The |TrashIcon| icon will delete the animation for the selection. 
| |AddKeyIcon| Add a keyframe at the current time for the object. Set the object as wanted, then click the icon to add a keyframe. Move the current frame on the timeline, change the object parameters, then add a new keyframe. Repeat these steps as needed to create an animation.
| |AutoKeyIcon| Auto-key. Click this icon to lock the auto-key setting. A keyframe will be added every time the object is moved or a parameter is changed. Move to a new frame, and move the object/parameter to create a new keyframe.
| |RemoveKeyIcon| Remove a keyframe at the current frame.
| To change the current frame, slide the blue marker on the timeline. By default the timeline shows keyframes 0 to 250. These values can be changed by clicking on them. 
| To change the displayed keyframes drag the start or end circles on the slider over the timeline. The gap can then be dragged along the slider to change the section displayed.
| 

.. image:: /img/TimelineSlider.png
    :align: center
    :width: 300
    :class: no-scaled-link

|
| The current keyframe can also be changed by moving the primary controller joystick left or right.
|
| The label on the right of the keyframe shows the current time, and keyframe number.
| Underneath the selected object or number of objects is shown.
| Under the timeline, keyframes added for this selection are displayed by a yellow diamond.


